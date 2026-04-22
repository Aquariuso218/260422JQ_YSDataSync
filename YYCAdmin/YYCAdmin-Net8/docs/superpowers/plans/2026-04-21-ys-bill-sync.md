# YS Bill Sync Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Quartz-driven YS bill synchronization flow that pulls four bill types from YS, filters and maps detail rows, upserts `EF_MidYSBillData`, and records each interface run in `EF_sysSyncLog`.

**Architecture:** Keep Quartz thin by adding a new `ZR.Tasks.TaskScheduler.Job_YsBillSync` entrypoint that resolves a dedicated `ZR.Service` synchronization service. Put YS auth, API calls, sync definitions, DTOs, filtering, mapping, upsert, and sync log writes in the service layer so the existing task center can trigger the new job as a standard assembly task.

**Tech Stack:** .NET 8, Quartz, SqlSugar, Newtonsoft.Json

---

### Task 1: Create the regression harness

**Files:**
- Create: `tests/YsSyncTests/YsSyncTests.csproj`
- Create: `tests/YsSyncTests/Program.cs`
- Modify: `ZRAdmin.sln`

- [ ] **Step 1: Write the failing test harness**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\ZR.Model\ZR.Model.csproj" />
    <ProjectReference Include="..\..\ZR.Service\ZR.Service.csproj" />
    <ProjectReference Include="..\..\ZR.Tasks\ZR.Tasks.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Add reflection-based failing tests for the new sync contract**

```csharp
AssertType("ZR.Tasks.TaskScheduler.Job_YsBillSync, ZR.Tasks");
AssertType("ZR.Service.Business.YsBillSyncService, ZR.Service");
AssertMethod("ZR.Service.Business.IService.IYsBillSyncService, ZR.Service", "SyncAsync");
AssertType("ZR.Model.Business.EF_MidYSBillData, ZR.Model");
AssertType("ZR.Model.Business.EF_sysSyncLog, ZR.Model");
```

- [ ] **Step 3: Run the harness and verify it fails**

Run: `dotnet run --project tests/YsSyncTests/YsSyncTests.csproj`
Expected: FAIL because the new types and method do not exist yet

- [ ] **Step 4: Add the test project to the solution**

Run: `dotnet sln ZRAdmin.sln add tests/YsSyncTests/YsSyncTests.csproj`
Expected: project added successfully

### Task 2: Add the sync entities and DTO contracts

**Files:**
- Create: `ZR.Model/Business/EF_MidYSBillData.cs`
- Create: `ZR.Model/Business/EF_sysSyncLog.cs`
- Create: `ZR.Service/Business/IService/IYsBillSyncService.cs`
- Create: `ZR.Service/Business/Ys/Dtos/YsTokenResponseDto.cs`
- Create: `ZR.Service/Business/Ys/Dtos/YsApiEnvelopeDto.cs`
- Create: `ZR.Service/Business/Ys/Dtos/YsBillListResponseDto.cs`
- Create: `ZR.Service/Business/Ys/Dtos/YsBillDetailDto.cs`

- [ ] **Step 1: Define the two SqlSugar entities with table/column mappings**

```csharp
[SugarTable("EF_MidYSBillData")]
public class EF_MidYSBillData
{
    [SugarColumn(IsPrimaryKey = true, Length = 50)]
    public string Id { get; set; } = string.Empty;
    public string? HeadId { get; set; }
    public string? Code { get; set; }
    public DateTime? BillDate { get; set; }
    public string? CreatorUserName { get; set; }
    public string? OrgCode { get; set; }
    public string? DepCode { get; set; }
    public string? StaffCode { get; set; }
    public string? EnterpriseBankAccountNo { get; set; }
    public string? EnterpriseBankAccountName { get; set; }
    public string? SettleModeCode { get; set; }
    public int? SettleState { get; set; }
    public string? QuickTypeName { get; set; }
    public string? CVouchType { get; set; }
    public string? ObjectCode { get; set; }
    public string? ObjectName { get; set; }
    public string? ObjectBankAccountName { get; set; }
    public string? ObjectBankAccountNo { get; set; }
    public decimal? LocalTaxIncludedAmount { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public int ProcessStatus { get; set; }
    public string? ProcessMsg { get; set; }
    public string? U8Code { get; set; }
    public DateTime? SynTime { get; set; }
}
```

