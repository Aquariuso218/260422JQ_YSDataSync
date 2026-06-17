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
        /// 资金组织ID(备用)
        /// </summary>
        [JsonProperty("accentity")]
        public string accentity { get; set; }

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

        /// <summary>
        /// 联行号。
        /// </summary>
        [JsonProperty("settleBench_bCharacterSys__GYSYHHH")]
        public string crBankNo { get; set; }

        /// <summary>
        /// 对方银行账号。
        /// </summary>
        [JsonProperty("counterpartybankacc")]
        public string caccountNum { get; set; }

        /// <summary>
        /// 对方银行账户名称
        /// </summary>
        [JsonProperty("counterpartyaccname")]
        public string caccountName { get; set; }

        /// <summary>
        /// 开户行。
        /// </summary>
        [JsonProperty("counterpartybankname")]
        public string cbranch { get; set; }

        /// <summary>
        /// 摘要。
        /// </summary>
        [JsonProperty("description")]
        public string cdigest { get; set; }


        /// <summary>
        /// 对方名称
        /// </summary>
        [JsonProperty("counterpartyname")]
        public string counterpartyname { get; set; }


        /// <summary>
        /// 票据方向。
        /// 接口不存在时为 null，有值时一般为 "1"、"2"、""、null。
        /// </summary>
        [JsonProperty("receiptDirection")]
        public string ReceiptDirection { get; set; }


        /// <summary>
        /// 来源单据号
        /// </summary>
        [JsonProperty("bizbillno")]
        public string bizbillno { get; set; }
             


    }
}
