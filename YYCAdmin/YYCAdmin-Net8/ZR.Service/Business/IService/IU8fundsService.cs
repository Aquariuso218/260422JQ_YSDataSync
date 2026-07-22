using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZR.Model.Business.Dto;
using ZR.Service.Business.U8.Dtos;

namespace ZR.Service.Business.IService;

public interface IU8fundsService
{
    Task<resultDto> GetU8fundsYE();
}

