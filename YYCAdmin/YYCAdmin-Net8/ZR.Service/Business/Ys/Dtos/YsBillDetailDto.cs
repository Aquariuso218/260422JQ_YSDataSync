using Newtonsoft.Json;

namespace ZR.Service.Business.Ys.Dtos
{
    /// <summary>
    /// YS 单据详情接口返回对象。
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
        [JsonProperty("billDate")]
        public DateTime? BillDate { get; set; }

        /// <summary>
        /// 制单人。
        /// </summary>
        [JsonProperty("creator")]
        public string Creator { get; set; }

        /// <summary>
        /// 组织 Id。
        /// </summary>
        [JsonProperty("accentity")]
        public string Accentity { get; set; }

        /// <summary>
        /// 资金付款单表体。
        /// </summary>
        [JsonProperty("FundPayment_b")]
        public List<YsFundBillBodyItemDto> FundPaymentBody { get; set; }

        /// <summary>
        /// 资金收款单表体。
        /// </summary>
        [JsonProperty("FundCollection_b")]
        public List<YsFundBillBodyItemDto> FundCollectionBody { get; set; }
    }

    /// <summary>
    /// YS 资金单据表体行对象。
    /// </summary>
    public class YsFundBillBodyItemDto
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
        /// 部门 Id。
        /// </summary>
        [JsonProperty("dept")]
        public string Dept { get; set; }

        /// <summary>
        /// 业务员 Id。
        /// </summary>
        [JsonProperty("operator")]
        public string Operator { get; set; }

        /// <summary>
        /// 企业银行账户 Id。
        /// </summary>
        [JsonProperty("enterprisebankaccount")]
        public string EnterpriseBankAccount { get; set; }

        /// <summary>
        /// 结算方式 Id。
        /// </summary>
        [JsonProperty("settlemode")]
        public string SettleMode { get; set; }

        /// <summary>
        /// 结算状态。
        /// </summary>
        [JsonProperty("settlestatus")]
        public string SettleStatus { get; set; }

        /// <summary>
        /// 款项类型名称。
        /// </summary>
        [JsonProperty("quickType_name")]
        public string QuickTypeName { get; set; }

        /// <summary>
        /// 往来对象类型。
        /// </summary>
        [JsonProperty("caobject")]
        public int? Caobject { get; set; }

        /// <summary>
        /// 往来对象 Id。
        /// </summary>
        [JsonProperty("oppositeobjectid")]
        public string OppositeObjectId { get; set; }

        /// <summary>
        /// 原币金额。
        /// </summary>
        [JsonProperty("oriSum")]
        public decimal? OriSum { get; set; }
    }
}
