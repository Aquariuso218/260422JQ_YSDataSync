using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZR.Service.Business.U8.Dtos
{
    /// <summary>
    /// U8 收付款单新增接口请求体。
    /// </summary>
    internal sealed class U8ApCloseBillAddRequestDto
    {
        [JsonProperty("orgCode")]
        public string OrgCode { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("cVouchCode")]
        public string CVouchCode { get; set; }

        [JsonProperty("quickTypeName")]
        public string QuickTypeName { get; set; }

        [JsonProperty("cVouchType")]
        public string CVouchType { get; set; }

        [JsonProperty("cDwType")]
        public string CDwType { get; set; }

        [JsonProperty("cDwCode")]
        public string CDwCode { get; set; }

        [JsonProperty("billDate")]
        public string BillDate { get; set; }

        [JsonProperty("cNatBankAccount")]
        public string CNatBankAccount { get; set; }

        [JsonProperty("cNatBank")]
        public string CNatBank { get; set; }

        [JsonProperty("cSSName")]
        public string CSSName { get; set; }

        [JsonProperty("cDepCode")]
        public string CDepCode { get; set; }

        [JsonProperty("cPersonCode")]
        public string CPersonCode { get; set; }

        [JsonProperty("cDigest")]
        public string CDigest { get; set; }

        [JsonProperty("cmaker")]
        public string CMaker { get; set; }

        [JsonProperty("cNoteCode")]
        public string CNoteCode { get; set; }

        [JsonProperty("receiptDirection")]
        public string ReceiptDirection { get; set; }

        [JsonProperty("tradetypeName")]
        public string TradetypeName { get; set; }

        [JsonProperty("iAmount")]
        public decimal? IAmount { get; set; }
    }
    
}
