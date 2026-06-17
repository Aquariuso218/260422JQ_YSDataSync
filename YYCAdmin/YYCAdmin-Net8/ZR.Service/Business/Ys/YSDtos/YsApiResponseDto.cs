using Newtonsoft.Json;

namespace ZR.Service.Business.Ys.Dtos
{
    public class YsApiResponseDto<T>
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }
    }
}
