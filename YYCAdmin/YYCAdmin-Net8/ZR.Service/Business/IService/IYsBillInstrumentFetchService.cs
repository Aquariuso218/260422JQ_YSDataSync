namespace ZR.Service.Business.IService
{
    /// <summary>
    /// YS 票据业务抓取服务。
    /// </summary>
    public interface IYsBillInstrumentFetchService
    {
        /// <summary>
        /// 抓取贴现办理、到期兑付、银行托收并写入中间表。
        /// </summary>
        /// <param name="jobParams">任务中心界面透传的参数。</param>
        /// <returns>返回本次抓取任务的简要结果。</returns>
        Task<string> SyncAsync(string jobParams = null);
    }
}
