# YS 中间表同步 U8 Service 结构设计

## 1. 背景

当前 `ZR.Service` 中已经具备两段流程能力：

1. `YS -> 中间表`：由 `IYsBillSyncService` / `YsBillSyncService` 的 `SyncAsync` 完成。
2. `YS -> 中间表状态刷新`：由 `IYsBillSyncService` / `YsBillSyncService` 的 `RefreshAsync` 完成。

同时，中间表实体 `ZR.Model\Business\Model\EF_MidYSBillData.cs` 已预留以下字段，明显用于后续向 U8 推送后的状态回写：

- `ProcessStatus`
- `ProcessMsg`
- `U8Code`
- `SynTime`
- `UpdateTime`

因此，本次需要在 service 层新增第三段流程的功能骨架：

- `中间表 -> U8`

用户将在该骨架基础上继续补充具体 U8 对接逻辑。

## 2. 本次目标

本次只完成“结构搭建”和“项目内职责优化”，不直接实现完整 U8 业务。

目标包括：

1. 新增独立的 U8 推送 service，而不是继续堆叠在现有 `YsBillSyncService` 中。
2. 顺手整理 `ZR.Service\Business` 下与 YS 同步相关的目录结构，使职责边界更清晰。
3. 为后续补充 U8 登录、请求组装、推送、状态回写、失败重试等逻辑预留稳定扩展点。
4. 保持现有 `SyncAsync` / `RefreshAsync` 行为与调用方式不变。
5. 保持中间表实体结构不变，不新增数据库字段。

## 3. 非目标

本次不做以下工作：

1. 不实现真实的 U8 接口调用。
2. 不修改数据库表结构。
3. 不调整现有任务中心中已经接入的 YS 拉取与刷新逻辑行为。
4. 不对无关模块进行重构。
5. 不引入超出本次需求范围的大规模公共抽象。

## 4. 现状问题

当前 `ZR.Service\Business` 结构比较扁平：

- `YsBillSyncService.cs` 位于 `ZR.Service\Business` 根下。
- 接口位于 `ZR.Service\Business\IService`。
- YS 相关 DTO、常量、客户端位于 `ZR.Service\Business\Ys`。

随着“中间表 -> U8”流程加入，如果继续沿用当前结构，会有两个问题：

1. `YsBillSyncService` 的职责继续膨胀，既处理 YS 拉取，又处理中间表刷新，再处理 U8 推送，后续维护成本高。
2. 目录组织无法直观表达“同一个 YS 领域下的多个 service 与支撑对象”的关系。

## 5. 设计方案

### 5.1 采用独立 Service 方案

采用方案 2：新增独立 `IYsBillToU8SyncService` / `YsBillToU8SyncService`。

原因：

1. 能让“来源系统同步”和“目标系统推送”职责彻底分开。
2. 后续补 U8 逻辑时，修改范围稳定，避免影响现有 YS 拉取流程。
3. 可单独配置 Quartz job，便于后续调度与排错。

### 5.2 目录结构优化

建议将 `ZR.Service\Business` 下与 YS 同步有关的内容统一收敛到 `Ys` 领域目录下，形成以下结构：

```text
ZR.Service/Business/Ys/
  Interfaces/
    IYsBillSyncService.cs
    IYsBillToU8SyncService.cs
  Services/
    YsBillSyncService.cs
    YsBillToU8SyncService.cs
  Dtos/
    ...
  Support/
    YsApiClient.cs
    YsBillSyncConstants.cs
    YsBillSyncDefinition.cs
    YsBillToU8SyncResult.cs
    YsBillToU8SyncContext.cs
```

说明：

- `Dtos` 保留现有 YS 接口契约对象。
- `Support` 放共用常量、定义、客户端、轻量上下文和结果对象。
- `Interfaces` 统一放领域 service 接口。
- `Services` 放领域 service 实现。

为控制改动风险：

1. 只整理 `ZR.Service\Business\Ys` 相关内容。
2. 不触碰无关 service。
3. 尽量以“迁移文件位置 + 调整 namespace + 最小必要改造”为主。

### 5.3 现有 Service 的职责边界

调整后职责如下：

- `YsBillSyncService`
  - 负责 `YS -> 中间表`
  - 负责中间表未完成数据的刷新
  - 对外保留：
    - `Task<string> SyncAsync(string jobParams = null)`
    - `Task<string> RefreshAsync(string jobParams = null)`

- `YsBillToU8SyncService`
  - 负责 `中间表 -> U8`
  - 对外新增：
    - `Task<string> SyncToU8Async(string jobParams = null)`

### 5.4 U8 推送 Service 的内部骨架

`YsBillToU8SyncService` 只搭建流程骨架，不写死真实 U8 逻辑。

建议拆分的方法边界如下：

1. `SyncToU8Async`
   - 总入口
   - 负责解析参数、拉取待同步数据、循环执行、汇总结果

2. `GetPendingRowsAsync`
   - 查询待处理的中间表记录
   - 第一版默认以 `ProcessStatus = 0` 为待处理条件
   - 未来可扩展失败重试、按组织过滤、按单据类型过滤等规则

