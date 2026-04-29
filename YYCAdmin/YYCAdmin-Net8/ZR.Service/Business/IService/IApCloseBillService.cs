using System.Data;
using ZR.Model.Business.Model.Dto;
using ZR.Service.Business.U8.Dtos;

namespace ZR.Service.Business.IService;

public interface IApCloseBillService 
{
    Task<resultDto> ApCloseBillAdd(ApCloseBillDto apCloseBill);

    DataTable getU8VouchId(string orgDbName, string cAccId, string cVouchType, int iAmount,
        string RemoteId = "00");

    string getU8VouchCode(string orgDbName, string CardNumber, string cContent, string cContentRule,
        string cSeed, string cSeedShow, int flowNum);
}