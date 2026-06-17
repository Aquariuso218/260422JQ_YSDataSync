using Infrastructure.Attribute;
using Infrastructure.Extensions;
using ZR.Model.Business.Dto;
using ZR.Model.Business;
using ZR.Repository;
using ZR.Service.Business.IService;

namespace ZR.Service.Business
{
    /// <summary>
    /// Service业务层处理
    /// </summary>
    [AppService(ServiceType = typeof(IEfMidysbilldataService), ServiceLifetime = LifeTime.Transient)]
    public class EfMidysbilldataService : BaseService<EfMidysbilldata>, IEfMidysbilldataService
    {
        /// <summary>
        /// 查询列表
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        public PagedInfo<EfMidysbilldataDto> GetList(EfMidysbilldataQueryDto parm)
        {
            var predicate = QueryExp(parm);

            var response = Queryable()
                .Where(predicate.ToExpression())
                .ToPage<EfMidysbilldata, EfMidysbilldataDto>(parm);

            return response;
        }


        /// <summary>
        /// 获取详情
        /// </summary>
        /// <param name="AutoId"></param>
        /// <returns></returns>
        public EfMidysbilldata GetInfo(int AutoId)
        {
            var response = Queryable()
                .Where(x => x.AutoId == AutoId)
                .First();

            return response;
        }

        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public EfMidysbilldata AddEfMidysbilldata(EfMidysbilldata model)
        {
            return Insertable(model).ExecuteReturnEntity();
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public int UpdateEfMidysbilldata(EfMidysbilldata model)
        {
            return Update(model, true);
        }

        /// <summary>
        /// 查询导出表达式
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        private static Expressionable<EfMidysbilldata> QueryExp(EfMidysbilldataQueryDto parm)
        {
            var predicate = Expressionable.Create<EfMidysbilldata>();

            predicate = predicate.AndIF(!string.IsNullOrEmpty(parm.Id), it => it.Id.Contains(parm.Id));
            predicate = predicate.AndIF(!string.IsNullOrEmpty(parm.MainId), it => it.MainId.Contains(parm.MainId));
            predicate = predicate.AndIF(!string.IsNullOrEmpty(parm.CVouchCode), it => it.CVouchCode.Contains(parm.CVouchCode));
            predicate = predicate.AndIF(parm.BeginBillDate != null, it => it.BillDate >= parm.BeginBillDate);
            predicate = predicate.AndIF(parm.EndBillDate != null, it => it.BillDate <= parm.EndBillDate);
            predicate = predicate.AndIF(parm.SettleStatus != null, it => it.SettleStatus == parm.SettleStatus);
            predicate = predicate.AndIF(!string.IsNullOrEmpty(parm.QuickTypeName), it => it.QuickTypeName.Contains(parm.QuickTypeName));
            predicate = predicate.AndIF(!string.IsNullOrEmpty(parm.CVouchType), it => it.CVouchType == parm.CVouchType);
            predicate = predicate.AndIF(!string.IsNullOrEmpty(parm.CDwType), it => it.CDwType == parm.CDwType);
            predicate = predicate.AndIF(parm.ProcessStatus != null, it => it.ProcessStatus == parm.ProcessStatus);
            predicate = predicate.AndIF(!string.IsNullOrEmpty(parm.U8Code), it => it.U8Code.Contains(parm.U8Code));

            return predicate;
        }
    }
}