3. `BuildSyncContextAsync`
   - 将 `EF_MidYSBillData` 收敛为内部同步上下文对象
   - 负责整理后续 U8 推送所需字段

4. `PushSingleAsync`
   - 单条推送执行入口
   - 第一版只保留占位实现，供后续填充真实 U8 调用逻辑

5. `MarkSuccessAsync`
   - 回写成功状态

6. `MarkFailureAsync`
   - 回写失败状态与错误摘要

7. `BuildSummary`
   - 返回任务中心可展示的汇总结果字符串

### 5.5 建议的内部模型

为了让后续扩展更自然，新增两个轻量内部模型：

#### `YsBillToU8SyncContext`

用于承接单条中间表数据的同步上下文，至少包含：

- 当前中间表记录 `EF_MidYSBillData`
- 规范化后的业务字段
- 预留的 U8 请求数据对象或载荷字段

#### `YsBillToU8SyncResult`

用于表达单条推送结果，至少包含：

- `bool IsSuccess`
- `string Message`
- `string U8Code`
- `bool ShouldRetry`

### 5.6 中间表状态约定

不新增表字段，仅固定现有字段用途：

- `ProcessStatus = 0`：待处理
- `ProcessStatus = 1`：同步成功
- `ProcessStatus = 2`：同步失败

字段用途：

- `ProcessMsg`
  - 保存最近一次处理结果说明或错误摘要
- `U8Code`
  - 保存 U8 返回的单号、编码或主键
- `SynTime`
  - 保存最后一次成功同步时间
- `UpdateTime`
  - 保存最后一次状态更新时间

### 5.7 现有 `YsBillSyncService` 的结构优化方式

本次不对现有逻辑做行为重写，只做结构优化。

优化原则：

1. 保留现有公开接口和返回语义。
2. 优先整理目录与 namespace。
3. 对内部私有方法按职责重新分组，提升可读性。
4. 如文件过长，可抽出共享支持类，但仅限本领域内复用能力。

可抽出的共用支持能力示例：

- 中间表行映射
- 组织编码查询
- 客户/供应商编码查询
- 通用响应成功校验

这些支持能力只用于减少 `YsBillSyncService` 的体积，不改变当前业务行为。

### 5.8 Quartz 任务层调整

新增独立任务入口：

- `ZR.Tasks\TaskScheduler\Job_YsBillToU8Sync.cs`

职责：

- 调用 `IYsBillToU8SyncService.SyncToU8Async(jobParams)`
- 与现有 `Job_YsBillSync`、`Job_YsBillRefresh` 风格保持一致
- 支持未来为 U8 推送单独配置调度周期

现有任务保持不变：

- `Job_YsBillSync`：YS 拉取
- `Job_YsBillRefresh`：中间表刷新

## 6. 实施范围

本次代码层面计划包含：

1. 新增 `IYsBillToU8SyncService`
2. 新增 `YsBillToU8SyncService`
3. 新增 `Job_YsBillToU8Sync`
4. 将现有 `IYsBillSyncService`、`YsBillSyncService` 迁移到新的 `Ys` 领域目录结构
5. 调整对应 namespace、using 和引用关系
6. 视需要新增轻量 `Context` / `Result` 支撑类
7. 保证项目可编译

## 7. 风险与控制

### 风险 1：迁移目录导致引用中断

控制方式：

- 只迁移少量与 YS 相关文件
- 每移动一类文件后同步修正 namespace 和 using
- 最后执行编译验证

### 风险 2：误改现有同步行为

控制方式：

- 不改 `SyncAsync` / `RefreshAsync` 的方法签名
- 不改现有业务判断逻辑
- 仅做必要的目录整理和轻量抽取

### 风险 3：U8 service 过早固化真实接口模型

控制方式：

- 第一版只提供流程骨架
- 不提前写死真实接口地址、鉴权流程和报文结构
- 将真实调用逻辑限制在 `PushSingleAsync` 及其后续扩展点中

## 8. 验收标准

完成后应满足：

1. `ZR.Service` 中形成清晰的 `Ys` 领域目录结构。
2. 存在独立的 `IYsBillToU8SyncService` 与 `YsBillToU8SyncService`。
3. 存在独立的 `Job_YsBillToU8Sync`。
4. 现有 `IYsBillSyncService` / `YsBillSyncService` 能继续承担 YS 拉取与刷新职责。
5. U8 同步 service 已具备待处理数据查询、上下文组装、单条推送、状态回写、结果汇总等扩展点。
6. 项目编译通过。
7. 不覆盖用户当前已存在的业务改动。

## 9. 后续实现建议

在本次骨架落地后，后续真实 U8 同步逻辑建议优先补充在以下位置：

1. `PushSingleAsync`：接入真实 U8 调用
2. `BuildSyncContextAsync`：补齐字段映射和报文转换
3. `GetPendingRowsAsync`：补失败重试和任务参数过滤
4. `MarkSuccessAsync` / `MarkFailureAsync`：结合实际返回语义细化状态说明