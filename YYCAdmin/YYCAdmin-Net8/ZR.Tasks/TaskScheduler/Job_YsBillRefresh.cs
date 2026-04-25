using Infrastructure.Attribute;
using Quartz;
using System.Threading.Tasks;
using ZR.Service.Business.IService;

namespace ZR.Tasks.TaskScheduler
{
    [AppService(ServiceType = typeof(Job_YsBillRefresh), ServiceLifetime = LifeTime.Scoped)]
    public class Job_YsBillRefresh : JobBase, IJob
    {
        private readonly IYsBillSyncService _ysBillSyncService;

        /// <summary>
        /// 初始化 YS 结算单刷新定时任务。
        /// </summary>
        /// <param name="ysBillSyncService">Quartz 任务执行时使用的同步服务。</param>
        public Job_YsBillRefresh(IYsBillSyncService ysBillSyncService)
        {
            _ysBillSyncService = ysBillSyncService;
        }

        /// <summary>
        /// 执行 Quartz 任务，并将任务参数透传给服务层的刷新逻辑。
        /// </summary>
        /// <param name="context">Quartz 执行上下文。</param>
        public async Task Execute(IJobExecutionContext context)
        {
            await ExecuteJob(context, async () =>
            {
                // 由服务层决定刷新全部待结算数据，还是按参数兼容旧配置执行。
                var jobParams = context.MergedJobDataMap.GetString("JobParam");
                return await _ysBillSyncService.RefreshAsync(jobParams);
            });
        }
    }
}
