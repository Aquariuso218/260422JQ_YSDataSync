using Infrastructure.Attribute;
using Newtonsoft.Json.Linq;
using SqlSugar;
using SqlSugar.IOC;
using ZR.Model.Business.Model;
using ZR.Model.Business.Model.Dto;
using ZR.Repository;
using ZR.Service.Business.IService;

namespace ZR.Service.Business.U8
{
    [AppService(ServiceType = typeof(IApCloseBillSyncService), ServiceLifetime = LifeTime.Transient)]
    public class ApCloseBillSyncService : IApCloseBillSyncService
    {
        private const int YsSettledStatusValue = 3;

        private static readonly HashSet<string> AllowedQuickTypesForU8Sync =
        [
            "应付款",
            "预付款",
            "费用付款"
        ];

        private readonly IApCloseBillService _apCloseBillService;
        private readonly ISqlSugarClient _db;
        private readonly BaseRepository<EF_MidYSBillData> _midBillRepository;

        /// <summary>
        /// 初始化 U8 收付款单同步服务。
        /// </summary>
        /// <param name="apCloseBillService">收付款单生成服务。</param>
        /// <param name="httpClientFactory">HTTP 客户端工厂。</param>
        public ApCloseBillSyncService(IApCloseBillService apCloseBillService, IHttpClientFactory httpClientFactory)
        {
            _apCloseBillService = apCloseBillService;
            _db = DbScoped.SugarScope.GetConnectionScope(0);
            _midBillRepository = new BaseRepository<EF_MidYSBillData>(_db);
        }

        /// <summary>
        /// 将中间表中满足条件的结算数据同步到 U8 收付款单接口。
        /// </summary>
        /// <param name="jobParams">任务中心界面透传的参数。</param>
        /// <returns>返回本次 U8 同步任务的简要结果字符串。</returns>
        public async Task<string> SyncAsync(string jobParams = null)
        {
            var pendingRows = await _midBillRepository.Queryable()
                .Where(x => (x.ProcessStatus == 0 || x.ProcessStatus == 2) && x.SettleStatus == YsSettledStatusValue)
                .ToListAsync();

            var rowsToSync = pendingRows.ToList();
            if (rowsToSync.Count == 0)
            {
                return "U8同步: 无待同步数据";
            }

            var successCount = 0;
            var failCount = 0;

            foreach (var row in rowsToSync)
            {
                try
                {
                    var dto = BuildApCloseBillDto(row);
                    var jsonDto = Newtonsoft.Json.JsonConvert.SerializeObject(dto);
                    
                    var result = await _apCloseBillService.ApCloseBillAdd(dto);
                    
                    if (result.StartsWith("操作成功"))
                    {
                        row.ProcessStatus = 1;
                        row.ProcessMsg = result;
                        row.SynTime = DateTime.Now;
                        successCount++;
                    }
                    else
                    {
                        row.ProcessStatus = 2;
                        row.ProcessMsg = result;
                        failCount++;
                    }
                }
                catch (Exception ex)
                {
                    row.ProcessStatus = 2;
                    row.ProcessMsg = ex.Message;
                    failCount++;
                }

                await UpdateU8SyncStateAsync(row);
            }

            return $"U8同步: 检查{rowsToSync.Count}条, 成功{successCount}条, 失败{failCount}条";
        }

        /// <summary>
        /// 组装 ApCloseBillDto 对象。
        /// </summary>
        private static ApCloseBillDto BuildApCloseBillDto(EF_MidYSBillData row)
        {
            return new ApCloseBillDto
            {
                orgCode = row.OrgCode,
                ID = row.Id,
                cVouchCode = row.CVouchCode,
                quickTypeName = row.QuickTypeName,
                cVouchType = row.CVouchType,
                cDwType = row.CDwType,
                cDwCode = row.CDwCode,
                billDate = row.BillDate?.ToString("yyyy-MM-dd"),
                cNatBankAccount = row.CNatBankAccount,
                cNatBank = row.CNatBank,
                cSSName = row.CSSName,
                cDepCode = row.CDepCode,
                cPersonCode = string.Empty,
                // cDigest = BuildDigest(row),
                cDigest = "",
                CMAKER = row.CMaker,
                cNoteCode = row.CNoteCode,
                receiptDirection = ConvertReceiptDirection(row.CVouchType),
                tradetypeName = row.TradetypeName,
                iAmount = row.IAmount ?? 0,
                noteTypeCode = row.NoteTypeCode,
                discountInterest = row.DiscountInterest ?? 0
            };
        }

        /// <summary>
        /// 生成默认摘要说明。
        /// </summary>
        private static string BuildDigest(EF_MidYSBillData row)
        {
            var parts = new[] { row.QuickTypeName, row.CVouchCode }
                .Where(x => !string.IsNullOrWhiteSpace(x));
            return string.Join("-", parts);
        }

        /// <summary>
        /// 将单据类型转换为接口需要的收付方向。
        /// </summary>
        private static string ConvertReceiptDirection(string vouchType)
        {
            return vouchType switch
            {
                "资金付款" => "付款",
                "资金收款" => "收款",
                _ => string.Empty
            };
        }

        /// <summary>
        /// 更新中间表中的 U8 同步状态字段。
        /// </summary>
        private async Task UpdateU8SyncStateAsync(EF_MidYSBillData row)
        {
            await _db.Updateable(row)
                .UpdateColumns(x => new
                {
                    x.ProcessStatus,
                    x.ProcessMsg,
                    x.U8Code,
                    x.SynTime
                })
                .ExecuteCommandAsync();
        }
    }
}
