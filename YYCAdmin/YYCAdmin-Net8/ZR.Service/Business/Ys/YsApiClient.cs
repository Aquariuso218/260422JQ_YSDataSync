using Infrastructure;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using ZR.Service.Business.Ys.Dtos;

namespace ZR.Service.Business.Ys
{
    internal sealed class YsApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly YsConfigOptions _config;

        private static readonly ConcurrentDictionary<string, DateTime> _lastRequestTimes = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _rateLimitLock = new();
        private const int MinRequestIntervalMs = 1500; // 同一API的最短调用间隔为 1.5 秒

        /// <summary>
        /// 初始化 YS 接口客户端。
        /// </summary>
        /// <param name="httpClientFactory">用于创建 HTTP 客户端的工厂。</param>
        public YsApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _config = AppSettings.Get<YsConfigOptions>(YsBillSyncConstants.ConfigSectionName) ?? new YsConfigOptions();
        }

        /// <summary>
        /// 调用 YS 鉴权接口获取访问令牌。
        /// </summary>
        /// <returns>返回后续业务接口调用使用的 access_token。</returns>
        public async Task<string> GetAccessTokenAsync()
        {
            ValidateConfig();

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            // 与参考程序保持一致：按 appKey、timestamp 排序后使用 HMACSHA256 生成签名。
            var signature = GenerateSignature(new Dictionary<string, string>
            {
                ["appKey"] = _config.AppKey,
                ["timestamp"] = timestamp
            }, _config.AppSecret);

            var requestUri =
                $"{_config.AuthBaseUrl}?appKey={Uri.EscapeDataString(_config.AppKey)}&timestamp={Uri.EscapeDataString(timestamp)}&signature={signature}";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var responseContent = await SendAsync(request);
            var tokenResponse = JsonConvert.DeserializeObject<YsTokenResponseDto>(responseContent)
                ?? throw new InvalidOperationException("YS token response is empty.");

            if (!string.Equals(tokenResponse.Code, "00000", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"YS token request failed: {tokenResponse.Message ?? tokenResponse.Msg ?? tokenResponse.Code}");
            }

            var accessToken = tokenResponse.Data?.AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new InvalidOperationException("YS token response did not include access_token.");
            }

            return accessToken;
        }

        /// <summary>
        /// 发送 POST 请求，并将响应内容反序列化为指定类型。
        /// </summary>
        public async Task<T> PostAsync<T>(string relativePath, object requestBody, string accessToken)
        {
            var requestUri = BuildBusinessUri(relativePath, accessToken);
            var payload = JsonConvert.SerializeObject(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            return await SendAndDeserializeAsync<T>(request);
        }

        /// <summary>
        /// 发送 GET 请求，并将响应内容反序列化为指定类型。
        /// </summary>
        public async Task<T> GetAsync<T>(string relativePath, IDictionary<string, string> query, string accessToken)
        {
            var requestUri = BuildBusinessUri(relativePath, accessToken, query);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            return await SendAndDeserializeAsync<T>(request);
        }

        /// <summary>
        /// 发送请求，并执行统一的 JSON 反序列化流程。
        /// </summary>
        private async Task<T> SendAndDeserializeAsync<T>(HttpRequestMessage request)
        {
            var responseContent = await SendAsync(request);
            return JsonConvert.DeserializeObject<T>(responseContent)
                ?? throw new InvalidOperationException($"YS response could not be deserialized to {typeof(T).Name}.");
        }

        /// <summary>
        /// 发送原始 HTTP 请求，并校验 HTTP 状态码。包含漏桶限速与自动退避重试逻辑。
        /// </summary>
        private async Task<string> SendAsync(HttpRequestMessage request)
        {
            var client = _httpClientFactory.CreateClient(nameof(YsApiClient));
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            int retryCount = 0;
            const int maxRetries = 3;
            const int retryDelayMs = 5000; // 遇到限频时重试等待 5 秒

            while (true)
            {
                // 1. 防频限速控制 (相对路径漏桶限流)
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;
                TimeSpan delay = TimeSpan.Zero;
                lock (_rateLimitLock)
                {
                    var now = DateTime.UtcNow;
                    if (_lastRequestTimes.TryGetValue(path, out var lastTime))
                    {
                        var elapsed = now - lastTime;
                        if (elapsed.TotalMilliseconds < MinRequestIntervalMs)
                        {
                            delay = TimeSpan.FromMilliseconds(MinRequestIntervalMs - elapsed.TotalMilliseconds);
                            _lastRequestTimes[path] = now + delay;
                        }
                        else
                        {
                            _lastRequestTimes[path] = now;
                        }
                    }
                    else
                    {
                        _lastRequestTimes[path] = now;
                    }
                }

                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay);
                }

                // 2. 深度复制 HTTP 请求以防重复发送报错
                using var cloneRequest = await CloneHttpRequestMessageAsync(request);

                try
                {
                    using var response = await client.SendAsync(cloneRequest);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    bool isRateLimit = false;

                    // 判断是否触发限流限制 (HTTP 429 或 响应 JSON 中的特定限流信息)
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        isRateLimit = true;
                    }
                    else
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(responseContent) && responseContent.Trim().StartsWith("{"))
                            {
                                var json = Newtonsoft.Json.Linq.JObject.Parse(responseContent);
                                var code = json["code"]?.ToString();
                                var msg = json["message"]?.ToString() ?? json["msg"]?.ToString();
                                if (code == "apigw.limit.rate.error" || 
                                    (msg != null && (msg.Contains("频繁") || msg.Contains("限流") || msg.Contains("限频") || msg.Contains("Too Many Requests"))))
                                {
                                    isRateLimit = true;
                                }
                            }
                        }
                        catch
                        {
                            // 忽略 JSON 解析报错
                        }
                    }

                    if (isRateLimit && retryCount < maxRetries)
                    {
                        retryCount++;
                        await Task.Delay(retryDelayMs);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException($"YS request failed ({(int)response.StatusCode}): {responseContent}");
                    }

                    return responseContent;
                }
                catch (Exception) when (retryCount < maxRetries)
                {
                    // 遇到网络异常或其他临时请求失败亦可尝试重试
                    retryCount++;
                    await Task.Delay(retryDelayMs);
                }
            }
        }

        /// <summary>
        /// 深度复制 HTTP 请求消息，规避已发送请求被重复发送的错误。
        /// </summary>
        private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri);
            if (req.Content != null)
            {
                var ms = new MemoryStream();
                await req.Content.CopyToAsync(ms);
                ms.Position = 0;
                clone.Content = new StreamContent(ms);
                foreach (var header in req.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            foreach (var header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            return clone;
        }

        /// <summary>
        /// 校验 YS 必填配置是否完整。
        /// </summary>
        private void ValidateConfig()
        {
            if (string.IsNullOrWhiteSpace(_config.AuthBaseUrl) ||
                string.IsNullOrWhiteSpace(_config.GatewayBaseUrl) ||
                string.IsNullOrWhiteSpace(_config.AppKey) ||
                string.IsNullOrWhiteSpace(_config.AppSecret))
            {
                throw new InvalidOperationException($"Missing YS configuration section: {YsBillSyncConstants.ConfigSectionName}");
            }
        }

        /// <summary>
        /// 组装包含 access_token 和可选查询参数的网关业务地址。
        /// </summary>
        private string BuildBusinessUri(string relativePath, string accessToken, IDictionary<string, string> query = null)
        {
            var uriBuilder = new StringBuilder($"{_config.GatewayBaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}");
            uriBuilder.Append($"?access_token={Uri.EscapeDataString(accessToken)}");

            // 业务接口统一通过查询字符串传递 access_token 和接口要求的附加参数。
            if (query != null)
            {
                foreach (var item in query.Where(x => !string.IsNullOrWhiteSpace(x.Value)))
                {
                    uriBuilder.Append('&')
                        .Append(Uri.EscapeDataString(item.Key))
                        .Append('=')
                        .Append(Uri.EscapeDataString(item.Value!));
                }
            }

            return uriBuilder.ToString();
        }

        /// <summary>
        /// 生成 YS 平台要求的鉴权签名。
        /// </summary>
        private static string GenerateSignature(IReadOnlyDictionary<string, string> parameters, string appSecret)
        {
            // 按 key 升序拼接参与签名的参数，并排除 signature 字段本身。
            var stringToSign = string.Concat(parameters
                .Where(x => !string.Equals(x.Key, "signature", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Value))
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => $"{x.Key}{x.Value}"));

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            return Uri.EscapeDataString(Convert.ToBase64String(hash));
        }
    }
}
