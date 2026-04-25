using System.Threading.Tasks;

namespace ZR.Service.Business.IService
{
    public interface IYsBillSyncService
    {
        /// <summary>
        /// 按任务中心传入参数执行 YS 结算单同步。
        /// </summary>
        /// <param name="jobParams">
        /// 任务中心界面透传的参数，支持
        /// <c>type=settleBench|settle|all</c>，
        /// 同时兼容旧参数 <c>fundPayment</c>、<c>fundCollection</c>、<c>payment</c> 和 <c>receipt</c>。
        /// </param>
        /// <returns>返回本次同步的简要结果。</returns>
        Task<string> SyncAsync(string jobParams = null);

        /// <summary>
        /// 刷新中间表中未结算完成的数据状态。
        /// </summary>
        /// <param name="jobParams">
        /// 任务中心界面透传的参数，支持
        /// <c>type=settleBench|settle|all</c>，
        /// 同时兼容旧参数 <c>fundPayment</c>、<c>fundCollection</c>、<c>payment</c> 和 <c>receipt</c>。
        /// </param>
        /// <returns>返回本次刷新任务的简要结果。</returns>
        Task<string> RefreshAsync(string jobParams = null);

        /// <summary>
        /// 将中间表中满足条件的结算数据同步到 U8 收付款单接口。
        /// </summary>
        /// <param name="jobParams">任务中心界面透传的参数，当前预留扩展使用。</param>
        /// <returns>返回本次 U8 同步任务的简要结果。</returns>
        Task<string> SyncToU8Async(string jobParams = null);
    }
}
