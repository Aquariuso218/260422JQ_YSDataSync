using Newtonsoft.Json;

namespace ZR.Service.Business.Ys.Dtos
{
    public class YsTokenResponseDto
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("data")]
        public YsTokenDataDto Data { get; set; }
    }

    public class YsTokenDataDto
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }
}
