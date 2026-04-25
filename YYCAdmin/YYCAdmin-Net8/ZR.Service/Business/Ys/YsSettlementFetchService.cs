using Infrastructure.Attribute;
using SqlSugar;
using SqlSugar.IOC;
using ZR.Model.Business.Model;
using ZR.Repository;
using ZR.Service.Business.IService;
using ZR.Service.Business.Ys.Dtos;

namespace ZR.Service.Business.Ys
{
    [AppService(ServiceType = typeof(IYsSettlementFetchService), ServiceLifetime = LifeTime.Transient)]
    public class YsSettlementFetchService : IYsSettlementFetchService
    {
        private const string SyncStatusValue = "2";
        private const string CustomerTypeValue = "1";
        private const string VendorTypeValue = "2";

        private static readonly HashSet<string> AllowedTradeTypesForOtherCounterparties =
        [
            "贴现办理",
            "到期兑付",
            "银行托收"
        ];

        private readonly YsApiClient _apiClient;
        private readonly ISqlSugarClient _db;
        private readonly BaseRepository<EF_MidYSBillData> _midBillRepository;
        private readonly BaseRepository<EF_sysSyncLog> _syncLogRepository;
        private readonly Dictionary<string, string> _orgCodeCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _customerCodeCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _vendorCodeCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 初始化 YS 结算单抓取服务。
        /// </summary>
        /// <param name="httpClientFactory">HTTP 客户端工厂。</param>
        public YsSettlementFetchService(IHttpClientFactory httpClientFactory)
        {
            _apiClient = new YsApiClient(httpClientFactory);
            _db = DbScoped.SugarScope.GetConnectionScope(0);
            _midBillRepository = new BaseRepository<EF_MidYSBillData>(_db);
            _syncLogRepository = new BaseRepository<EF_sysSyncLog>(_db);
        }

        /// <summary>
        /// 执行一个或多个 YS 结算单同步定义。
        /// </summary>
        /// <param name="jobParams">任务中心界面透传的参数。</param>
        /// <returns>返回本次同步的简要结果字符串。</returns>
        public async Task<string> SyncAsync(string jobParams = null)
        {
            var messages = new List<string>();
            foreach (var definition in ResolveDefinitions(jobParams))
            {
                messages.Add(await SyncSingleDefinitionAsync(definition));
            }

            return string.Join(" | ", messages);
        }

        /// <summary>
        /// 刷新中间表中未结算完成的数据。
        /// </summary>
        /// <param name="jobParams">任务中心界面透传的参数。</param>
        /// <returns>返回本次刷新任务的简要结果字符串。</returns>
        public async Task<string> RefreshAsync(string jobParams = null)
        {
            try
            {
                var definition = ResolveDefinitions(jobParams).First();
                var pendingRows = await _midBillRepository.Queryable()
                    .Where(x => (x.SettleStatus == null || x.SettleStatus != 3)
                    && (x.TradetypeName != "贴现办理" || x.TradetypeName != "到期兑付" || x.TradetypeName != "银行托收")
                    )
                    .ToListAsync();

                if (pendingRows.Count == 0)
                {
                    return "YS刷新: 无待刷新数据";
                }

                var accessToken = await _apiClient.GetAccessTokenAsync();
                var rowsToUpdate = await BuildRefreshRowsAsync(definition, accessToken, pendingRows);
                if (rowsToUpdate.Count == 0)
                {
                    return $"YS刷新: 检查{pendingRows.Count}条, 更新0条";
                }

                _db.Ado.BeginTran();
                foreach (var row in rowsToUpdate)
                {
                    await _db.Updateable(row).ExecuteCommandAsync();
                }

                _db.Ado.CommitTran();
                return $"YS刷新: 检查{pendingRows.Count}条, 更新{rowsToUpdate.Count}条";
            }
            catch (Exception ex)
            {
                _db.Ado.RollbackTran();
                return $"YS刷新: 失败 - {ex.Message}";
            }
        }

        /// <summary>
        /// 执行单个结算单定义的完整同步流程。
        /// </summary>
        private async Task<string> SyncSingleDefinitionAsync(YsBillSyncDefinition definition)
        {
            var syncEndTime = DateTime.Now;
            var syncStartTime = await GetLastSuccessEndTimeAsync(definition.InterfaceName)
                ?? syncEndTime.AddDays(-YsBillSyncConstants.FirstSyncFallbackDays);

            try
            {
                var accessToken = await _apiClient.GetAccessTokenAsync();
                var ids = await CollectBillIdsAsync(definition, accessToken, syncStartTime, syncEndTime);
                var rows = await BuildMidRowsAsync(definition, accessToken, ids);

                _db.Ado.BeginTran();
                await UpsertMidRowsAsync(rows);
                await InsertSyncLogAsync(definition.InterfaceName, syncStartTime, syncEndTime, rows.Count, 1, null);
                _db.Ado.CommitTran();

                return $"{definition.InterfaceName}: synced {rows.Count} rows";
            }
            catch (Exception ex)
            {
                _db.Ado.RollbackTran();
                await InsertFailureLogSafelyAsync(definition.InterfaceName, syncStartTime, syncEndTime, ex);
                return $"{definition.InterfaceName}: failed - {ex.Message}";
            }
        }

