namespace ZR.Service.Business.IService
{
    /// <summary>
    /// 中间表同步 U8 收付款单服务。
    /// </summary>
    public interface IApCloseBillSyncService
    {
        /// <summary>
        /// 将满足条件的中间表数据同步到 U8 收付款单接口。
        /// </summary>
        /// <param name="jobParams">任务中心界面透传的参数。</param>
        /// <returns>返回本次 U8 同步任务的简要结果。</returns>
        Task<string> SyncAsync(string jobParams = null);
    }
}
