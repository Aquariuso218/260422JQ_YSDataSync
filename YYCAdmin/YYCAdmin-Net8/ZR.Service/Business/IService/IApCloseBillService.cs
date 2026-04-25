using System.Data;
using ZR.Model.Business.Model.Dto;

namespace ZR.Service.Business.IService;

public interface IApCloseBillService 
{
    Task<string> ApCloseBillAdd(ApCloseBillDto apCloseBill);

    DataTable getU8VouchId(string orgDbName, string cAccId, string cVouchType, int iAmount,
        string RemoteId = "00");

    string getU8VouchCode(string orgDbName, string CardNumber, string cContent, string cContentRule,
        string cSeed, string cSeedShow, int flowNum);
}