        /// <summary>
        /// 读取最近一次成功同步日志的结束时间。
        /// </summary>
        private async Task<DateTime?> GetLastSuccessEndTimeAsync(string interfaceName)
        {
            var log = await _syncLogRepository.Queryable()
                .Where(x => x.InterfaceName == interfaceName && x.SyncStatus == 1)
                .OrderBy(x => x.SyncEndTime, OrderByType.Desc)
                .FirstAsync();

            return log?.SyncEndTime;
        }

        /// <summary>
        /// 拉取分页列表数据，并收集后续详情查询所需的单据 Id。
        /// </summary>
        private async Task<HashSet<string>> CollectBillIdsAsync(
            YsBillSyncDefinition definition,
            string accessToken,
            DateTime beginTime,
            DateTime endTime)
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pageIndex = 1;
            int pageCount;

            do
            {
                var request = BuildListRequest(pageIndex, beginTime, endTime);
                var response = await _apiClient.PostAsync<YsApiResponseDto<YsBillListDataDto>>(definition.ListPath, request, accessToken);

                EnsureSuccess(response, $"{definition.InterfaceName} list");
                var page = response.Data ?? new YsBillListDataDto();
                pageCount = page.PageCount > 0
                    ? page.PageCount
                    : (page.PageSize <= 0 ? 1 : (int)Math.Ceiling(page.RecordCount / (double)page.PageSize));
                pageCount = Math.Max(pageCount, 1);

                foreach (var record in page.RecordList ?? [])
                {
                    if (!string.IsNullOrWhiteSpace(record.Id))
                    {
                        ids.Add(record.Id);
                    }
                }

                pageIndex++;
            } while (pageIndex <= pageCount);

            return ids;
        }

        /// <summary>
        /// 构造结算单列表查询请求体。
        /// </summary>
        private static YsBillListRequestDto BuildListRequest(int pageIndex, DateTime beginTime, DateTime endTime)
        {
            return new YsBillListRequestDto
            {
                PageIndex = pageIndex,
                PageSize = 100,
                OpenVouchdateBegin = beginTime.ToString("yyyy-MM-dd"),
                OpenVouchdateEnd = endTime.ToString("yyyy-MM-dd"),
                SimpleVOs =
                [
                    new YsSimpleQueryConditionDto
                    {
                        Field = "verifystate",
                        Op = "eq",
                        Value1 = SyncStatusValue
                    }
                ]
            };
        }

        /// <summary>
        /// 拉取单据详情，并映射为中间表数据行。
        /// </summary>
        private async Task<List<EF_MidYSBillData>> BuildMidRowsAsync(
            YsBillSyncDefinition definition,
            string accessToken,
            IEnumerable<string> ids)
        {
            var rows = new List<EF_MidYSBillData>();

            foreach (var id in ids)
            {
                var response = await _apiClient.GetAsync<YsApiResponseDto<YsBillDetailDto>>(
                    definition.DetailPath,
                    new Dictionary<string, string> { ["id"] = id },
                    accessToken);

                EnsureSuccess(response, $"{definition.InterfaceName} detail");
                var detail = response.Data;
                if (detail == null)
                {
                    continue;
                }

                var bodyItems = GetBodyItems(definition, detail)
                    .Where(ShouldSyncBodyItem)
                    .ToList();
                var newItems = await FilterExistingBodyItemsAsync(bodyItems);
                foreach (var item in newItems)
                {
                    var row = await MapRowAsync(detail, item, accessToken);
                    if (row != null)
                    {
                        rows.Add(row);
                    }
                }
            }

            return rows;
        }

