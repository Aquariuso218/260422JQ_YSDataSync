using Infrastructure.Attribute;
using ZR.Service.Business.IService;

namespace ZR.Service.Business
{
    [AppService(ServiceType = typeof(IYsBillSyncService), ServiceLifetime = LifeTime.Transient)]
    public class YsBillSyncService : IYsBillSyncService
    {
        private readonly IYsSettlementFetchService _ysSettlementFetchService;
        private readonly IApCloseBillSyncService _apCloseBillSyncService;

        /// <summary>
        /// 初始化 YS 与 U8 同步门面服务。
        /// </summary>
        /// <param name="ysSettlementFetchService">YS 结算单抓取与刷新服务。</param>
        /// <param name="apCloseBillSyncService">中间表同步 U8 收付款单服务。</param>
        public YsBillSyncService(
            IYsSettlementFetchService ysSettlementFetchService,
            IApCloseBillSyncService apCloseBillSyncService)
        {
            _ysSettlementFetchService = ysSettlementFetchService;
            _apCloseBillSyncService = apCloseBillSyncService;
        }

        /// <summary>
        /// 执行 YS 结算单同步。
        /// </summary>
        /// <param name="jobParams">任务中心界面透传的参数。</param>
        /// <returns>返回本次同步的简要结果字符串。</returns>
        public Task<string> SyncAsync(string jobParams = null)
        {
            return _ysSettlementFetchService.SyncAsync(jobParams);
        }

        /// <summary>
        /// 刷新 YS 中间表中的结算状态。
        /// </summary>
        /// <param name="jobParams">任务中心界面透传的参数。</param>
        /// <returns>返回本次刷新任务的简要结果字符串。</returns>
        public Task<string> RefreshAsync(string jobParams = null)
        {
            return _ysSettlementFetchService.RefreshAsync(jobParams);
        }

        /// <summary>
        /// 执行中间表到 U8 收付款单的同步。
        /// </summary>
        /// <param name="jobParams">任务中心界面透传的参数。</param>
        /// <returns>返回本次 U8 同步任务的简要结果字符串。</returns>
        public Task<string> SyncToU8Async(string jobParams = null)
        {
            return _apCloseBillSyncService.SyncAsync(jobParams);
        }
    }
}
