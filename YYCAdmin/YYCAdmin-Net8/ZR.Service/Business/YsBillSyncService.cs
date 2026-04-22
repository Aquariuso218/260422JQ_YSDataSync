using Infrastructure.Attribute;
using SqlSugar;
using SqlSugar.IOC;
using ZR.Model.Business.Model;
using ZR.Repository;
using ZR.Service.Business.IService;
using ZR.Service.Business.Ys;
using ZR.Service.Business.Ys.Dtos;

namespace ZR.Service.Business
{
    [AppService(ServiceType = typeof(IYsBillSyncService), ServiceLifetime = LifeTime.Transient)]
    public class YsBillSyncService : IYsBillSyncService
    {
        private const string SyncStatusValue = "2";

        private readonly YsApiClient _apiClient;
        private readonly ISqlSugarClient _db;
        private readonly BaseRepository<EF_MidYSBillData> _midBillRepository;
        private readonly BaseRepository<EF_sysSyncLog> _syncLogRepository;
        private readonly Dictionary<string, string> _orgCodeCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _deptCodeCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _staffCodeCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _settleModeCodeCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _customerCodeCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _vendorCodeCache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, YsEnterpriseBankRecordDto> _bankCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 初始化 YS 单据同步服务。
        /// </summary>
        /// <param name="httpClientFactory">HTTP 客户端工厂。</param>
        public YsBillSyncService(IHttpClientFactory httpClientFactory)
        {
            _apiClient = new YsApiClient(httpClientFactory);
            _db = DbScoped.SugarScope.GetConnectionScope(0);
            _midBillRepository = new BaseRepository<EF_MidYSBillData>(_db);
            _syncLogRepository = new BaseRepository<EF_sysSyncLog>(_db);
        }

        /// <summary>
        /// 执行一个或多个 YS 资金单据同步定义。
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
        /// 执行单个资金单据定义的完整同步流程。
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
                var request = BuildListRequest(definition, pageIndex, beginTime, endTime);
                var response = await _apiClient.PostAsync<YsApiResponseDto<YsBillListDataDto>>(definition.ListPath, request, accessToken);

                EnsureSuccess(response, $"{definition.InterfaceName} list");
                var page = response.Data ?? new YsBillListDataDto();
                pageCount = page.PageCount <= 0 ? 1 : page.PageCount;

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
        /// 根据当前单据定义构造列表查询请求体。
        /// </summary>
        private static YsBillListRequestDto BuildListRequest(
            YsBillSyncDefinition definition,
            int pageIndex,
            DateTime beginTime,
            DateTime endTime)
        {
            var request = new YsBillListRequestDto
            {
                PageIndex = pageIndex,
                PageSize = 100,
                OpenVouchdateBegin = beginTime.ToString("yyyy-MM-dd"),
                OpenVouchdateEnd = endTime.ToString("yyyy-MM-dd")
            };

            if (definition.UseSimpleVosVerifyFilter)
            {
                request.SimpleVOs =
                [
                    new YsSimpleQueryConditionDto
                    {
                        Field = "verifystate",
                        Op = "eq",
                        Value1 = SyncStatusValue
                    }
                ];
            }
            else
            {
                request.Verifystate = SyncStatusValue;
            }

            return request;
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

                foreach (var item in GetBodyItems(definition, detail))
                {
                    var row = await MapRowAsync(definition, detail, item, accessToken);
                    if (row != null)
                    {
                        rows.Add(row);
                    }
                }
            }

            return rows;
        }

        /// <summary>
        /// 根据当前详情类型，获取对应的表体集合。
        /// </summary>
        private static IEnumerable<YsFundBillBodyItemDto> GetBodyItems(YsBillSyncDefinition definition, YsBillDetailDto detail)
        {
            return definition.BodyPropertyName switch
            {
                "FundPayment_b" => detail.FundPaymentBody ?? [],
                "FundCollection_b" => detail.FundCollectionBody ?? [],
                _ => []
            };
        }