        /// <summary>
        /// 过滤掉已经存在于中间表中的表体数据。
        /// </summary>
        private async Task<List<YsBillBodyItemDto>> FilterExistingBodyItemsAsync(List<YsBillBodyItemDto> items)
        {
            if (items.Count == 0)
            {
                return items;
            }

            var itemIds = items
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .Select(x => x.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (itemIds.Count == 0)
            {
                return [];
            }

            var existingIds = await _midBillRepository.Queryable()
                .Where(x => itemIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            if (existingIds.Count == 0)
            {
                return items;
            }

            var existingIdSet = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return items
                .Where(x => !string.IsNullOrWhiteSpace(x.Id) && !existingIdSet.Contains(x.Id))
                .ToList();
        }

        /// <summary>
        /// 生成未结算数据需要刷新的最新中间表行。
        /// </summary>
        private async Task<List<EF_MidYSBillData>> BuildRefreshRowsAsync(
            YsBillSyncDefinition definition,
            string accessToken,
            List<EF_MidYSBillData> pendingRows)
        {
            var rowsToUpdate = new List<EF_MidYSBillData>();
            var detailCache = new Dictionary<string, YsBillDetailDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var pendingRow in pendingRows)
            {
                if (string.IsNullOrWhiteSpace(pendingRow.MainId) || string.IsNullOrWhiteSpace(pendingRow.Id))
                {
                    continue;
                }

                var detail = await GetCachedDetailAsync(definition, pendingRow.MainId, accessToken, detailCache);
                if (detail == null)
                {
                    continue;
                }

                var item = GetBodyItems(definition, detail)
                    .Where(ShouldSyncBodyItem)
                    .FirstOrDefault(x => string.Equals(x.Id, pendingRow.Id, StringComparison.OrdinalIgnoreCase));
                if (item == null)
                {
                    continue;
                }

                var latestRow = await MapRowAsync(detail, item, accessToken);
                if (latestRow == null || !NeedsRefreshUpdate(pendingRow, latestRow))
                {
                    continue;
                }

                latestRow.CreateTime = pendingRow.CreateTime;
                latestRow.ProcessStatus = pendingRow.ProcessStatus;
                latestRow.ProcessMsg = pendingRow.ProcessMsg;
                latestRow.U8Code = pendingRow.U8Code;
                latestRow.SynTime = pendingRow.SynTime;
                latestRow.UpdateTime = DateTime.Now;
                rowsToUpdate.Add(latestRow);
            }

            return rowsToUpdate;
        }

        /// <summary>
        /// 读取并缓存指定主表 Id 的详情数据。
        /// </summary>
        private async Task<YsBillDetailDto> GetCachedDetailAsync(
            YsBillSyncDefinition definition,
            string mainId,
            string accessToken,
            Dictionary<string, YsBillDetailDto> detailCache)
        {
            var cacheKey = $"{definition.InterfaceName}:{mainId}";
            if (detailCache.TryGetValue(cacheKey, out var cachedDetail))
            {
                return cachedDetail;
            }

            var response = await _apiClient.GetAsync<YsApiResponseDto<YsBillDetailDto>>(
                definition.DetailPath,
                new Dictionary<string, string> { ["id"] = mainId },
                accessToken);

            EnsureSuccess(response, $"{definition.InterfaceName} detail");
            var detail = response.Data;
            detailCache[cacheKey] = detail;
            return detail;
        }

        /// <summary>
        /// 根据当前详情类型，获取对应的表体集合。
        /// </summary>
        private static IEnumerable<YsBillBodyItemDto> GetBodyItems(YsBillSyncDefinition definition, YsBillDetailDto detail)
        {
            return definition.BodyPropertyName switch
            {
                "settleBench_b" => detail.SettleBenchBody ?? [],
                _ => []
            };
        }

        /// <summary>
        /// 判断当前表体行是否允许同步到中间表。
        /// </summary>
        private static bool ShouldSyncBodyItem(YsBillBodyItemDto item)
        {
            if ((string.Equals(item.CounterpartyType, CustomerTypeValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.CounterpartyType, VendorTypeValue, StringComparison.OrdinalIgnoreCase)) && ! AllowedTradeTypesForOtherCounterparties.Contains(item.TradetypeName)) 
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 将一条 YS 详情表体数据映射为同步实体。
        /// </summary>
        private async Task<EF_MidYSBillData> MapRowAsync(YsBillDetailDto detail, YsBillBodyItemDto item, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                return null;
            }

            var orgCode = await GetOrgCodeAsync(item.Org, accessToken);
            var dwCode = await GetDwCodeAsync(item.CounterpartyType, item.CounterpartyId, accessToken);

            return new EF_MidYSBillData
            {
                Id = item.Id,
                MainId = NormalizeString(item.MainId ?? detail.Id),
                CVouchCode = NormalizeString(detail.Code),
                BillDate = detail.BillDate ?? DateTime.MinValue,
                CMaker = NormalizeString(detail.Creator),
                OrgCode = orgCode,
                CDepCode = NormalizeString(item.DeptCode),
                CNatBankAccount = NormalizeString(item.OurBankAccount),
                CNatBank = NormalizeString(item.OurBankName),
                CSSName = NormalizeString(item.SettleModeName),
                SettleStatus = item.SettleStatus ?? 0,
                QuickTypeName = NormalizeString(item.QuickTypeName),
                CVouchType = ConvertVouchType(item.ReceiptTypeBody ?? item.ReceiptType),
                CDwType = ConvertDwType(item.CounterpartyType),
                CDwCode = dwCode,
                IAmount = item.OriginalCurrencyAmount ?? item.SuccessAmount ?? 0m,
                CNoteCode = NormalizeString(item.NoteCode),
                TradetypeName = NormalizeString(item.TradetypeName)
            };
        }

        /// <summary>
        /// 将收付款类型转换为中间表单据类型名称。
        /// </summary>
        private static string ConvertVouchType(int? receiptType)
        {
            return receiptType switch
            {
                1 => "资金收款",
                2 => "资金付款",
                _ => string.Empty
            };
        }

        /// <summary>
        /// 将对象类型编码转换为中间表使用的对象类型名称。
        /// </summary>
        private static string ConvertDwType(string counterpartyType)
        {
            return counterpartyType switch
            {
                CustomerTypeValue => "客户",
                VendorTypeValue => "供应商",
                _ => NormalizeString(counterpartyType)
            };
        }

        /// <summary>
        /// 通过组织档案接口获取组织编码。
        /// </summary>
        private async Task<string> GetOrgCodeAsync(string orgId, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(orgId))
            {
                return string.Empty;
            }

            if (_orgCodeCache.TryGetValue(orgId, out var cachedCode))
            {
                return cachedCode;
            }

            var response = await _apiClient.GetAsync<YsApiResponseDto<YsOrgInfoDto>>(
                "/yonbip/uspace/org/info_by_id",
                new Dictionary<string, string> { ["orgId"] = orgId },
                accessToken);

            EnsureSuccess(response, "org archive");
            var code = NormalizeString(response.Data?.Code);
            _orgCodeCache[orgId] = code;
            return code;
        }

