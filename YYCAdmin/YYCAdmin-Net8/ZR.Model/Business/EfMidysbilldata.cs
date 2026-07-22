namespace ZR.Model.Business
{
    [SugarTable("EF_MidYSBillData")]
    [Tenant("0")]
    public class EfMidysbilldata
    {
        [SugarColumn(ColumnName = "autoId", IsPrimaryKey = true, IsIdentity = true)]
        public int AutoId { get; set; }
        
        [SugarColumn(ColumnName = "id",  Length = 50, IsNullable = true)]
        public string Id { get; set; } 

        [SugarColumn(ColumnName = "mainid", Length = 50, IsNullable = true)]
        public string MainId { get; set; }

        [SugarColumn(ColumnName = "cVouchCode", Length = 50, IsNullable = true)]
        public string CVouchCode { get; set; }

        [SugarColumn(ColumnName = "billDate", IsNullable = true)]
        public DateTime? BillDate { get; set; }

        [SugarColumn(ColumnName = "CMAKER", Length = 50, IsNullable = true)]
        public string CMaker { get; set; }

        [SugarColumn(ColumnName = "orgCode", Length = 50, IsNullable = true)]
        public string OrgCode { get; set; }

        [SugarColumn(ColumnName = "cDepCode", Length = 50, IsNullable = true)]
        public string CDepCode { get; set; }

        [SugarColumn(ColumnName = "cNatBankAccount", Length = 100, IsNullable = true)]
        public string CNatBankAccount { get; set; }

        [SugarColumn(ColumnName = "cNatBank", Length = 100, IsNullable = true)]
        public string CNatBank { get; set; }

        [SugarColumn(ColumnName = "cSSName", Length = 50, IsNullable = true)]
        public string CSSName { get; set; }

        [SugarColumn(ColumnName = "settlestatus", IsNullable = true)]
        public int? SettleStatus { get; set; }

        [SugarColumn(ColumnName = "quickTypeName", Length = 100, IsNullable = true)]
        public string QuickTypeName { get; set; }

        [SugarColumn(ColumnName = "cVouchType", Length = 50, IsNullable = true)]
        public string CVouchType { get; set; }

        [SugarColumn(ColumnName = "cDwType", Length = 100, IsNullable = true)]
        public string CDwType { get; set; }

        [SugarColumn(ColumnName = "cDwCode", Length = 100, IsNullable = true)]
        public string CDwCode { get; set; }

        [SugarColumn(ColumnName = "iAmount", DecimalDigits = 4, Length = 18, IsNullable = true)]
        public decimal? IAmount { get; set; }

        [SugarColumn(ColumnName = "cNoteCode", Length = 100, IsNullable = true)]
        public string CNoteCode { get; set; }

        [SugarColumn(ColumnName = "tradetypeName", Length = 100, IsNullable = true)]
        public string TradetypeName { get; set; }

        [SugarColumn(ColumnName = "discountInterest", DecimalDigits = 4, Length = 18, IsNullable = true)]
        public decimal? DiscountInterest { get; set; }

        [SugarColumn(ColumnName = "noteTypeCode", Length = 100, IsNullable = true)]
        public string NoteTypeCode { get; set; }

        [SugarColumn(ColumnName = "createTime")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        [SugarColumn(ColumnName = "updateTime", IsNullable = true)]
        public DateTime? UpdateTime { get; set; }

        [SugarColumn(ColumnName = "processStatus")]
        public int ProcessStatus { get; set; }

        [SugarColumn(ColumnName = "processMsg", Length = 500, IsNullable = true)]
        public string ProcessMsg { get; set; }

        [SugarColumn(ColumnName = "u8code", Length = 100, IsNullable = true)]
        public string U8Code { get; set; }

        [SugarColumn(ColumnName = "SYNTime", IsNullable = true)]
        public DateTime? SynTime { get; set; }

        [SugarColumn(ColumnName = "cbank", Length = 200, IsNullable = true)]
        public string cbank { get; set; }

        [SugarColumn(ColumnName = "cbranch", Length = 200, IsNullable = true)]
        public string cbranch { get; set; }

        [SugarColumn(ColumnName = "caccountNum", Length = 200, IsNullable = true)]
        public string caccountNum { get; set; }

        [SugarColumn(ColumnName = "caccountName", Length = 200, IsNullable = true)]
        public string caccountName { get; set; }

        [SugarColumn(ColumnName = "crBankNo", Length = 200, IsNullable = true)]
        public string crBankNo { get; set; }

        [SugarColumn(ColumnName = "cdigest", Length = 200, IsNullable = true)]
        public string cdigest { get; set; }

        /// <summary>
        /// 票证/票据方向
        /// </summary>
        [SugarColumn(ColumnName = "receiptDirection", Length = 10, IsNullable = true)]
        public string ReceiptDirection { get; set; }

        /// <summary>
        /// 来源单据号
        /// </summary>
        [SugarColumn(ColumnName = "bizbillno", Length = 200, IsNullable = true)]
        public string bizbillno { get; set; }

        /// <summary>
        /// 客户编码
        /// </summary>
        [SugarColumn(ColumnName = "payerCode", Length = 100, IsNullable = true)]
        public string PayerCode { get; set; }
    }
}
