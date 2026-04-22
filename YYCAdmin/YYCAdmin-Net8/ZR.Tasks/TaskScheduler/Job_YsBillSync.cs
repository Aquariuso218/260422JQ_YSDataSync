using Infrastructure.Attribute;
using Quartz;
using System.Threading.Tasks;
using ZR.Service.Business.IService;

namespace ZR.Tasks.TaskScheduler
{
    [AppService(ServiceType = typeof(Job_YsBillSync), ServiceLifetime = LifeTime.Scoped)]
    public class Job_YsBillSync : JobBase, IJob
    {
        private readonly IYsBillSyncService _ysBillSyncService;

        /// <summary>
        /// 初始化 YS 单据同步定时任务。
        /// </summary>
        /// <param name="ysBillSyncService">Quartz 任务执行时使用的同步服务。</param>
        public Job_YsBillSync(IYsBillSyncService ysBillSyncService)
        {
            _ysBillSyncService = ysBillSyncService;
        }

        /// <summary>
        /// 执行 Quartz 任务，并将任务参数透传给服务层。
        /// </summary>
        /// <param name="context">Quartz 执行上下文。</param>
        public async Task Execute(IJobExecutionContext context)
        {
            await ExecuteJob(context, async () =>
            {
                // 由服务层决定同步全部单据类型，还是仅同步某一种类型。
                var jobParams = context.MergedJobDataMap.GetString("JobParam");
                return await _ysBillSyncService.SyncAsync(jobParams);
            });
        }
    }
}
