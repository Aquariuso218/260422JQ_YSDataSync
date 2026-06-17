using Quartz.Spi;
using SqlSugar;
using SqlSugar.IOC;
using ZR.Model.System;
using ZR.Tasks;

namespace ZR.Admin.WebApi.Extensions
{
    /// <summary>
    /// 定时任务扩展方法
    /// </summary>
    public static class TasksExtension
    {
        /// <summary>
        /// 注册任务
        /// </summary>
        /// <param name="services"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddTaskSchedulers(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            //添加Quartz服务
            services.AddSingleton<IJobFactory, JobFactory>();
            services.AddTransient<ITaskSchedulerServer, TaskSchedulerServer>();
        }

        /// <summary>
        /// 程序启动后添加任务计划
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseAddTaskSchedulers(this IApplicationBuilder app)
        {
            ITaskSchedulerServer _schedulerServer = app.ApplicationServices.GetRequiredService<ITaskSchedulerServer>();

            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine("====== 开始异步加载启动定时任务 ======");
                    var tasks = await DbScoped.SugarScope.Queryable<SysTasks>()
                        .Where(m => m.IsStart == 1)
                        .ToListAsync();

                    if (tasks == null || tasks.Count == 0)
                    {
                        Console.WriteLine("未找到状态为已开启的定时任务。");
                        return;
                    }

                    foreach (var task in tasks)
                    {
                        var result = await _schedulerServer.AddTaskScheduleAsync(task);
                        if (result.IsSuccess())
                        {
                            Console.WriteLine($"[Success] 自动注册定时任务 [{task.Name}] (ID: {task.ID}) 成功");
                        }
                        else
                        {
                            result.TryGetValue("msg", out var msg);
                            Console.WriteLine($"[Fail] 自动注册定时任务 [{task.Name}] 失败，原因: {msg}");
                        }
                    }
                    Console.WriteLine("====== 异步加载启动定时任务结束 ======");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] 系统启动注册定时任务时抛出异常: {ex.Message}\n{ex.StackTrace}");
                }
            });

            return app;
        }

        /// <summary>
        /// 初始化字典
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseInit(this IApplicationBuilder app)
        {
            //Console.WriteLine("初始化字典数据...");
            var db = DbScoped.SugarScope;
            var types = db.Queryable<SysDictType>()
                .Where(it => it.Status == "0")
                .Select(it => it.DictType)
                .ToList();

            //上面有耗时操作写在Any上面，保证程序启动后只执行一次
            if (!db.ConfigQuery.Any())
            {
                foreach (var type in types)
                {
                    db.ConfigQuery.SetTable<SysDictData>(it => SqlFunc.ToString(it.DictValue), it => it.DictLabel, type, it => it.DictType == type);
                }
            }
            return app;
        }
    }
}
