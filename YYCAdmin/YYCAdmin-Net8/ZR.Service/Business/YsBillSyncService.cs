using Infrastructure.Attribute;
using ZR.Service.Business.IService;

namespace ZR.Service.Business
{
    [AppService(ServiceType = typeof(IYsBillSyncService), ServiceLifetime = LifeTime.Transient)]
    public class YsBillSyncService : IYsBillSyncService
    {
        private readonly IYsSettlementFetchService _ysSettlementFetchService;
        private readonly IYsBillInstrumentFetchService _ysBillInstrumentFetchService;
        private readonly IApCloseBillSyncService _apCloseBillSyncService;

        /// <summary>
        /// 初始化 YS 与 U8 同步门面服务。
        /// </summary>
        /// <param name="ysSettlementFetchService">YS 结算单抓取与刷新服务。</param>
        /// <param name="ysBillInstrumentFetchService">YS 票据业务抓取服务。</param>
        /// <param name="apCloseBillSyncService">中间表同步 U8 收付款单服务。</param>
        public YsBillSyncService(
            IYsSettlementFetchService ysSettlementFetchService,
            IYsBillInstrumentFetchService ysBillInstrumentFetchService,
            IApCloseBillSyncService apCloseBillSyncService)
        {
            _ysSettlementFetchService = ysSettlementFetchService;
            _ysBillInstrumentFetchService = ysBillInstrumentFetchService;
            _apCloseBillSyncService = apCloseBillSyncService;
        }

        /// <summary>
        /// 执行 YS 结算单同步。
        /// </summary>
        /// <param name="jobParams">任务中心界面透传的参数。</param>
        /// <returns>返回本次同步的简要结果字符串。</returns>
        public async Task<string> SyncAsync(string jobParams = null)
        {
            var typeValue = GetTypeValue(jobParams);
            if (string.IsNullOrWhiteSpace(typeValue) || string.Equals(typeValue, "all", StringComparison.OrdinalIgnoreCase))
            {
                var messages = new List<string>
                {
                    await _ysSettlementFetchService.SyncAsync(jobParams),
                    await _ysBillInstrumentFetchService.SyncAsync(jobParams)
                };

                return string.Join(" | ", messages.Where(x => !string.IsNullOrWhiteSpace(x)));
            }

            if (IsInstrumentType(typeValue))
            {
                return await _ysBillInstrumentFetchService.SyncAsync(jobParams);
            }

            return await _ysSettlementFetchService.SyncAsync(jobParams);
        }

        /// <summary>
        /// 刷新 YS 中间表中的结算状态。
        /// </summary>
        /// <param name="jobParams">任务中心界面透传的参数。</param>
        /// <returns>返回本次刷新任务的简要结果字符串。</returns>
        public Task<string> RefreshAsync(string jobParams = null)
        {
            var typeValue = GetTypeValue(jobParams);
            if (IsInstrumentType(typeValue))
            {
                return Task.FromResult("YS刷新: 当前类型不支持刷新");
            }

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

        /// <summary>
        /// 判断当前任务类型是否属于新增票据业务。
        /// </summary>
        private static bool IsInstrumentType(string typeValue)
        {
            return string.Equals(typeValue, "discount", StringComparison.OrdinalIgnoreCase)
                || string.Equals(typeValue, "expireCash", StringComparison.OrdinalIgnoreCase)
                || string.Equals(typeValue, "consignBank", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 读取任务参数中的 type 值。
        /// </summary>
        private static string GetTypeValue(string jobParams)
        {
            if (string.IsNullOrWhiteSpace(jobParams))
            {
                return string.Empty;
            }

            var parameters = jobParams.Split(['&', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

            return parameters.TryGetValue("type", out var typeValue) ? typeValue : string.Empty;
        }
    }
}
