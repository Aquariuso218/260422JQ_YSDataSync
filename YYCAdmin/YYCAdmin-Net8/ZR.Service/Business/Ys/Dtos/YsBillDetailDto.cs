using Newtonsoft.Json;

namespace ZR.Service.Business.Ys.Dtos
{
    /// <summary>
    /// YS 结算单详情接口返回对象。
    /// </summary>
    public class YsBillDetailDto
    {
        /// <summary>
        /// 单据主表 Id。
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// 单据编码。
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary>
        /// 单据日期。
        /// </summary>
        [JsonProperty("vouchdate")]
        public DateTime? BillDate { get; set; }

        /// <summary>
        /// 制单人。
        /// </summary>
        [JsonProperty("creator")]
        public string Creator { get; set; }

        /// <summary>
        /// 结算单表体。
        /// </summary>
        [JsonProperty("settleBench_b")]
        public List<YsBillBodyItemDto> SettleBenchBody { get; set; }
    }

    /// <summary>
    /// YS 结算单表体行对象。
    /// </summary>
    public class YsBillBodyItemDto
    {
        /// <summary>
        /// 表体行 Id。
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// 主表 Id。
        /// </summary>
        [JsonProperty("mainid")]
        public string MainId { get; set; }

        /// <summary>
        /// 组织 Id。
        /// </summary>
        [JsonProperty("org")]
        public string Org { get; set; }

        /// <summary>
        /// 部门编码。
        /// </summary>
        [JsonProperty("dept_code")]
        public string DeptCode { get; set; }

        /// <summary>
        /// 企业银行账号。
        /// </summary>
        [JsonProperty("ourbankaccount_account")]
        public string OurBankAccount { get; set; }

        /// <summary>
        /// 企业银行账户名称。
        /// </summary>
        [JsonProperty("ourbankaccount_name")]
        public string OurBankName { get; set; }

        /// <summary>
        /// 结算方式名称。
        /// </summary>
        [JsonProperty("settlemode_name")]
        public string SettleModeName { get; set; }

        /// <summary>
        /// 结算状态。
        /// </summary>
        [JsonProperty("statementdetailstatus")]
        public int? SettleStatus { get; set; }

        /// <summary>
        /// 款项类型名称。
        /// </summary>
        [JsonProperty("proceedType_name")]
        public string QuickTypeName { get; set; }

        /// <summary>
        /// 表体收付款类型。
        /// </summary>
        [JsonProperty("receipttypeb")]
        public int? ReceiptTypeBody { get; set; }

        /// <summary>
        /// 兼容部分返回中直接使用 receipttype 的情况。
        /// </summary>
        [JsonProperty("receipttype")]
        public int? ReceiptType { get; set; }

        /// <summary>
        /// 往来对象类型。
        /// </summary>
        [JsonProperty("counterpartytype")]
        public string CounterpartyType { get; set; }

        /// <summary>
        /// 往来对象 Id。
        /// </summary>
        [JsonProperty("counterpartyid")]
        public string CounterpartyId { get; set; }

        /// <summary>
        /// 原币金额。
        /// </summary>
        [JsonProperty("originalcurrencyamt")]
        public decimal? OriginalCurrencyAmount { get; set; }

        /// <summary>
        /// 兼容旧示例中出现的成功金额字段。
        /// </summary>
        [JsonProperty("sucessamount")]
        public decimal? SuccessAmount { get; set; }

        /// <summary>
        /// 票据号。
        /// </summary>
        [JsonProperty("swbillno")]
        public string NoteCode { get; set; }

        /// <summary>
        /// 来源交易类型。
        /// </summary>
        [JsonProperty("tradetype_name")]
        public string TradetypeName { get; set; }
    }
}
