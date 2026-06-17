using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using SqlSugar.IOC;
using ZR.Model.Business;
using ZR.Repository;
using ZR.Service.Business.IService;
using ZR.Service.Business.Ys.Dtos;

namespace ZR.Service.Business.Ys
{
    [AppService(ServiceType = typeof(IYsBillInstrumentFetchService), ServiceLifetime = LifeTime.Transient)]
    public class YsBillInstrumentFetchService : IYsBillInstrumentFetchService
    {
        private const string DiscountType = "discount";
        private const string ExpireCashType = "expirecash";
        private const string ConsignBankType = "consignbank";

        private const string DiscountInterfaceName = "YS_Discount";
        private const string ExpireCashInterfaceName = "YS_ExpireCash";
        private const string ConsignBankInterfaceName = "YS_ConsignBank";

        private const string DiscountPath = "/yonbip/ctm/discountapi/query/discountdetail";
        private const string ExpireCashPath = "/yonbip/FCC/api/expirecash/detail";
        private const string ConsignBankPath = "/yonbip/FCC/api/consignbank/detail";

        private const string DiscountBillStatus = "1";
        private const string ExpireCashBillStatus = "9";
        private const string ConsignBankBillStatus = "5";

        private const string CustomerRoleValue = "1";
        private const string VendorRoleValue = "2";
        private const string DefaultMaker = "ys同步";
        private const string DefaultDiscountQuickTypeName = "贴现款";
        private const string DefaultTenantId = "sibt9q6m";

        private readonly YsApiClient _apiClient;
        private readonly ISqlSugarClient _db;
        private readonly BaseRepository<EfMidysbilldata> _midBillRepository;
        private readonly BaseRepository<EF_sysSyncLog> _syncLogRepository;
        private readonly Dictionary<string, string> _customerCodeCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly YsConfigOptions _config;

        /// <summary>
        /// 初始化 YS 票据业务抓取服务。
        /// </summary>
        /// <param name="httpClientFactory">HTTP 客户端工厂。</param>
        public YsBillInstrumentFetchService(IHttpClientFactory httpClientFactory)
        {
            _apiClient = new YsApiClient(httpClientFactory);
            _db = DbScoped.SugarScope.GetConnectionScope(0);
            _midBillRepository = new BaseRepository<EfMidysbilldata>(_db);
            _syncLogRepository = new BaseRepository<EF_sysSyncLog>(_db);
            _config = AppSettings.Get<YsConfigOptions>(YsBillSyncConstants.ConfigSectionName) ?? new YsConfigOptions();
        }