        /// <summary>
        /// 根据对象类型获取客户或供应商编码。
        /// </summary>
        private async Task<string> GetDwCodeAsync(string counterpartyType, string counterpartyId, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(counterpartyId))
            {
                return string.Empty;
            }

            return counterpartyType switch
            {
                CustomerTypeValue => await GetCustomerCodeAsync(counterpartyId, accessToken),
                VendorTypeValue => await GetVendorCodeAsync(counterpartyId, accessToken),
                _ => string.Empty
            };
        }

        /// <summary>
        /// 通过客户档案接口获取客户编码。
        /// </summary>
        private async Task<string> GetCustomerCodeAsync(string customerId, string accessToken)
        {
            if (_customerCodeCache.TryGetValue(customerId, out var cachedCode))
            {
                return cachedCode;
            }

            var response = await _apiClient.PostAsync<YsApiResponseDto<List<YsMerchantRecordDto>>>(
                "/yonbip/digitalModel/merchant/newlist",
                new YsMerchantLookupRequestDto { Id = customerId },
                accessToken);

            EnsureSuccess(response, "merchant archive");
            var code = NormalizeString(response.Data?.FirstOrDefault()?.Code);
            _customerCodeCache[customerId] = code;
            return code;
        }

        /// <summary>
        /// 通过供应商档案接口获取供应商编码。
        /// </summary>
        private async Task<string> GetVendorCodeAsync(string vendorId, string accessToken)
        {
            if (_vendorCodeCache.TryGetValue(vendorId, out var cachedCode))
            {
                return cachedCode;
            }

            var response = await _apiClient.GetAsync<YsApiResponseDto<YsVendorDetailDto>>(
                "/yonbip/digitalModel/vendor/detail",
                new Dictionary<string, string> { ["id"] = vendorId },
                accessToken);

            EnsureSuccess(response, "vendor archive");
            var code = NormalizeString(response.Data?.Code);
            _vendorCodeCache[vendorId] = code;
            return code;
        }

        /// <summary>
        /// 将字符串规范化为非空值。
        /// </summary>
        private static string NormalizeString(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }

        /// <summary>
        /// 校验 YS 通用响应契约是否成功。
        /// </summary>
        private static void EnsureSuccess<T>(YsApiResponseDto<T> response, string actionName)
        {
            if (!string.Equals(response?.Code, "200", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{actionName} failed: {response?.Message ?? response?.Code}");
            }
        }

        /// <summary>
        /// 按行 Id 写入新数据，已存在数据直接跳过。
        /// </summary>
        private async Task UpsertMidRowsAsync(List<EF_MidYSBillData> rows)
        {
            if (rows.Count == 0)
            {
                return;
            }

            var ids = rows.Select(x => x.Id).ToList();
            var existingIds = await _midBillRepository.Queryable()
                .Where(x => ids.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();
            var existingIdSet = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var now = DateTime.Now;

            foreach (var row in rows.Where(x => !existingIdSet.Contains(x.Id)))
            {
                row.CreateTime = now;
                row.ProcessStatus = 0;
                row.UpdateTime = null;
                await _db.Insertable(row).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// 写入一条同步日志记录。
        /// </summary>
        private async Task InsertSyncLogAsync(
            string interfaceName,
            DateTime syncStartTime,
            DateTime syncEndTime,
            int totalRecords,
            int syncStatus,
            string errorMessage)
        {
            var log = new EF_sysSyncLog
            {
                InterfaceName = interfaceName,
                SyncStartTime = syncStartTime,
                SyncEndTime = syncEndTime,
                TotalRecords = totalRecords,
                SyncStatus = syncStatus,
                ErrorMsg = errorMessage,
                ExecuteTime = DateTime.Now
            };

            await _db.Insertable(log).ExecuteCommandAsync();
        }

        /// <summary>
        /// 尽力写入失败日志，同时不覆盖原始异常。
        /// </summary>
        private async Task InsertFailureLogSafelyAsync(
            string interfaceName,
            DateTime syncStartTime,
            DateTime syncEndTime,
            Exception exception)
        {
            try
            {
                await InsertSyncLogAsync(interfaceName, syncStartTime, syncEndTime, 0, 0, exception.Message);
            }
            catch
            {
                // 保留原始同步异常，避免被写日志异常覆盖。
            }
        }

        /// <summary>
        /// 根据任务参数解析本次需要执行的同步定义。
        /// </summary>
        private static IReadOnlyList<YsBillSyncDefinition> ResolveDefinitions(string jobParams)
        {
            if (string.IsNullOrWhiteSpace(jobParams))
            {
                return YsBillSyncDefinition.All;
            }

            var parameters = jobParams.Split(['&', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

            if (!parameters.TryGetValue(YsBillSyncConstants.JobParamTypeKey, out var typeValue) || string.IsNullOrWhiteSpace(typeValue))
            {
                return YsBillSyncDefinition.All;
            }

            return typeValue.ToLowerInvariant() switch
            {
                "all" => YsBillSyncDefinition.All,
                "settlebench" => YsBillSyncDefinition.All,
                "settle" => YsBillSyncDefinition.All,
                "ys_settlebench" => YsBillSyncDefinition.All,
                "fundpayment" => YsBillSyncDefinition.All,
                "fundcollection" => YsBillSyncDefinition.All,
                "payment" => YsBillSyncDefinition.All,
                "receipt" => YsBillSyncDefinition.All,
                _ => throw new InvalidOperationException($"Unsupported YS sync type: {typeValue}")
            };
        }

        /// <summary>
        /// 判断刷新任务是否需要更新当前中间表行。
        /// </summary>
        private static bool NeedsRefreshUpdate(EF_MidYSBillData currentRow, EF_MidYSBillData latestRow)
        {
            return !string.Equals(currentRow.MainId, latestRow.MainId, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(currentRow.CVouchCode, latestRow.CVouchCode, StringComparison.OrdinalIgnoreCase)
                || currentRow.BillDate != latestRow.BillDate
                || !string.Equals(currentRow.CMaker, latestRow.CMaker, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(currentRow.OrgCode, latestRow.OrgCode, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(currentRow.CDepCode, latestRow.CDepCode, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(currentRow.CNatBankAccount, latestRow.CNatBankAccount, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(currentRow.CNatBank, latestRow.CNatBank, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(currentRow.CSSName, latestRow.CSSName, StringComparison.OrdinalIgnoreCase)
                || currentRow.SettleStatus != latestRow.SettleStatus
                || !string.Equals(currentRow.QuickTypeName, latestRow.QuickTypeName, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(currentRow.CVouchType, latestRow.CVouchType, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(currentRow.CDwType, latestRow.CDwType, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(currentRow.CDwCode, latestRow.CDwCode, StringComparison.OrdinalIgnoreCase)
                || currentRow.IAmount != latestRow.IAmount
                || !string.Equals(currentRow.CNoteCode, latestRow.CNoteCode, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(currentRow.TradetypeName, latestRow.TradetypeName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
