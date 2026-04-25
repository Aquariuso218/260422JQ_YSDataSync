namespace ZR.Service.Business.IService
{
    /// <summary>
    /// YS 结算单抓取与刷新服务。
    /// </summary>
    public interface IYsSettlementFetchService
    {
        /// <summary>
        /// 执行 YS 结算单抓取并写入中间表。
        /// </summary>
        /// <param name="jobParams">任务中心界面透传的参数。</param>
        /// <returns>返回本次抓取任务的简要结果。</returns>
        Task<string> SyncAsync(string jobParams = null);

        /// <summary>
        /// 刷新中间表中的结算状态。
        /// </summary>
        /// <param name="jobParams">任务中心界面透传的参数。</param>
        /// <returns>返回本次刷新任务的简要结果。</returns>
        Task<string> RefreshAsync(string jobParams = null);
    }
}
