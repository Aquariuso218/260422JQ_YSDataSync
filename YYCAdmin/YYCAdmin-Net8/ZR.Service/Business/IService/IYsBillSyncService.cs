using System.Threading.Tasks;

namespace ZR.Service.Business.IService
{
    public interface IYsBillSyncService
    {
        /// <summary>
        /// 按任务中心传入参数执行 YS 单据同步。
        /// </summary>
        /// <param name="jobParams">
        /// 任务中心界面透传的参数，支持
        /// <c>type=fundPayment|fundCollection|all</c>，
        /// 同时兼容 <c>payment</c> 和 <c>receipt</c> 别名。
        /// </param>
        /// <returns>返回本次同步的简要结果。</returns>
        Task<string> SyncAsync(string jobParams = null);
    }
}
