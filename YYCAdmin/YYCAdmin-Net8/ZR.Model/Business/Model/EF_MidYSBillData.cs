namespace ZR.Model.Business.Model
{
    [SugarTable("EF_MidYSBillData")]
    [Tenant("0")]
    public class EF_MidYSBillData
    {
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, Length = 50)]
        public string Id { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "mainid", Length = 50, IsNullable = true)]
        public string MainId { get; set; }

        [SugarColumn(ColumnName = "code", Length = 50, IsNullable = true)]
        public string Code { get; set; }

        [SugarColumn(ColumnName = "billDate", IsNullable = true)]
        public DateTime? BillDate { get; set; }

        [SugarColumn(ColumnName = "creator", Length = 50, IsNullable = true)]
        public string Creator { get; set; }

        [SugarColumn(ColumnName = "orgCode", Length = 50, IsNullable = true)]
        public string OrgCode { get; set; }

        [SugarColumn(ColumnName = "depcode", Length = 50, IsNullable = true)]
        public string DepCode { get; set; }

        [SugarColumn(ColumnName = "operatorCode", Length = 50, IsNullable = true)]
        public string OperatorCode { get; set; }

        [SugarColumn(ColumnName = "enterpriseBankAccountNo", Length = 100, IsNullable = true)]
        public string EnterpriseBankAccountNo { get; set; }

        [SugarColumn(ColumnName = "enterpriseBankAccountName", Length = 100, IsNullable = true)]
        public string EnterpriseBankAccountName { get; set; }

        [SugarColumn(ColumnName = "settleModeCode", Length = 50, IsNullable = true)]
        public string SettleModeCode { get; set; }

        [SugarColumn(ColumnName = "settlestatus", IsNullable = true)]
        public int? SettleStatus { get; set; }

        [SugarColumn(ColumnName = "quickTypeName", Length = 100, IsNullable = true)]
        public string QuickTypeName { get; set; }

        [SugarColumn(ColumnName = "cVouchType", Length = 50, IsNullable = true)]
        public string CVouchType { get; set; }

        [SugarColumn(ColumnName = "caobject", IsNullable = true)]
        public int? Caobject { get; set; }

        [SugarColumn(ColumnName = "objectCode", Length = 50, IsNullable = true)]
        public string ObjectCode { get; set; }

        [SugarColumn(ColumnName = "oriSum", DecimalDigits = 4, Length = 18, IsNullable = true)]
        public decimal? OriSum { get; set; }

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
    }
}
