using Infrastructure.Attribute;
using Quartz;
using System.Threading.Tasks;
using ZR.Service.Business.IService;

namespace ZR.Tasks.TaskScheduler
{
    /// <summary>
    /// U8 资金期初余额同步定时任务
    /// </summary>
    [AppService(ServiceType = typeof(Job_U8fundsSync), ServiceLifetime = LifeTime.Scoped)]
    public class Job_U8fundsSync : JobBase, IJob
    {
        private readonly IU8fundsSyncService _u8fundsSyncService;

        /// <summary>
        /// 构造函数
        /// </summary>
        public Job_U8fundsSync(IU8fundsSyncService u8fundsSyncService)
        {
            _u8fundsSyncService = u8fundsSyncService;
        }

        /// <summary>
        /// 执行 Quartz 定时任务
        /// </summary>
        public async Task Execute(IJobExecutionContext context)
        {
            await ExecuteJob(context, async () =>
            {
                var jobParams = context.MergedJobDataMap.GetString("JobParam");
                return await _u8fundsSyncService.SyncAsync(jobParams);
            });
        }
    }
}