- [ ] **Step 2: Define the sync log entity and the service interface**

```csharp
[SugarTable("EF_sysSyncLog")]
public class EF_sysSyncLog
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int LogId { get; set; }
    public string InterfaceName { get; set; } = string.Empty;
    public DateTime SyncStartTime { get; set; }
    public DateTime SyncEndTime { get; set; }
    public int TotalRecords { get; set; }
    public int SyncStatus { get; set; }
    public string? ErrorMsg { get; set; }
    public DateTime ExecuteTime { get; set; }
}

public interface IYsBillSyncService
{
    Task<string> SyncAsync(string? jobParams = null);
}
```

- [ ] **Step 3: Define only the YS DTO fields the sync uses**

```csharp
public class YsBillListRecordDto
{
    [JsonProperty("id")]
    public string? Id { get; set; }
}

public class YsBillBodyItemDto
{
    [JsonProperty("id")]
    public string? Id { get; set; }
    [JsonProperty("headerId")]
    public string? HeaderId { get; set; }
    [JsonProperty("deptCode")]
    public string? DeptCode { get; set; }
    [JsonProperty("customerCode")]
    public string? CustomerCode { get; set; }
    [JsonProperty("supplierCode")]
    public string? SupplierCode { get; set; }
    [JsonProperty("localTaxIncludedAmount")]
    public decimal? LocalTaxIncludedAmount { get; set; }
}
```

- [ ] **Step 4: Run the harness and verify entity/interface tests pass or fail only on remaining missing types**

Run: `dotnet run --project tests/YsSyncTests/YsSyncTests.csproj`
Expected: still FAIL, but only for missing job/service implementation types

### Task 3: Implement YS client and sync definitions

**Files:**
- Create: `ZR.Service/Business/Ys/YsApiClient.cs`
- Create: `ZR.Service/Business/Ys/YsBillSyncDefinition.cs`
- Create: `ZR.Service/Business/Ys/YsBillSyncConstants.cs`
- Test: `tests/YsSyncTests/Program.cs`

- [ ] **Step 1: Add a failing test for the four bill definitions**

```csharp
var definitionsType = AssertType("ZR.Service.Business.Ys.YsBillSyncDefinition, ZR.Service");
var allField = definitionsType.GetField("All", BindingFlags.Public | BindingFlags.Static);
var all = (IEnumerable<object>)allField!.GetValue(null)!;
AssertEqual(4, all.Count(), "YS bill definitions count");
```

- [ ] **Step 2: Run the harness and verify the definitions test fails**

Run: `dotnet run --project tests/YsSyncTests/YsSyncTests.csproj`
Expected: FAIL because `YsBillSyncDefinition` is missing

- [ ] **Step 3: Implement the YS constants, definitions, and API client**

```csharp
public sealed class YsBillSyncDefinition
{
    public static readonly IReadOnlyList<YsBillSyncDefinition> All = new[]
    {
        new YsBillSyncDefinition("YS_Payment", "/yonbip/EFI/payment/list", "/yonbip/EFI/payment/detail", "付款单", "supplier"),
        new YsBillSyncDefinition("YS_PaymentRefund", "/yonbip/EFI/paymentRefund/list", "/yonbip/EFI/paymentRefund/detail", "付款退款单", "supplier"),
        new YsBillSyncDefinition("YS_Receipt", "/yonbip/EFI/ar/list", "/yonbip/EFI/ar/detail", "收款单", "customer"),
        new YsBillSyncDefinition("YS_ReceiptRefund", "/yonbip/EFI/arRefund/list", "/yonbip/EFI/arRefund/detail", "收款退款单", "customer"),
    };
}
```

- [ ] **Step 4: Re-run the harness**

Run: `dotnet run --project tests/YsSyncTests/YsSyncTests.csproj`
Expected: FAIL only on the remaining missing sync service and Quartz job

### Task 4: Implement the sync service

