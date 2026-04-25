# YS Bill To U8 Service Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reorganize the YS business service area, add a dedicated middle-table-to-U8 synchronization service skeleton, and expose a separate Quartz job without changing the existing YS pull/refresh behavior.

**Architecture:** Consolidate the YS domain under `ZR.Service/Business/Ys`, keeping DTOs in place while moving interfaces, services, and support classes into focused subfolders. Keep `YsBillSyncService` responsible only for `YS -> 中间表` and refresh, add a new `YsBillToU8SyncService` for `中间表 -> U8`, and keep Quartz jobs thin wrappers over service contracts.

**Tech Stack:** .NET 8, Quartz, SqlSugar, PowerShell regression script

---

### Task 1: Extend the regression harness for the new structure

**Files:**
- Modify: `tests/ys-sync-regression.ps1`

- [ ] **Step 1: Add failing assertions for the new YS folder layout and U8 service skeleton**

```powershell
Assert-SourceContains 'ZR.Service\Business\Ys\Interfaces\IYsBillSyncService.cs' 'interface IYsBillSyncService' 'moved sync service contract'
Assert-SourceContains 'ZR.Service\Business\Ys\Services\YsBillSyncService.cs' 'class YsBillSyncService' 'moved sync service class'
Assert-SourceContains 'ZR.Service\Business\Ys\Interfaces\IYsBillToU8SyncService.cs' 'Task<string> SyncToU8Async(' 'U8 sync service contract'
Assert-SourceContains 'ZR.Service\Business\Ys\Services\YsBillToU8SyncService.cs' 'class YsBillToU8SyncService' 'U8 sync service class'
Assert-SourceContains 'ZR.Service\Business\Ys\Support\YsBillToU8SyncContext.cs' 'class YsBillToU8SyncContext' 'U8 sync context class'
Assert-SourceContains 'ZR.Service\Business\Ys\Support\YsBillToU8SyncResult.cs' 'class YsBillToU8SyncResult' 'U8 sync result class'
Assert-SourceContains 'ZR.Tasks\TaskScheduler\Job_YsBillToU8Sync.cs' 'class Job_YsBillToU8Sync' 'U8 sync job class'
```

- [ ] **Step 2: Remove the old flat-path assertions so the harness checks the new target layout instead of the current layout**

```powershell
# Replace these old checks:
# Assert-SourceContains 'ZR.Service\Business\YsBillSyncService.cs' 'class YsBillSyncService' 'sync service class'
# Assert-SourceContains 'ZR.Service\Business\IService\IYsBillSyncService.cs' 'Task<string> SyncAsync(' 'sync service contract'

# With these new checks:
Assert-SourceContains 'ZR.Service\Business\Ys\Services\YsBillSyncService.cs' 'Task<string> SyncAsync(' 'sync service implementation entrypoint'
Assert-SourceContains 'ZR.Service\Business\Ys\Interfaces\IYsBillSyncService.cs' 'Task<string> RefreshAsync(' 'sync service refresh contract'
```

- [ ] **Step 3: Run the regression harness and verify it fails on the not-yet-created files**

Run: `powershell -ExecutionPolicy Bypass -File tests/ys-sync-regression.ps1`
Expected: FAIL with missing-source-file messages for the new `Ys/Interfaces`, `Ys/Services`, `Ys/Support`, and `Job_YsBillToU8Sync.cs` paths

### Task 2: Reorganize the existing YS service files into the domain folders

**Files:**
- Create: `ZR.Service/Business/Ys/Interfaces/`
- Create: `ZR.Service/Business/Ys/Services/`
- Create: `ZR.Service/Business/Ys/Support/`
- Modify: `ZR.Service/Business/IService/IYsBillSyncService.cs`
- Modify: `ZR.Service/Business/YsBillSyncService.cs`
- Modify: `ZR.Service/Business/Ys/YsApiClient.cs`
- Modify: `ZR.Service/Business/Ys/YsBillSyncConstants.cs`
- Modify: `ZR.Service/Business/Ys/YsBillSyncDefinition.cs`
- Modify: `ZR.Service/Business/Ys/YsConfigOptions.cs`
- Modify: `ZR.Tasks/TaskScheduler/Job_YsBillSync.cs`
- Modify: `ZR.Tasks/TaskScheduler/Job_YsBillRefresh.cs`

- [ ] **Step 1: Move the existing sync interface into `Ys/Interfaces` and update its namespace only**

```csharp
using System.Threading.Tasks;

namespace ZR.Service.Business.Ys.Interfaces
{
    public interface IYsBillSyncService
    {
        Task<string> SyncAsync(string jobParams = null);
        Task<string> RefreshAsync(string jobParams = null);
    }
}
```

- [ ] **Step 2: Move the existing sync service into `Ys/Services` and switch it to the new interface/support namespaces without changing business logic**

