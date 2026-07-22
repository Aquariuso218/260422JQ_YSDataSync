using Newtonsoft.Json;

namespace ZR.Service.Business.Ys.Dtos
{
    public class YsRegisterDetailDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("payerCode")]
        public string PayerCode { get; set; }

        [JsonProperty("payerName")]
        public string PayerName { get; set; }
        
        [JsonProperty("noteno")]
        public string NoteNo { get; set; }
    }
}
