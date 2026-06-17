using Newtonsoft.Json;

namespace ZR.Service.Business.Ys.Dtos
{
    /// <summary>
    /// 贴现办理查询请求。
    /// </summary>
    public class YsDiscountQueryRequestDto
    {
        [JsonProperty("ytenantId")]
        public string YtenantId { get; set; }

        [JsonProperty("discountDateStart")]
        public string DiscountDateStart { get; set; }

        [JsonProperty("discountDateEnd")]
        public string DiscountDateEnd { get; set; }

        [JsonProperty("billStatus")]
        public string BillStatus { get; set; }
    }

    /// <summary>
    /// 到期兑付查询请求。
    /// </summary>
    public class YsExpireCashQueryRequestDto
    {
        [JsonProperty("paymentDateStart")]
        public string PaymentDateStart { get; set; }

        [JsonProperty("paymentDateEnd")]
        public string PaymentDateEnd { get; set; }

        [JsonProperty("billStatus")]
        public string BillStatus { get; set; }
    }

    /// <summary>
    /// 银行托收查询请求。
    /// </summary>
    public class YsConsignBankQueryRequestDto
    {
        [JsonProperty("consignDateStart")]
        public string ConsignDateStart { get; set; }

        [JsonProperty("consignDateEnd")]
        public string ConsignDateEnd { get; set; }

        [JsonProperty("billStatus")]
        public string BillStatus { get; set; }
    }

    /// <summary>
    /// 贴现办理返回数据。
    /// </summary>
    public class YsDiscountDetailResponseDataDto
    {
        [JsonProperty("resultCount")]
        public int ResultCount { get; set; }

        [JsonProperty("discountDatas")]
        public List<YsDiscountRecordDto> DiscountDatas { get; set; }
    }

    /// <summary>
    /// 通用 recordList 返回数据。
    /// </summary>
    public class YsRecordListResponseDataDto<TRecord>
    {
        [JsonProperty("recordNum")]
        public int RecordNum { get; set; }

        [JsonProperty("recordList")]
        public List<TRecord> RecordList { get; set; }
    }

    /// <summary>
    /// 贴现办理单据记录。
    /// </summary>
    public class YsDiscountRecordDto
    {
        [JsonProperty("billId")]
        public string BillId { get; set; }

        [JsonProperty("billCode")]
        public string BillCode { get; set; }

        [JsonProperty("discountDate")]
        public DateTime? DiscountDate { get; set; }

        [JsonProperty("accentity")]
        public string Accentity { get; set; }

        [JsonProperty("invoiceRoles")]
        public string InvoiceRoles { get; set; }

        [JsonProperty("invoicerId")]
        public string InvoicerId { get; set; }

        [JsonProperty("discountBankAccount")]
        public string DiscountBankAccount { get; set; }

        [JsonProperty("discountAmount")]
        public decimal? DiscountMoney { get; set; }

        [JsonProperty("noteno")]
        public string NoteNo { get; set; }

        [JsonProperty("discountInterest")]
        public decimal? DiscountInterest { get; set; }

        [JsonProperty("noteTypeCode")]
        public string NoteTypeCode { get; set; }
    }

    /// <summary>
    /// 到期兑付单据记录。
    /// </summary>
    public class YsExpireCashRecordDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("paymentDate")]
        public DateTime? PaymentDate { get; set; }

        [JsonProperty("accentityCode")]
        public string AccentityCode { get; set; }

        [JsonProperty("paybankaccount")]
        public string PayBankAccount { get; set; }

        [JsonProperty("receiveroles")]
        public string ReceiverRoles { get; set; }

        [JsonProperty("receiverCode")]
        public string ReceiverCode { get; set; }

        [JsonProperty("oriSum")]
        public decimal? OriSum { get; set; }

        [JsonProperty("noteno")]
        public string NoteNo { get; set; }

        [JsonProperty("quicktype")]
        public string QuickType { get; set; }

        [JsonProperty("billType")]
        public string BillType { get; set; }
    }

    /// <summary>
    /// 银行托收单据记录。
    /// </summary>
    public class YsConsignBankRecordDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("consigndate")]
        public DateTime? ConsignDate { get; set; }

        [JsonProperty("accentityCode")]
        public string AccentityCode { get; set; }

        [JsonProperty("consignbankacc")]
        public string ConsignBankAccount { get; set; }

        [JsonProperty("invoiceroles")]
        public string InvoicerRoles { get; set; }

        [JsonProperty("invoicerCode")]
        public string InvoicerCode { get; set; }

        [JsonProperty("consignAmount")]
        public decimal? ConsignAmount { get; set; }

        [JsonProperty("noteNo")]
        public string NoteNo { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("billType")]
        public string BillType { get; set; }
    }
}
