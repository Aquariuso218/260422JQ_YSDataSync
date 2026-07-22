using Infrastructure;
using Infrastructure.Attribute;
using SqlSugar;
using SqlSugar.IOC;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ZR.Model.Business;
using ZR.Service.Business.IService;
using ZR.Service.Business.U8.Dtos;
using ZR.Service.Business.U8.U8Dtos;
using ZR.Service.Business.Ys;

namespace ZR.Service.Business.U8
{
    /// <summary>
    /// U8 资金期初余额同步 YS 服务实现类
    /// </summary>
    [AppService(ServiceType = typeof(IU8fundsSyncService), ServiceLifetime = LifeTime.Transient)]
    public class U8fundsSyncService : IU8fundsSyncService
    {
        private readonly IU8fundsService _u8fundsService;
        private readonly YsApiClient _apiClient;
        private readonly ISqlSugarClient _db;
        private readonly YsConfigOptions _config;

        /// <summary>
        /// 构造函数
        /// </summary>
        public U8fundsSyncService(IU8fundsService u8fundsService, IHttpClientFactory httpClientFactory)
        {
            _u8fundsService = u8fundsService;
            _apiClient = new YsApiClient(httpClientFactory);
            _db = DbScoped.SugarScope.GetConnectionScope(0);
            _config = AppSettings.Get<YsConfigOptions>(YsBillSyncConstants.ConfigSectionName) ?? new YsConfigOptions();
        }

        /// <summary>
        /// 执行同步
        /// </summary>
        public async Task<string> SyncAsync(string jobParams = null)
        {
            var syncStartTime = DateTime.Now;
            const string interfaceName = "U8_TOBalance_Sync";

            try
            {
                // 1. 获取 U8 本定期初数据
                var u8Result = await _u8fundsService.GetU8fundsYE();
                if (u8Result == null || !u8Result.success)
                {
                    var errorMsg = u8Result?.msg ?? "未获取到有效的U8期初数据";
                    await InsertSyncLogAsync(interfaceName, syncStartTime, DateTime.Now, 0, 0, errorMsg);
                    return $"同步失败：{errorMsg}";
                }

                var u8FundsList = u8Result.u8Funds;
                if (u8FundsList == null || u8FundsList.Count == 0)
                {
                    await InsertSyncLogAsync(interfaceName, syncStartTime, DateTime.Now, 0, 1, "U8 期初数据为空，无需同步");
                    return "同步完成：无期初数据需要同步";
                }

                // 2. 调用 YS 获取 access_token
                var accessToken = await _apiClient.GetAccessTokenAsync();

                // 3. 构建请求体
                var requestBody = new
                {
                    Data = u8FundsList
                };

                // 4. 调用 YS API 进行推送
                var relativePath = $"/{_config.YtenantId}/yonbip/FCC/U8TOBalanceCreate";
                var ysResponse = await _apiClient.PostAsync<U8TOBalanceSyncResponseDto>(relativePath, requestBody, accessToken);

                // 5. 解析并记录同步响应结果
                if (ysResponse == null)
                {
                    var errorMsg = "YS 接口返回响应为空";
                    await InsertSyncLogAsync(interfaceName, syncStartTime, DateTime.Now, u8FundsList.Count, 0, errorMsg);
                    return $"同步失败：{errorMsg}";
                }

                if (!ysResponse.Success || ysResponse.Code != "200")
                {
                    var errorMsg = $"YS 返回错误 - Code: {ysResponse.Code}, Message: {ysResponse.Message}";
                    await InsertSyncLogAsync(interfaceName, syncStartTime, DateTime.Now, u8FundsList.Count, 0, errorMsg);
                    return $"同步失败：{errorMsg}";
                }

                var successMsg = ysResponse.Message ?? $"同步成功：共推送 {u8FundsList.Count} 条数据";
                await InsertSyncLogAsync(interfaceName, syncStartTime, DateTime.Now, u8FundsList.Count, 1, successMsg);
                return $"同步成功：{successMsg}";
            }
            catch (Exception ex)
            {
                try
                {
                    await InsertSyncLogAsync(interfaceName, syncStartTime, DateTime.Now, 0, 0, ex.Message);
                }
                catch
                {
                    // 吞掉记录日志自身可能发生的错误，保证原程序异常能够被正常抛出和记录
                }
                throw;
            }
        }

        /// <summary>
        /// 记录同步日志
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
    }
}