        /// <summary>
        /// 抓取贴现办理、到期兑付、银行托收数据。
        /// </summary>
        /// <param name="jobParams">任务中心界面透传的参数。</param>
        /// <returns>返回本次抓取任务的简要结果。</returns>
        public async Task<string> SyncAsync(string jobParams = null)
        {
            var syncTypes = ResolveSyncTypes(jobParams);
            if (syncTypes.Count == 0)
            {
                return "YS票据抓取: 当前类型无需执行";
            }

            var isCompensation = IsCompensation(jobParams);
            var messages = new List<string>();
            foreach (var syncType in syncTypes)
            {
                messages.Add(await SyncSingleTypeAsync(syncType, isCompensation));
            }

            return string.Join(" | ", messages.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        /// <summary>
        /// 执行单个票据业务类型的完整同步流程。
        /// </summary>
        private async Task<string> SyncSingleTypeAsync(string syncType, bool isCompensation)
        {
            var syncEndTime = DateTime.Now;
            var baseInterfaceName = GetInterfaceName(syncType);
            var interfaceName = isCompensation ? $"{baseInterfaceName}_Compensation" : baseInterfaceName;
            
            DateTime syncStartTime;
            if (isCompensation)
            {
                // 补单业务：固定同步起点为上月 1 号的 0 点
                syncStartTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
            }
            else
            {
                syncStartTime = await GetLastSuccessEndTimeAsync(interfaceName)
                    ?? syncEndTime.AddDays(-YsBillSyncConstants.FirstSyncFallbackDays);
            }

            try
            {
                var accessToken = await _apiClient.GetAccessTokenAsync();
                var rows = await BuildRowsAsync(syncType, accessToken, syncStartTime, syncEndTime);

                _db.Ado.BeginTran();
                await UpsertMidRowsAsync(rows);
                await InsertSyncLogAsync(interfaceName, syncStartTime, syncEndTime, rows.Count, 1, null);
                _db.Ado.CommitTran();

                return $"{interfaceName}: synced {rows.Count} rows";
            }
            catch (Exception ex)
            {
                _db.Ado.RollbackTran();
                await InsertFailureLogSafelyAsync(interfaceName, syncStartTime, syncEndTime, ex);
                return $"{interfaceName}: failed - {ex.Message}";
            }
        }

        /// <summary>
        /// 根据同步类型调用对应接口并构造中间表数据。
        /// </summary>
        private async Task<List<EfMidysbilldata>> BuildRowsAsync(
            string syncType,
            string accessToken,
            DateTime syncStartTime,
            DateTime syncEndTime)
        {
            return syncType switch
            {
                DiscountType => await FetchDiscountRowsAsync(accessToken, syncStartTime, syncEndTime),
                ExpireCashType => await FetchExpireCashRowsAsync(accessToken, syncStartTime, syncEndTime),
                ConsignBankType => await FetchConsignBankRowsAsync(accessToken, syncStartTime, syncEndTime),
                _ => throw new InvalidOperationException($"Unsupported YS instrument sync type: {syncType}")
            };
        }

        /// <summary>
        /// 拉取贴现办理数据并映射到中间表。
        /// </summary>
        private async Task<List<EfMidysbilldata>> FetchDiscountRowsAsync(
            string accessToken,
            DateTime syncStartTime,
            DateTime syncEndTime)
        {
            var request = new YsDiscountQueryRequestDto
            {
                YtenantId = string.IsNullOrWhiteSpace(_config.YtenantId) ? DefaultTenantId : _config.YtenantId,
                DiscountDateStart = syncStartTime.ToString("yyyy-MM-dd"),
                DiscountDateEnd = syncEndTime.ToString("yyyy-MM-dd"),
                BillStatus = DiscountBillStatus
            };

            var response = await _apiClient.PostAsync<YsApiResponseDto<YsDiscountDetailResponseDataDto>>(
                DiscountPath,
                request,
                accessToken);

            EnsureSuccess(response, DiscountInterfaceName);
            var records = response.Data?.DiscountDatas ?? [];
            var rows = new List<EfMidysbilldata>();

            foreach (var record in records.Where(x => string.Equals(NormalizeString(x.InvoiceRoles), CustomerRoleValue, StringComparison.OrdinalIgnoreCase)))
            {
                var row = await MapDiscountRowAsync(record, accessToken);
                if (row != null)
                {
                    rows.Add(row);
                }
            }

            return await FilterExistingRowsAsync(rows);
        }

        /// <summary>
        /// 拉取到期兑付数据并映射到中间表。
        /// </summary>
        private async Task<List<EfMidysbilldata>> FetchExpireCashRowsAsync(
            string accessToken,
            DateTime syncStartTime,
            DateTime syncEndTime)
        {
            var request = new YsExpireCashQueryRequestDto
            {
                PaymentDateStart = syncStartTime.ToString("yyyy-MM-dd"),
                PaymentDateEnd = syncEndTime.ToString("yyyy-MM-dd"),
                BillStatus = ExpireCashBillStatus
            };

            var response = await _apiClient.PostAsync<YsApiResponseDto<YsRecordListResponseDataDto<YsExpireCashRecordDto>>>(
                ExpireCashPath,
                request,
                accessToken);

            EnsureSuccess(response, ExpireCashInterfaceName);
            var records = response.Data?.RecordList ?? [];
            var rows = records
                .Where(x => string.Equals(NormalizeString(x.ReceiverRoles), VendorRoleValue, StringComparison.OrdinalIgnoreCase))
                .Select(MapExpireCashRow)
                .Where(x => x != null)
                .ToList();

            return await FilterExistingRowsAsync(rows!);
        }

        /// <summary>
        /// 拉取银行托收数据并映射到中间表。
        /// </summary>
        private async Task<List<EfMidysbilldata>> FetchConsignBankRowsAsync(
            string accessToken,
            DateTime syncStartTime,
            DateTime syncEndTime)
        {
            var request = new YsConsignBankQueryRequestDto
            {
                ConsignDateStart = syncStartTime.ToString("yyyy-MM-dd"),
                ConsignDateEnd = syncEndTime.ToString("yyyy-MM-dd"),
                BillStatus = ConsignBankBillStatus
            };

            var response = await _apiClient.PostAsync<YsApiResponseDto<YsRecordListResponseDataDto<YsConsignBankRecordDto>>>(
                ConsignBankPath,
                request,
                accessToken);

            EnsureSuccess(response, ConsignBankInterfaceName);
            var records = response.Data?.RecordList ?? [];
            var rows = records
                .Where(x => string.Equals(NormalizeString(x.InvoicerRoles), CustomerRoleValue, StringComparison.OrdinalIgnoreCase))
                .Select(MapConsignBankRow)
                .Where(x => x != null)
                .ToList();

            return await FilterExistingRowsAsync(rows!);
        }

        /// <summary>
        /// 将贴现办理记录映射为中间表实体。
        /// </summary>
        private async Task<EfMidysbilldata> MapDiscountRowAsync(YsDiscountRecordDto record, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(record.BillId))
            {
                return null;
            }

            return new EfMidysbilldata
            {
                Id = NormalizeString(record.BillId),
                MainId = string.Empty,
                CVouchCode = NormalizeString(record.BillCode),
                BillDate = record.DiscountDate,
                CMaker = DefaultMaker,
                OrgCode = ExtractParentOrgCode(NormalizeString(record.Accentity)),
                CNatBankAccount = NormalizeString(record.DiscountBankAccount),
                SettleStatus = 3,
                QuickTypeName = DefaultDiscountQuickTypeName,
                CVouchType = "贴现办理",
                CDwType = "客户",
                CDwCode = await GetCustomerCodeAsync(record.InvoicerId, accessToken),
                IAmount = record.DiscountMoney ?? 0m,
                CNoteCode = NormalizeString(record.NoteNo),
                TradetypeName = "贴现办理",
                DiscountInterest = record.DiscountInterest,
                NoteTypeCode = NormalizeString(record.NoteTypeCode)
            };
        }

        /// <summary>
        /// 将到期兑付记录映射为中间表实体。
        /// </summary>
        private static EfMidysbilldata MapExpireCashRow(YsExpireCashRecordDto record)
        {
            if (string.IsNullOrWhiteSpace(record.Id))
            {
                return null;
            }

            return new EfMidysbilldata
            {
                Id = NormalizeString(record.Id),
                MainId = string.Empty,
                CVouchCode = NormalizeString(record.Code),
                BillDate = record.PaymentDate,
                CMaker = DefaultMaker,
                OrgCode = ExtractParentOrgCode(NormalizeString(record.AccentityCode)),
                CNatBankAccount = NormalizeString(record.PayBankAccount),
                SettleStatus = 3,
                QuickTypeName = NormalizeString(record.QuickType),
                CVouchType = "到期兑付",
                CDwType = "供应商",
                CDwCode = NormalizeString(record.ReceiverCode),
                IAmount = record.OriSum ?? 0m,
                CNoteCode = NormalizeString(record.NoteNo),
                TradetypeName = "到期兑付",
                NoteTypeCode = NormalizeString(record.BillType)
            };
        }

        /// <summary>
        /// 将银行托收记录映射为中间表实体。
        /// </summary>
        private static EfMidysbilldata MapConsignBankRow(YsConsignBankRecordDto record)
        {
            if (string.IsNullOrWhiteSpace(record.Id))
            {
                return null;
            }

            return new EfMidysbilldata
            {
                Id = NormalizeString(record.Id),
                MainId = string.Empty,
                CVouchCode = NormalizeString(record.Code),
                BillDate = record.ConsignDate,
                CMaker = DefaultMaker,
                OrgCode = ExtractParentOrgCode(NormalizeString(record.AccentityCode)),
                CNatBankAccount = NormalizeString(record.ConsignBankAccount),
                SettleStatus = 3,
                QuickTypeName = NormalizeString(record.Description),
                CVouchType = "银行托收",
                CDwType = "客户",
                CDwCode = NormalizeString(record.InvoicerCode),
                IAmount = record.ConsignAmount ?? 0m,
                CNoteCode = NormalizeString(record.NoteNo),
                TradetypeName = "银行托收",
                NoteTypeCode = NormalizeString(record.BillType)
            };
        }

        /// <summary>
        /// 过滤掉中间表已存在的记录，按 Id 去重。
        /// </summary>
        private async Task<List<EfMidysbilldata>> FilterExistingRowsAsync(List<EfMidysbilldata> rows)
        {
            if (rows.Count == 0)
            {
                return rows;
            }

            var deduplicatedRows = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToList();
            var ids = deduplicatedRows.Select(x => x.Id).ToList();

            var existingIds = await _midBillRepository.Queryable()
                .Where(x => ids.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            if (existingIds.Count == 0)
            {
                return deduplicatedRows;
            }

            var existingIdSet = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            return deduplicatedRows
                .Where(x => !existingIdSet.Contains(x.Id))
                .ToList();
        }

        /// <summary>
        /// 批量插入中间表数据。
        /// </summary>
        private async Task UpsertMidRowsAsync(List<EfMidysbilldata> rows)
        {
            if (rows.Count == 0)
            {
                return;
            }

            var now = DateTime.Now;
            foreach (var row in rows)
            {
                var normalizedRow = NormalizeForInsert(row, now);
                await _db.Insertable(normalizedRow).ExecuteCommandAsync();
            }
        }

        /// <summary>
        /// 将新增票据业务中间表记录统一规范化，避免写入空值。
        /// </summary>
        private static EfMidysbilldata NormalizeForInsert(EfMidysbilldata row, DateTime insertTime)
        {
            return new EfMidysbilldata
            {
                Id = NormalizeString(row.Id),
                MainId = string.Empty,
                CVouchCode = NormalizeString(row.CVouchCode),
                BillDate = row.BillDate ?? insertTime.Date,
                CMaker = string.IsNullOrWhiteSpace(row.CMaker) ? DefaultMaker : row.CMaker,
                OrgCode = NormalizeString(row.OrgCode),
                CDepCode = NormalizeString(row.CDepCode),
                CNatBankAccount = NormalizeString(row.CNatBankAccount),
                CNatBank = NormalizeString(row.CNatBank),
                CSSName = NormalizeString(row.CSSName),
                SettleStatus = row.SettleStatus ?? 3,
                QuickTypeName = NormalizeString(row.QuickTypeName),
                CVouchType = NormalizeString(row.CVouchType),
                CDwType = NormalizeString(row.CDwType),
                CDwCode = NormalizeString(row.CDwCode),
                IAmount = row.IAmount ?? 0m,
                CNoteCode = NormalizeString(row.CNoteCode),
                TradetypeName = NormalizeString(row.TradetypeName),
                DiscountInterest = row.DiscountInterest ?? 0m,
                NoteTypeCode = NormalizeString(row.NoteTypeCode),
                CreateTime = insertTime,
                UpdateTime = row.UpdateTime ?? insertTime,
                ProcessStatus = 0,
                ProcessMsg = NormalizeString(row.ProcessMsg),
                U8Code = NormalizeString(row.U8Code),
                SynTime = row.SynTime ?? insertTime
            };
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
        /// 写入同步日志。
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
        /// 尽力写入失败日志，同时保留原始异常。
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
                // 保留原始异常，避免失败日志覆盖真正问题。
            }
        }

        /// <summary>
        /// 通过客户档案接口获取客户编码。
        /// </summary>
        private async Task<string> GetCustomerCodeAsync(string customerId, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(customerId))
            {
                return string.Empty;
            }

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
        /// 解析本次需要执行的票据业务类型。
        /// </summary>
        private static IReadOnlyList<string> ResolveSyncTypes(string jobParams)
        {
            var typeValue = GetTypeValue(jobParams);
            if (string.IsNullOrWhiteSpace(typeValue) || string.Equals(typeValue, "all", StringComparison.OrdinalIgnoreCase))
            {
                return [DiscountType, ExpireCashType, ConsignBankType];
            }

            return typeValue.ToLowerInvariant() switch
            {
                DiscountType => [DiscountType],
                ExpireCashType => [ExpireCashType],
                ConsignBankType => [ConsignBankType],
                _ => []
            };
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

            return parameters.TryGetValue(YsBillSyncConstants.JobParamTypeKey, out var typeValue)
                ? NormalizeString(typeValue)
                : string.Empty;
        }

        /// <summary>
        /// 根据同步类型获取同步日志接口名称。
        /// </summary>
        private static string GetInterfaceName(string syncType)
        {
            return syncType switch
            {
                DiscountType => DiscountInterfaceName,
                ExpireCashType => ExpireCashInterfaceName,
                ConsignBankType => ConsignBankInterfaceName,
                _ => throw new InvalidOperationException($"Unsupported YS instrument sync type: {syncType}")
            };
        }

        /// <summary>
        /// 校验 YS 通用响应契约是否成功。
        /// </summary>
        private static void EnsureSuccess<T>(YsApiResponseDto<T> response, string actionName)
        {
            if (!string.Equals(response?.Code, "00", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(response?.Code, "200", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{actionName} failed: {response?.Message ?? response?.Code}");
            }
        }

        /// <summary>
        /// 将字符串规范化为非空值。
        /// </summary>
        private static string NormalizeString(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }

        /// <summary>
        /// 从子组织编码中提取前四位主组织编码。
        /// YS 返回的组织编码为子组织编码，其前四位为主组织编码。
        /// </summary>
        private static string ExtractParentOrgCode(string orgCode)
        {
            if (string.IsNullOrWhiteSpace(orgCode))
            {
                return string.Empty;
            }

            return orgCode.Length >= 4 ? orgCode[..4] : orgCode;
        }

        /// <summary>
        /// 解析任务参数中是否指定了补单业务标识。
        /// </summary>
        private static bool IsCompensation(string jobParams)
        {
            if (string.IsNullOrWhiteSpace(jobParams))
            {
                return false;
            }

            var parameters = jobParams.Split(['&', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => part.Split('=', 2, StringSplitOptions.TrimEntries))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

            return parameters.TryGetValue("isCompensation", out var value) && bool.TryParse(value, out var result) && result;
        }
    }
}