        /// <summary>
        /// 将一条 YS 详情表体数据映射为同步实体。
        /// </summary>
        private async Task<EF_MidYSBillData> MapRowAsync(
            YsBillSyncDefinition definition,
            YsBillDetailDto detail,
            YsFundBillBodyItemDto item,
            string accessToken)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                return null;
            }

            var orgCode = await GetOrgCodeAsync(detail.Accentity, accessToken);
            var deptCode = await GetDeptCodeAsync(item.Dept, accessToken);
            var operatorCode = await GetStaffCodeAsync(item.Operator, accessToken);
            var bankInfo = await GetBankInfoAsync(item.EnterpriseBankAccount, accessToken);
            var settleModeCode = await GetSettleModeCodeAsync(item.SettleMode, accessToken);
            var objectCode = await GetObjectCodeAsync(item.Caobject, item.OppositeObjectId, accessToken);

            return new EF_MidYSBillData
            {
                Id = item.Id,
                MainId = item.MainId ?? detail.Id,
                Code = detail.Code,
                BillDate = detail.BillDate,
                Creator = detail.Creator,
                OrgCode = orgCode,
                DepCode = deptCode,
                OperatorCode = operatorCode,
                EnterpriseBankAccountNo = bankInfo?.Account,
                EnterpriseBankAccountName = bankInfo?.AcctName?.ZhCn,
                SettleModeCode = settleModeCode,
                SettleStatus = ParseNullableInt(item.SettleStatus),
                QuickTypeName = item.QuickTypeName,
                CVouchType = definition.VouchType,
                Caobject = item.Caobject,
                ObjectCode = objectCode,
                OriSum = item.OriSum
            };
        }

        /// <summary>
        /// 通过组织档案接口获取组织编码。
        /// </summary>
        private async Task<string> GetOrgCodeAsync(string orgId, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(orgId))
            {
                return null;
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
            var code = response.Data?.Code;
            _orgCodeCache[orgId] = code;
            return code;
        }

        /// <summary>
        /// 通过部门档案接口获取部门编码。
        /// </summary>
        private async Task<string> GetDeptCodeAsync(string deptId, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(deptId))
            {
                return null;
            }

            if (_deptCodeCache.TryGetValue(deptId, out var cachedCode))
            {
                return cachedCode;
            }

            var response = await _apiClient.GetAsync<YsApiResponseDto<YsDeptInfoDto>>(
                "/yonbip/digitalModel/admindept/detail",
                new Dictionary<string, string> { ["id"] = deptId },
                accessToken);

            EnsureSuccess(response, "dept archive");
            var code = response.Data?.Code;
            _deptCodeCache[deptId] = code;
            return code;
        }

        /// <summary>
        /// 通过人员档案接口获取员工编码。
        /// </summary>
        private async Task<string> GetStaffCodeAsync(string staffId, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(staffId))
            {
                return null;
            }

            if (_staffCodeCache.TryGetValue(staffId, out var cachedCode))
            {
                return cachedCode;
            }

            var response = await _apiClient.GetAsync<YsApiResponseDto<YsStaffInfoDto>>(
                "/yonbip/uspace/staff/info_by_id",
                new Dictionary<string, string> { ["id"] = staffId },
                accessToken);

            EnsureSuccess(response, "staff archive");
            var code = response.Data?.Code;
            _staffCodeCache[staffId] = code;
            return code;
        }

        /// <summary>
        /// 通过银行档案接口获取企业银行账户信息。
        /// </summary>
        private async Task<YsEnterpriseBankRecordDto> GetBankInfoAsync(string bankId, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(bankId))
            {
                return null;
            }

            if (_bankCache.TryGetValue(bankId, out var cachedRecord))
            {
                return cachedRecord;
            }

            var response = await _apiClient.PostAsync<YsApiResponseDto<YsPagedRecordListDto<YsEnterpriseBankRecordDto>>>(
                "/yonbip/digitalModel/basedoc/enterprisebank/batchQueryDetail",
                new YsEnterpriseBankBatchRequestDto
                {
                    Ids = [bankId]
                },
                accessToken);

            EnsureSuccess(response, "enterprise bank archive");
            var record = response.Data?.RecordList?.FirstOrDefault();
            _bankCache[bankId] = record;
            return record;
        }

        /// <summary>
        /// 通过结算方式档案接口获取结算方式编码。
        /// </summary>
        private async Task<string> GetSettleModeCodeAsync(string settleModeId, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(settleModeId))
            {
                return null;
            }

            if (_settleModeCodeCache.TryGetValue(settleModeId, out var cachedCode))
            {
                return cachedCode;
            }

            var response = await _apiClient.PostAsync<YsApiResponseDto<YsPagedRecordListDto<YsSettleMethodRecordDto>>>(
                "/yonbip/digitalModel/settlemethod/batchQueryDetail",
                new YsEnterpriseBankBatchRequestDto
                {
                    Ids = [settleModeId]
                },
                accessToken);

            EnsureSuccess(response, "settle method archive");
            var code = response.Data?.RecordList?.FirstOrDefault()?.Code;
            _settleModeCodeCache[settleModeId] = code;
            return code;
        }

        /// <summary>
        /// 根据对象类型标记获取往来对象编码。
        /// </summary>
        private async Task<string> GetObjectCodeAsync(int? caobject, string objectId, string accessToken)
        {
            if (string.IsNullOrWhiteSpace(objectId) || caobject == null)
            {
                return null;
            }

            return caobject.Value switch
            {
                1 => await GetCustomerCodeAsync(objectId, accessToken),
                2 => await GetVendorCodeAsync(objectId, accessToken),
                _ => null
            };
        }

        /// <summary>
        /// 通过客商档案接口获取客户编码。
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
            var code = response.Data?.FirstOrDefault()?.Code;
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
            var code = response.Data?.Code;
            _vendorCodeCache[vendorId] = code;
            return code;
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
        /// 按行 Id 对中间表数据执行新增或更新。
        /// </summary>
        private async Task UpsertMidRowsAsync(List<EF_MidYSBillData> rows)
        {
            if (rows.Count == 0)
            {
                return;
            }

            var now = DateTime.Now;
            var ids = rows.Select(x => x.Id).ToList();
            var existingRows = await _midBillRepository.Queryable()
                .Where(x => ids.Contains(x.Id))
                .ToListAsync();
            var existingMap = existingRows.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                if (existingMap.TryGetValue(row.Id, out var existing))
                {
                    row.CreateTime = existing.CreateTime;
                    row.ProcessStatus = existing.ProcessStatus;
                    row.ProcessMsg = existing.ProcessMsg;
                    row.U8Code = existing.U8Code;
                    row.SynTime = existing.SynTime;
                    row.UpdateTime = now;

                    await _db.Updateable(row).ExecuteCommandAsync();
                }
                else
                {
                    row.CreateTime = now;
                    row.ProcessStatus = 0;
                    row.UpdateTime = null;

                    await _db.Insertable(row).ExecuteCommandAsync();
                }
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
        /// 将接口中的字符串字段解析为可空整数。
        /// </summary>
        private static int? ParseNullableInt(string value)
        {
            return int.TryParse(value, out var result) ? result : null;
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
                "fundpayment" => [FindDefinition("YS_FundPayment")],
                "fundcollection" => [FindDefinition("YS_FundCollection")],
                "payment" => [FindDefinition("YS_FundPayment")],
                "receipt" => [FindDefinition("YS_FundCollection")],
                _ => throw new InvalidOperationException($"Unsupported YS sync type: {typeValue}")
            };
        }

        /// <summary>
        /// 根据接口名称查找单个同步定义。
        /// </summary>
        private static YsBillSyncDefinition FindDefinition(string interfaceName)
        {
            return YsBillSyncDefinition.All.First(x => x.InterfaceName == interfaceName);
        }
    }
}
