
namespace ZR.Model.Business.Dto
{
    /// <summary>
    /// 查询对象
    /// </summary>
    public class EfMidysbilldataQueryDto : PagerInfo
    {
        public string Id { get; set; }
        public string MainId { get; set; }
        public string CVouchCode { get; set; }
        public DateTime? BeginBillDate { get; set; }
        public DateTime? EndBillDate { get; set; }
        public int? SettleStatus { get; set; }
        public string QuickTypeName { get; set; }
        public string CVouchType { get; set; }
        public string CDwType { get; set; }
        public int? ProcessStatus { get; set; }
        public string U8Code { get; set; }
    }

    /// <summary>
    /// 输入输出对象
    /// </summary>
    public class EfMidysbilldataDto
    {
        [Required(ErrorMessage = "AutoId不能为空")]
        public int AutoId { get; set; }

        public string Id { get; set; }

        public string MainId { get; set; }

        public string CVouchCode { get; set; }

        public DateTime? BillDate { get; set; }

        public string CMaker { get; set; }

        public string OrgCode { get; set; }

        public string CDepCode { get; set; }

        public string CNatBankAccount { get; set; }

        public string CNatBank { get; set; }

        public string CSSName { get; set; }

        public int? SettleStatus { get; set; }

        public string QuickTypeName { get; set; }

        public string CVouchType { get; set; }

        public string CDwType { get; set; }

        public string CDwCode { get; set; }

        public decimal IAmount { get; set; }

        public string CNoteCode { get; set; }

        public string TradetypeName { get; set; }

        public decimal DiscountInterest { get; set; }

        public string NoteTypeCode { get; set; }

        public DateTime? CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }

        public int? ProcessStatus { get; set; }

        public string ProcessMsg { get; set; }

        public string U8Code { get; set; }

        public DateTime? SynTime { get; set; }



        [ExcelColumn(Name = "Settlestatus")]
        public string SettleStatusLabel { get; set; }


        public string cbank { get; set; }

        public string cbranch { get; set; }

        public string caccountNum { get; set; }

        public string caccountName { get; set; }

        public string crBankNo { get; set; }

        public string cdigest { get; set; }
    }
}