**Files:**
- Create: `ZR.Service/Business/YsBillSyncService.cs`
- Modify: `tests/YsSyncTests/Program.cs`

- [ ] **Step 1: Add a failing test for parameter parsing and supported bill type selection**

```csharp
var serviceType = AssertType("ZR.Service.Business.YsBillSyncService, ZR.Service");
var parseMethod = serviceType.GetMethod("ResolveDefinitions", BindingFlags.NonPublic | BindingFlags.Static);
var result = (IReadOnlyCollection<object>)parseMethod!.Invoke(null, new object?[] { "type=payment" })!;
AssertEqual(1, result.Count, "single payment definition");
```

- [ ] **Step 2: Run the harness and verify the service test fails**

Run: `dotnet run --project tests/YsSyncTests/YsSyncTests.csproj`
Expected: FAIL because `YsBillSyncService` is missing

- [ ] **Step 3: Implement the service with the sync template**

```csharp
public async Task<string> SyncAsync(string? jobParams = null)
{
    var summaries = new List<string>();
    foreach (var definition in ResolveDefinitions(jobParams))
    {
        var summary = await SyncOneAsync(definition);
        summaries.Add(summary);
    }
    return string.Join(" | ", summaries);
}
```

- [ ] **Step 4: Implement `SyncOneAsync` to query last success log, page list ids, dedupe, fetch detail, filter, map, upsert, and record sync log**

```csharp
var begin = await GetLastSuccessEndTimeAsync(definition.InterfaceName) ?? DateTime.Now.AddDays(-7);
var end = DateTime.Now;
var ids = await CollectIdsAsync(definition, begin, end);
var rows = await BuildRowsAsync(definition, ids);
await UpsertRowsAsync(rows);
await InsertSyncLogAsync(definition.InterfaceName, begin, end, rows.Count, 1, null);
```

- [ ] **Step 5: Re-run the harness**

Run: `dotnet run --project tests/YsSyncTests/YsSyncTests.csproj`
Expected: FAIL only on the missing Quartz job

### Task 5: Implement the Quartz job

**Files:**
- Create: `ZR.Tasks/TaskScheduler/Job_YsBillSync.cs`
- Modify: `tests/YsSyncTests/Program.cs`

- [ ] **Step 1: Add a failing test for the Quartz job type**

```csharp
var jobType = AssertType("ZR.Tasks.TaskScheduler.Job_YsBillSync, ZR.Tasks");
AssertTrue(typeof(IJob).IsAssignableFrom(jobType), "Job_YsBillSync implements Quartz.IJob");
```

- [ ] **Step 2: Run the harness and verify the job test fails**

Run: `dotnet run --project tests/YsSyncTests/YsSyncTests.csproj`
Expected: FAIL because `Job_YsBillSync` is missing

- [ ] **Step 3: Implement the Quartz job as a thin wrapper**

```csharp
[AppService(ServiceType = typeof(Job_YsBillSync), ServiceLifetime = LifeTime.Scoped)]
public class Job_YsBillSync : JobBase, IJob
{
    private readonly IYsBillSyncService _syncService;

    public Job_YsBillSync(IYsBillSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await ExecuteJob(context, async () =>
        {
            var jobParams = context.MergedJobDataMap.GetString("JobParam");
            return await _syncService.SyncAsync(jobParams);
        });
    }
}
```

- [ ] **Step 4: Re-run the harness**

Run: `dotnet run --project tests/YsSyncTests/YsSyncTests.csproj`
Expected: PASS

### Task 6: Build the solution and verify integration

**Files:**
- Modify: any files needed from previous tasks

- [ ] **Step 1: Build the full solution**

Run: `dotnet build ZRAdmin.sln`
Expected: exit code 0

- [ ] **Step 2: Re-run the regression harness after the build**

Run: `dotnet run --project tests/YsSyncTests/YsSyncTests.csproj`
Expected: PASS

- [ ] **Step 3: Record the final task configuration for manual setup**

```text
AssemblyName = ZR.Tasks
ClassName = TaskScheduler.Job_YsBillSync
TaskType = 1
JobParams = (empty for all, or type=payment/paymentRefund/receipt/receiptRefund)
```