```csharp
using ZR.Service.Business.Ys.Dtos;
using ZR.Service.Business.Ys.Interfaces;
using ZR.Service.Business.Ys.Support;

namespace ZR.Service.Business.Ys.Services
{
    [AppService(ServiceType = typeof(IYsBillSyncService), ServiceLifetime = LifeTime.Transient)]
    public class YsBillSyncService : IYsBillSyncService
    {
        // 保持现有 SyncAsync / RefreshAsync 和私有流程方法不变，只调整命名空间与文件落点。
    }
}
```

- [ ] **Step 3: Move the shared YS support files into `Ys/Support` and give them a consistent namespace**

```csharp
namespace ZR.Service.Business.Ys.Support
{
    public sealed class YsBillSyncDefinition
    {
        public string InterfaceName { get; }
        public string ListPath { get; }
        public string DetailPath { get; }
        public string BodyPropertyName { get; }
    }
}
```

- [ ] **Step 4: Update both existing Quartz jobs to use the new interface namespace and keep their behavior unchanged**

```csharp
using ZR.Service.Business.Ys.Interfaces;

namespace ZR.Tasks.TaskScheduler
{
    public class Job_YsBillRefresh : JobBase, IJob
    {
        private readonly IYsBillSyncService _ysBillSyncService;
    }
}
```

- [ ] **Step 5: Re-run the regression harness and verify the only remaining failures are the new U8 files and job**

Run: `powershell -ExecutionPolicy Bypass -File tests/ys-sync-regression.ps1`
Expected: FAIL only for `IYsBillToU8SyncService`, `YsBillToU8SyncService`, `YsBillToU8SyncContext`, `YsBillToU8SyncResult`, and `Job_YsBillToU8Sync`

### Task 3: Add the dedicated U8 sync contract and support models

**Files:**
- Create: `ZR.Service/Business/Ys/Interfaces/IYsBillToU8SyncService.cs`
- Create: `ZR.Service/Business/Ys/Support/YsBillToU8SyncContext.cs`
- Create: `ZR.Service/Business/Ys/Support/YsBillToU8SyncResult.cs`

- [ ] **Step 1: Add the new service interface with a single orchestration entrypoint**

```csharp
namespace ZR.Service.Business.Ys.Interfaces
{
    public interface IYsBillToU8SyncService
    {
        Task<string> SyncToU8Async(string jobParams = null);
    }
}
```

- [ ] **Step 2: Add the internal sync context model that wraps one pending middle-table row**

```csharp
using ZR.Model.Business.Model;

namespace ZR.Service.Business.Ys.Support
{
    public sealed class YsBillToU8SyncContext
    {
        public EF_MidYSBillData MidBillRow { get; init; }
        public string JobParams { get; init; }
        public string RequestBody { get; init; }
    }
}
```

- [ ] **Step 3: Add the internal sync result model for success/failure/state-writeback decisions**

```csharp
namespace ZR.Service.Business.Ys.Support
{
    public sealed class YsBillToU8SyncResult
    {
        public bool IsSuccess { get; init; }
        public string Message { get; init; }
        public string U8Code { get; init; }
        public bool ShouldRetry { get; init; }
    }
}
```

- [ ] **Step 4: Re-run the regression harness and verify only the U8 service implementation and Quartz job are still missing**

Run: `powershell -ExecutionPolicy Bypass -File tests/ys-sync-regression.ps1`
Expected: FAIL only for `YsBillToU8SyncService` and `Job_YsBillToU8Sync`

### Task 4: Implement the U8 sync service skeleton

**Files:**
- Create: `ZR.Service/Business/Ys/Services/YsBillToU8SyncService.cs`

- [ ] **Step 1: Add the failing implementation shell with the final public contract and injected repositories**

```csharp
using Infrastructure.Attribute;
using SqlSugar;
using SqlSugar.IOC;
using ZR.Model.Business.Model;
using ZR.Repository;
using ZR.Service.Business.Ys.Interfaces;
using ZR.Service.Business.Ys.Support;

namespace ZR.Service.Business.Ys.Services
{
    [AppService(ServiceType = typeof(IYsBillToU8SyncService), ServiceLifetime = LifeTime.Transient)]
    public class YsBillToU8SyncService : IYsBillToU8SyncService
    {
        private readonly ISqlSugarClient _db;
        private readonly BaseRepository<EF_MidYSBillData> _midBillRepository;

        public YsBillToU8SyncService()
        {
            _db = DbScoped.SugarScope.GetConnectionScope(0);
            _midBillRepository = new BaseRepository<EF_MidYSBillData>(_db);
        }
    }
}
```

- [ ] **Step 2: Implement the orchestration entrypoint, pending-row query, and summary builder exactly as the skeleton extension points**

