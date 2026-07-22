using Newtonsoft.Json;

namespace ZR.Service.Business.U8.U8Dtos
{
    /// <summary>
    /// U8 资金期初余额同步 YS 接口响应 Dto
    /// </summary>
    public class U8TOBalanceSyncResponseDto
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
