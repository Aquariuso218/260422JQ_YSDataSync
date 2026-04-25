using Infrastructure.Attribute;
using Quartz;
using System.Threading.Tasks;
using ZR.Service.Business.IService;

namespace ZR.Tasks.TaskScheduler
{
    [AppService(ServiceType = typeof(Job_YsBillToU8Sync), ServiceLifetime = LifeTime.Scoped)]
    public class Job_YsBillToU8Sync : JobBase, IJob
    {
        private readonly IYsBillSyncService _ysBillSyncService;

        /// <summary>
        /// 初始化 YS 中间表同步 U8 收付款单定时任务。
        /// </summary>
        /// <param name="ysBillSyncService">Quartz 任务执行时使用的同步服务。</param>
        public Job_YsBillToU8Sync(IYsBillSyncService ysBillSyncService)
        {
            _ysBillSyncService = ysBillSyncService;
        }

        /// <summary>
        /// 执行 Quartz 任务，并将任务参数透传给服务层的 U8 同步逻辑。
        /// </summary>
        /// <param name="context">Quartz 执行上下文。</param>
        public async Task Execute(IJobExecutionContext context)
        {
            await ExecuteJob(context, async () =>
            {
                // 当前任务按中间表状态筛选待同步数据，任务参数预留后续扩展。
                var jobParams = context.MergedJobDataMap.GetString("JobParam");
                return await _ysBillSyncService.SyncToU8Async(jobParams);
            });
        }
    }
}
