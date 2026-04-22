namespace ZR.Model.Business.Model
{
    [SugarTable("EF_sysSyncLog")]
    [Tenant("0")]
    public class EF_sysSyncLog
    {
        [SugarColumn(ColumnName = "log_id", IsPrimaryKey = true, IsIdentity = true)]
        public int LogId { get; set; }

        [SugarColumn(ColumnName = "interface_name", Length = 50)]
        public string InterfaceName { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "sync_start_time")]
        public DateTime SyncStartTime { get; set; }

        [SugarColumn(ColumnName = "sync_end_time")]
        public DateTime SyncEndTime { get; set; }

        [SugarColumn(ColumnName = "total_records")]
        public int TotalRecords { get; set; }

        [SugarColumn(ColumnName = "sync_status")]
        public int SyncStatus { get; set; }

        [SugarColumn(ColumnName = "error_msg", IsNullable = true)]
        public string ErrorMsg { get; set; }

        [SugarColumn(ColumnName = "execute_time")]
        public DateTime ExecuteTime { get; set; } = DateTime.Now;
    }
}
