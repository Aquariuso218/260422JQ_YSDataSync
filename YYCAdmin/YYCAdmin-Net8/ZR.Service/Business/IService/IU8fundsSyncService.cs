using System.Threading.Tasks;

namespace ZR.Service.Business.IService
{
    /// <summary>
    /// U8 资金期初余额同步 YS 服务接口
    /// </summary>
    public interface IU8fundsSyncService
    {
        /// <summary>
        /// 同步 U8 资金期初余额至 YS
        /// </summary>
        /// <param name="jobParams">定时任务透传的参数</param>
        /// <returns>同步结果提示信息</returns>
        Task<string> SyncAsync(string jobParams = null);
    }
}