```csharp
public async Task<string> SyncToU8Async(string jobParams = null)
{
    var pendingRows = await GetPendingRowsAsync(jobParams);
    if (pendingRows.Count == 0)
    {
        return "YS->U8同步: 无待处理数据";
    }

    var successCount = 0;
    var failureCount = 0;

    foreach (var row in pendingRows)
    {
        try
        {
            var context = await BuildSyncContextAsync(row, jobParams);
            var result = await PushSingleAsync(context);
            if (result.IsSuccess)
            {
                await MarkSuccessAsync(row, result);
                successCount++;
                continue;
            }

            await MarkFailureAsync(row, result.Message);
            failureCount++;
        }
        catch (Exception ex)
        {
            await MarkFailureAsync(row, ex.Message);
            failureCount++;
        }
    }

    return BuildSummary(pendingRows.Count, successCount, failureCount);
}
```

- [ ] **Step 3: Add the minimal helper methods and implement `PushSingleAsync` as a fixed first-pass failure result so the skeleton writes back a consistent "待补充 U8 同步逻辑" message**

```csharp
private Task<List<EF_MidYSBillData>> GetPendingRowsAsync(string jobParams)
{
    return _midBillRepository.Queryable()
        .Where(x => x.ProcessStatus == 0)
        .OrderBy(x => x.CreateTime, OrderByType.Asc)
        .ToListAsync();
}

private Task<YsBillToU8SyncContext> BuildSyncContextAsync(EF_MidYSBillData row, string jobParams)
{
    return Task.FromResult(new YsBillToU8SyncContext
    {
        MidBillRow = row,
        JobParams = jobParams,
        RequestBody = string.Empty
    });
}

private Task<YsBillToU8SyncResult> PushSingleAsync(YsBillToU8SyncContext context)
{
    return Task.FromResult(new YsBillToU8SyncResult
    {
        IsSuccess = false,
        Message = "待补充 U8 同步逻辑",
        U8Code = context.MidBillRow.U8Code,
        ShouldRetry = true
    });
}
```

- [ ] **Step 4: Add the status write-back helpers so the concrete U8 implementation has one place to update middle-table state**

```csharp
private async Task MarkSuccessAsync(EF_MidYSBillData row, YsBillToU8SyncResult result)
{
    row.ProcessStatus = 1;
    row.ProcessMsg = result.Message;
    row.U8Code = result.U8Code;
    row.SynTime = DateTime.Now;
    row.UpdateTime = DateTime.Now;
    await _db.Updateable(row).ExecuteCommandAsync();
}

private async Task MarkFailureAsync(EF_MidYSBillData row, string errorMessage)
{
    row.ProcessStatus = 2;
    row.ProcessMsg = errorMessage;
    row.UpdateTime = DateTime.Now;
    await _db.Updateable(row).ExecuteCommandAsync();
}
```

- [ ] **Step 5: Re-run the regression harness and verify the only remaining failure is the missing Quartz job**

Run: `powershell -ExecutionPolicy Bypass -File tests/ys-sync-regression.ps1`
Expected: FAIL only for `Job_YsBillToU8Sync`

### Task 5: Add the dedicated Quartz job and verify the solution build

**Files:**
- Create: `ZR.Tasks/TaskScheduler/Job_YsBillToU8Sync.cs`

- [ ] **Step 1: Implement the new Quartz job as a thin wrapper over `IYsBillToU8SyncService`**

```csharp
using Infrastructure.Attribute;
using Quartz;
using ZR.Service.Business.Ys.Interfaces;

namespace ZR.Tasks.TaskScheduler
{
    [AppService(ServiceType = typeof(Job_YsBillToU8Sync), ServiceLifetime = LifeTime.Scoped)]
    public class Job_YsBillToU8Sync : JobBase, IJob
    {
        private readonly IYsBillToU8SyncService _ysBillToU8SyncService;

        public Job_YsBillToU8Sync(IYsBillToU8SyncService ysBillToU8SyncService)
        {
            _ysBillToU8SyncService = ysBillToU8SyncService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await ExecuteJob(context, async () =>
            {
                var jobParams = context.MergedJobDataMap.GetString("JobParam");
                return await _ysBillToU8SyncService.SyncToU8Async(jobParams);
            });
        }
    }
}
```

- [ ] **Step 2: Run the regression harness and verify the source-layout checks all pass**

Run: `powershell -ExecutionPolicy Bypass -File tests/ys-sync-regression.ps1`
Expected: PASS

- [ ] **Step 3: Build the full solution to catch namespace and reference regressions after the file moves**

Run: `dotnet build ZRAdmin.sln`
Expected: exit code 0

- [ ] **Step 4: Record the new task-center configuration values for manual setup after deployment**

```text
AssemblyName = ZR.Tasks
ClassName = TaskScheduler.Job_YsBillToU8Sync
TaskType = 1
JobParams = (empty for the current default pending-row query)
```