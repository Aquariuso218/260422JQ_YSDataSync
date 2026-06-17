using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZR.Model.Business;
using ZR.Model.Business.Dto;

namespace ZR.Service.Business.IService
{
    /// <summary>
    /// service接口
    /// </summary>
    public interface IEfMidysbilldataService : IBaseService<EfMidysbilldata>
    {
        PagedInfo<EfMidysbilldataDto> GetList(EfMidysbilldataQueryDto parm);

        EfMidysbilldata GetInfo(int AutoId);


        EfMidysbilldata AddEfMidysbilldata(EfMidysbilldata parm);
        int UpdateEfMidysbilldata(EfMidysbilldata parm);


    }
}
