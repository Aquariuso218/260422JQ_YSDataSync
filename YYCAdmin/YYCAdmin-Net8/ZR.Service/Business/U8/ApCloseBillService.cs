using Infrastructure.Attribute;
using JinianNet.JNTemplate;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using SqlSugar.IOC;
using System.Collections.Generic;
using System.Data;
using System.Reflection.Emit;
using System.Text;
using ZR.Model.Business.Dto;
using ZR.Service.Business.IService;
using ZR.Service.Business.U8.Dtos;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace ZR.Service.Business.U8;

[AppService(ServiceType = typeof(IApCloseBillService), ServiceLifetime = LifeTime.Transient)]
public class ApCloseBillService : IApCloseBillService
{
    public ISqlSugarClient _db = DbScoped.SugarScope.GetConnectionScope("1");


    /// <summary>
    /// 收付款单生成
    /// </summary>
    /// <returns></returns>
    public async Task<resultDto> ApCloseBillAdd(ApCloseBillDto apCloseBill)
    {
        try
        {

            string message = "";
            string code = "";

            #region 收款付款单据生成

            if (apCloseBill != null)
            {
                #region 表头验证

                if (string.IsNullOrWhiteSpace(apCloseBill.orgCode))
                {
                    throw new Exception("业务组织账套号不允许为空");
                }

                //账套号
                string u8AccId = "";
                string u8DbName = "";
                string u8prefix = "";
                //获取账套库名
                DataTable orgdt =
                    _db.Ado.GetDataTable(
                        $"select u8AccId,u8DbName,u8prefix from [ZRAdmin]..OrgInfoSyn (nolock) where orgCode=@orgCode",
                        new SugarParameter[] { new SugarParameter("@orgCode", apCloseBill.orgCode) });
                if (orgdt.Rows.Count == 0)
                {
                    throw new Exception("业务账套号[" + apCloseBill.orgCode + "]未获取到数据库配置信息");
                }
                else
                {
                    u8AccId = orgdt.Rows[0]["u8AccId"].ToString();
                    u8DbName = orgdt.Rows[0]["u8DbName"].ToString();
                    u8prefix = orgdt.Rows[0]["u8prefix"].ToString();
                }

                if (string.IsNullOrWhiteSpace(apCloseBill.ID))
                {
                    throw new Exception("YS单据id不允许为空");
                }

                if (string.IsNullOrWhiteSpace(apCloseBill.cVouchCode))
                {
                    throw new Exception("YS单号不允许为空");
                }

                if (string.IsNullOrWhiteSpace(apCloseBill.billDate))
                {
                    throw new Exception("YS单据日期不允许为空");
                }
                else
                {
                    DateTime dte = DateTime.Now;
                    if (!DateTime.TryParse(apCloseBill.billDate, out dte))
                    {
                        throw new Exception("YS单据日期格式错误");
                    }
                }


                //制单人
                if (string.IsNullOrWhiteSpace(apCloseBill.CMAKER))
                {
                    throw new Exception("制单人不允许为空");
                }

                if (apCloseBill.iAmount <= 0)
                {
                    throw new Exception("金额必须大于0");
                }


                //本单位银行账号和名称
                string cNatBankAccount = apCloseBill.cNatBankAccount;
                string cNatBank = apCloseBill.cNatBank;

                #endregion

                //来源交易类型
                if (apCloseBill.tradetypeName == "到期兑付")
                {
                    #region 生成应付票据兑付凭证

                    string cVenCode = "";
                    //供应商编码
                    if (string.IsNullOrWhiteSpace(apCloseBill.cDwCode))
                    {
                        throw new Exception("往来对应编码不允许为空");
                    }
                    else
                    {
                        //获取供应商编码
                        cVenCode = _db.Ado.GetString(
                            $"select cVenCode from {u8DbName}..VenDor (nolock) where cVenCode=@cVenCode",
                            new SugarParameter[]
                                { new SugarParameter("@cVenCode", apCloseBill.cDwCode.Remove(0, u8prefix.Length)) });
                        if (string.IsNullOrWhiteSpace(cVenCode))
                        {
                            throw new Exception("往来对应编码在U8业务账套号[" + u8AccId + "]供应商档案中不存在");
                        }
                    }

                    string isignseq = "4";
                    string csign = "银行付款";

                    if (apCloseBill.orgCode == "1004")
                    {
                        csign = "04";
                    }

                    //判断凭证类别是否存在
                    string i_id =
                        _db.Ado.GetString(
                            $"select i_id from {u8DbName}..dsign (nolock) where csign='{csign}' and isignseq='{isignseq}'");
                    if (string.IsNullOrWhiteSpace(i_id))
                    {
                        throw new Exception("凭证类别字[" + csign + "]在U8账套号[" + u8AccId + "]中不存在");
                    }

                    //获取凭证业务号
                    string coutno_id =
                        _db.Ado.GetString(
                            $"declare @p4 nvarchar(17) set @p4=N'GL0000000000012' exec {u8DbName}..Ap_Proc_CancelNo N'PZ',N'GL',default,@p4 output select @p4");
                    //获取最大凭证号
                    string ino_id = _db.Ado.GetString(
                        $"SELECT isnull(max(ino_id),0)+1  from {u8DbName}..gl_accvouch (nolock) where iperiod='{DateTime.Parse(apCloseBill.billDate).Month}' and isignseq='{isignseq}' and csign='{csign}' and iyear='{DateTime.Parse(apCloseBill.billDate).Year}'");

                    message = "凭证号:" + csign + "-" + ino_id.PadLeft(4, '0');
                    code = csign + "-" + ino_id.PadLeft(4, '0');

                    //借方应付票据科目
                    string mdCode = "";
                    if (apCloseBill.orgCode == "1003" || apCloseBill.orgCode == "1004")
                    {
                        mdCode = "2201";
                        cVenCode = "";
                    }
                    else
                    {
                        if (apCloseBill.noteTypeCode == "10" || apCloseBill.noteTypeCode == "银行承兑汇票")
                        {
                            mdCode = "220101";
                        }
                        else if (apCloseBill.noteTypeCode == "20" || apCloseBill.noteTypeCode == "商业承兑汇票")
                        {
                            mdCode = "220103";
                        }
                        else if (apCloseBill.noteTypeCode == "30" || apCloseBill.noteTypeCode == "债权凭证")
                        {
                            mdCode = "220104";
                        }
                        else
                        {
                            mdCode = "220102";
                        }
                    }

                    //贷方科目（根据结算方式获取应付银行结算科目）
                    string mcCode = _db.Ado.GetString($"select cOrgNo from {u8DbName}..Bank (nolock) where cBAccount=@cBAccount",
                        new SugarParameter[] { new SugarParameter("@cBAccount", cNatBankAccount) });

                    //贷方现金科目
                    string mcxjcode = "04";

                    if (string.IsNullOrWhiteSpace(mcCode))
                    {
                        throw new Exception("银行账号[" + cNatBankAccount + "]在U8账套号[" + u8AccId + "]本企业单位银行档案未设置银行科目");
                    }

                    //判断借方科目是否存在
                    string ccode =
                        _db.Ado.GetString(
                            $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{mdCode}' and bend=1");

                    if (string.IsNullOrWhiteSpace(ccode))
                    {
                        throw new Exception("科目编码[" + mdCode + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                    }

                    //判断贷方科目是否存在
                    ccode = _db.Ado.GetString(
                        $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{mcCode}' and bend=1");

                    if (string.IsNullOrWhiteSpace(ccode))
                    {
                        throw new Exception("科目编码[" + mcCode + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                    }


                    //判断现金流量项目编码是否存在
                    string citemcode =
                        _db.Ado.GetString(
                            $"select citemcode from {u8DbName}..fitemss98 (nolock) where citemcode='{mcxjcode}'");
                    if (string.IsNullOrWhiteSpace(citemcode))
                    {
                        throw new Exception("现金流量项目编码[" + mcxjcode + "]在U8账套号[" + u8AccId + "]中不存在");
                    }

                    await _db.Ado.BeginTranAsync();

                    int inid = 1;

                    string RowGuid = Guid.NewGuid().ToString().Replace("-", "") + "00000000";

                    //摘要
                    string cdigest = apCloseBill.cVouchCode + "/票据兑付";


                    #region 总账应付票据凭证生成

                    string cDepCode = "";
                    string cPersonCode = "";

                    //总账凭证借方（应付票据科目）
                    _db.Ado.ExecuteCommand(
                        $"insert into {u8DbName}..GL_accvouch(iperiod,csign,isignseq,ino_id,inid,dbill_date,idoc,cbill,ibook,cdigest,ccode,md,mc,md_f,mc_f,nfrat,nd_s,nc_s,csettle,dt_date,cdept_id,cperson_id,ccus_id,csup_id,citem_id,citem_class,cname,ccode_equal,bdelete,coutaccset,ioutyear,coutsysname,doutbilldate,ioutperiod,coutsign,coutno_id,doutdate,coutbillsign,coutid,bvouchedit,bvouchAddordele,bvouchmoneyhold,bvalueedit,bcodeedit,ccodecontrol,bPCSedit,bDeptedit,bItemedit,bCusSupInput,cDefine12,cDefine13,cDefine14,bFlagOut,RowGuid,iyear,iYPeriod,tvouchtime,ccodeexch_equal) values('{DateTime.Parse(apCloseBill.billDate).Month}','{csign}','{isignseq}','{ino_id}','{inid}','{apCloseBill.billDate}','-1','{apCloseBill.CMAKER}',0,'{cdigest}','{mdCode}','{apCloseBill.iAmount}',0,0,0,0,0,0,Null,Null,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),Null,'{cVenCode}',Null,Null,Null,'{mcCode}',0,Null,Null,Null,'{apCloseBill.billDate}',Null,'','{coutno_id}',Null,Null,Null,1,0,0,1,1,Null,1,1,1,0,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','到期兑付',0,'{RowGuid}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),getdate(),'{mcCode}')");

                    //借方应付票据科目现金流量
                    _db.Ado.ExecuteCommand(
                        $"insert into {u8DbName}..GL_CashTable([iPeriod],[iSignSeq],[iNo_id],[inid],[cCashItem],[md],[mc],[ccode],[md_f],[mc_f],[nd_s],[nc_s],[cdept_id],[cperson_id],[ccus_id],[csup_id],[citem_class],[citem_id],cDefine12,[cDefine13],cDefine14,[dbill_date],[csign],[iyear],[iYPeriod],[RowGuid],[cexch_name]) values('{DateTime.Parse(apCloseBill.billDate).Month}','{isignseq}','{ino_id}','{inid}','{mcxjcode}','{apCloseBill.iAmount}','0','{mcCode}',0,0,0,0,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),Null,'{cVenCode}',Null,Null,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','到期兑付','{apCloseBill.billDate}','{csign}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),'{RowGuid}',Null)");

                    inid = inid + 1;

                    RowGuid = Guid.NewGuid().ToString().Replace("-", "") + "00000000";

                    //贷方（银行存款科目）
                    _db.Ado.ExecuteCommand(
                        $"insert into {u8DbName}..GL_accvouch(iperiod,csign,isignseq,ino_id,inid,dbill_date,idoc,cbill,ibook,cdigest,ccode,md,mc,md_f,mc_f,nfrat,nd_s,nc_s,csettle,dt_date,cdept_id,cperson_id,ccus_id,csup_id,citem_id,citem_class,cname,ccode_equal,bdelete,coutaccset,ioutyear,coutsysname,doutbilldate,ioutperiod,coutsign,coutno_id,doutdate,coutbillsign,coutid,bvouchedit,bvouchAddordele,bvouchmoneyhold,bvalueedit,bcodeedit,ccodecontrol,bPCSedit,bDeptedit,bItemedit,bCusSupInput,cDefine12,cDefine13,cDefine14,bFlagOut,RowGuid,iyear,iYPeriod,tvouchtime,ccodeexch_equal) values('{DateTime.Parse(apCloseBill.billDate).Month}','{csign}','{isignseq}','{ino_id}','{inid}','{apCloseBill.billDate}','-1','{apCloseBill.CMAKER}',0,'{cdigest}','{mcCode}','0','{apCloseBill.iAmount}',0,0,0,0,0,Null,Null,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),Null,Null,Null,Null,Null,'{mdCode}',0,Null,Null,Null,'{apCloseBill.billDate}',Null,'','{coutno_id}',Null,Null,Null,1,0,0,1,1,Null,1,1,1,0,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','到期兑付',0,'{RowGuid}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),getdate(),'{mdCode}')");


                    RowGuid = Guid.NewGuid().ToString().Replace("-", "") + "00000000";

                    //贷方银行存款现金流量
                    _db.Ado.ExecuteCommand(
                        $"insert into {u8DbName}..GL_CashTable([iPeriod],[iSignSeq],[iNo_id],[inid],[cCashItem],[md],[mc],[ccode],[md_f],[mc_f],[nd_s],[nc_s],[cdept_id],[cperson_id],[ccus_id],[csup_id],[citem_class],[citem_id],cDefine12,[cDefine13],cDefine14,[dbill_date],[csign],[iyear],[iYPeriod],[RowGuid],[cexch_name]) values('{DateTime.Parse(apCloseBill.billDate).Month}','{isignseq}','{ino_id}','{inid}','{mcxjcode}','0','{apCloseBill.iAmount}','{mcCode}',0,0,0,0,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),Null,Null,Null,Null,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','到期兑付','{apCloseBill.billDate}','{csign}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),'{RowGuid}',Null)");

                    #endregion

                    //查询凭证是否重复生成
                    string counts = _db.Ado.GetString(
                        $"select count(1) from {u8DbName}..gl_accvouch (nolock) where iperiod='{DateTime.Parse(apCloseBill.billDate).Month}' and isignseq='{isignseq}' and csign='{csign}' and iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ino_id='{ino_id}' and inid='1'");

                    if (int.Parse(counts) > 1)
                    {
                        throw new Exception("U8业务账套号[" + u8AccId + "]凭证号获取重复，请重试");
                    }


                    await _db.Ado.CommitTranAsync();

                    #endregion
                }
                else if (apCloseBill.tradetypeName == "银行托收")
                {
                    #region 生成应收票据承兑凭证

                    string cCusCode = "";
                    //客户编码
                    if (string.IsNullOrWhiteSpace(apCloseBill.cDwCode))
                    {
                        throw new Exception("往来对应编码不允许为空");
                    }
                    else
                    {
                        //获取客户编码
                        cCusCode = _db.Ado.GetString(
                            $"select cCusCode from {u8DbName}..Customer (nolock) where cCusCode=@cCusCode",
                            new SugarParameter[]
                                { new SugarParameter("@cCusCode", apCloseBill.cDwCode.Remove(0, u8prefix.Length)) });
                        if (string.IsNullOrWhiteSpace(cCusCode))
                        {
                            throw new Exception("往来对应编码在U8业务账套号[" + u8AccId + "]客户档案中不存在");
                        }
                    }



                    string isignseq = "2";
                    string csign = "银行收款";
                    if (apCloseBill.orgCode == "1004")
                    {
                        csign = "03";
                    }
                    else if (apCloseBill.orgCode == "1003")
                    {
                        isignseq = "3";
                    }

                    //借方科目（根据银行账户获取银行科目）
                    string mdCode = _db.Ado.GetString($"select cOrgNo from {u8DbName}..Bank (nolock) where cBAccount=@cBAccount",
                        new SugarParameter[] { new SugarParameter("@cBAccount", cNatBankAccount) });

                    if (apCloseBill.quickTypeName == "质押款")
                    {
                        isignseq = "5";
                        csign = "转账";

                        if (apCloseBill.orgCode == "1004")
                        {
                            csign = "转";
                        }

                        mdCode = "1012";
                        if (apCloseBill.orgCode == "1001")
                        {
                            mdCode = "101201";
                        }
                    }

                    //判断凭证类别是否存在
                    string i_id =
                        _db.Ado.GetString(
                            $"select i_id from {u8DbName}..dsign (nolock) where csign='{csign}' and isignseq='{isignseq}'");
                    if (string.IsNullOrWhiteSpace(i_id))
                    {
                        throw new Exception("凭证类别字[" + csign + "]在U8账套号[" + u8AccId + "]中不存在");
                    }

                    //获取凭证业务号
                    string coutno_id =
                        _db.Ado.GetString(
                            $"declare @p4 nvarchar(17) set @p4=N'GL0000000000012' exec {u8DbName}..Ap_Proc_CancelNo N'PZ',N'GL',default,@p4 output select @p4");
                    //获取最大凭证号
                    string ino_id = _db.Ado.GetString(
                        $"SELECT isnull(max(ino_id),0)+1  from {u8DbName}..gl_accvouch (nolock) where iperiod='{DateTime.Parse(apCloseBill.billDate).Month}' and isignseq='{isignseq}' and csign='{csign}' and iyear='{DateTime.Parse(apCloseBill.billDate).Year}'");

                    message = "凭证号:" + csign + "-" + ino_id.PadLeft(4, '0'); //ino_id需要为四位编码，需要补零
                    code = csign + "-" + ino_id.PadLeft(4, '0');

                    if (string.IsNullOrWhiteSpace(mdCode))
                    {
                        throw new Exception("银行账号[" + cNatBankAccount + "]在U8账套号[" + u8AccId + "]本企业单位银行档案未设置银行科目");
                    }

                    //贷方现金科目
                    string mdxjcode = "01";


                    //贷方科目（应收票据科目）
                    string mcCode = "";
                    if (apCloseBill.orgCode == "1003" || apCloseBill.orgCode == "1004")
                    {
                        mcCode = "1121";
                        cCusCode = "";
                    }
                    else
                    {
                        if (apCloseBill.noteTypeCode == "10" || apCloseBill.noteTypeCode == "银行承兑汇票")
                        {
                            mcCode = "112101";
                        }
                        else if (apCloseBill.noteTypeCode == "20" || apCloseBill.noteTypeCode == "商业承兑汇票")
                        {
                            mcCode = "112103";
                        }
                        else if (apCloseBill.noteTypeCode == "30" || apCloseBill.noteTypeCode == "债权凭证")
                        {
                            mcCode = "112102";
                        }
                        else
                        {
                            mcCode = "112104";
                        }
                    }

                    //判断借方科目是否存在
                    string ccode =
                        _db.Ado.GetString(
                            $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{mdCode}' and bend=1");

                    if (string.IsNullOrWhiteSpace(ccode))
                    {
                        throw new Exception("科目编码[" + mdCode + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                    }

                    //判断贷方科目是否存在
                    ccode = _db.Ado.GetString(
                        $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{mcCode}' and bend=1");

                    if (string.IsNullOrWhiteSpace(ccode))
                    {
                        throw new Exception("科目编码[" + mcCode + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                    }

                    //判断现金流量项目编码是否存在
                    string citemcode =
                        _db.Ado.GetString(
                            $"select citemcode from {u8DbName}..fitemss98 (nolock) where citemcode='{mdxjcode}'");
                    if (string.IsNullOrWhiteSpace(citemcode))
                    {
                        throw new Exception("现金流量项目编码[" + mdxjcode + "]在U8账套号[" + u8AccId + "]中不存在");
                    }

                    await _db.Ado.BeginTranAsync();

                    int inid = 1;

                    string RowGuid = Guid.NewGuid().ToString().Replace("-", "") + "00000000";

                    //摘要
                    string cdigest = apCloseBill.cVouchCode + "/票据结算";

                    #region 总账应收票据凭证生成

                    string cDepCode = "";
                    string cPersonCode = "";

                    //凭证借方（银行存款科目）
                    _db.Ado.ExecuteCommand(
                        $"insert into {u8DbName}..GL_accvouch(iperiod,csign,isignseq,ino_id,inid,dbill_date,idoc,cbill,ibook,cdigest,ccode,md,mc,md_f,mc_f,nfrat,nd_s,nc_s,csettle,dt_date,cdept_id,cperson_id,ccus_id,csup_id,citem_id,citem_class,cname,ccode_equal,bdelete,coutaccset,ioutyear,coutsysname,doutbilldate,ioutperiod,coutsign,coutno_id,doutdate,coutbillsign,coutid,bvouchedit,bvouchAddordele,bvouchmoneyhold,bvalueedit,bcodeedit,ccodecontrol,bPCSedit,bDeptedit,bItemedit,bCusSupInput,cDefine12,cDefine13,cDefine14,bFlagOut,RowGuid,iyear,iYPeriod,tvouchtime,ccodeexch_equal) values('{DateTime.Parse(apCloseBill.billDate).Month}','{csign}','{isignseq}','{ino_id}','{inid}','{apCloseBill.billDate}','-1','{apCloseBill.CMAKER}',0,'{cdigest}','{mdCode}','{apCloseBill.iAmount}',0,0,0,0,0,0,Null,Null,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),Null,Null,Null,Null,Null,'{mcCode}',0,Null,Null,Null,'{apCloseBill.billDate}',Null,'','{coutno_id}',Null,Null,Null,1,0,0,1,1,Null,1,1,1,0,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','银行托收',0,'{RowGuid}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),getdate(),'{mcCode}')");


                    RowGuid = Guid.NewGuid().ToString().Replace("-", "") + "00000000";

                    if (apCloseBill.quickTypeName != "质押款")
                    {
                        //借方银行存款科目现金流量
                        _db.Ado.ExecuteCommand(
                            $"insert into GL_CashTable([iPeriod],[iSignSeq],[iNo_id],[inid],[cCashItem],[md],[mc],[ccode],[md_f],[mc_f],[nd_s],[nc_s],[cdept_id],[cperson_id],[ccus_id],[csup_id],[citem_class],[citem_id],[cDefine12],[cDefine13],[cDefine14],[dbill_date],[csign],[iyear],[iYPeriod],[RowGuid],[cexch_name]) values('{DateTime.Parse(apCloseBill.billDate).Month}','{isignseq}','{ino_id}','{inid}','{mdxjcode}','{apCloseBill.iAmount}',0,'{mdCode}',0,0,0,0,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),Null,Null,Null,Null,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','银行托收','{apCloseBill.billDate}','{csign}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),'{RowGuid}',Null)");
                    }

                    inid = inid + 1;

                    RowGuid = Guid.NewGuid().ToString().Replace("-", "") + "00000000";

                    //贷方（应收票据科目）
                    _db.Ado.ExecuteCommand(
                        $"insert into {u8DbName}..GL_accvouch(iperiod,csign,isignseq,ino_id,inid,dbill_date,idoc,cbill,ibook,cdigest,ccode,md,mc,md_f,mc_f,nfrat,nd_s,nc_s,csettle,dt_date,cdept_id,cperson_id,ccus_id,csup_id,citem_id,citem_class,cname,ccode_equal,bdelete,coutaccset,ioutyear,coutsysname,doutbilldate,ioutperiod,coutsign,coutno_id,doutdate,coutbillsign,coutid,bvouchedit,bvouchAddordele,bvouchmoneyhold,bvalueedit,bcodeedit,ccodecontrol,bPCSedit,bDeptedit,bItemedit,bCusSupInput,cDefine12,cDefine13,cDefine14,bFlagOut,RowGuid,iyear,iYPeriod,tvouchtime,ccodeexch_equal) values('{DateTime.Parse(apCloseBill.billDate).Month}','{csign}','{isignseq}','{ino_id}','{inid}','{apCloseBill.billDate}','-1','{apCloseBill.CMAKER}',0,'{cdigest}','{mcCode}','0','{apCloseBill.iAmount}',0,0,0,0,0,Null,Null,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),nullif(N'{cCusCode}',''),Null,Null,Null,Null,'{mdCode}',0,Null,Null,Null,'{apCloseBill.billDate}',Null,'','{coutno_id}',Null,Null,Null,1,0,0,1,1,Null,1,1,1,0,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','银行托收',0,'{RowGuid}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),getdate(),'{mdCode}')");


                    if (apCloseBill.quickTypeName != "质押款")
                    {
                        //贷方（应收票据科目）现金流量
                        _db.Ado.ExecuteCommand(
                            $"insert into GL_CashTable([iPeriod],[iSignSeq],[iNo_id],[inid],[cCashItem],[md],[mc],[ccode],[md_f],[mc_f],[nd_s],[nc_s],[cdept_id],[cperson_id],[ccus_id],[csup_id],[citem_class],[citem_id],[cDefine12],[cDefine13],[cDefine14],[dbill_date],[csign],[iyear],[iYPeriod],[RowGuid],[cexch_name]) values('{DateTime.Parse(apCloseBill.billDate).Month}','{isignseq}','{ino_id}','{inid}','{mdxjcode}','0','{apCloseBill.iAmount}','{mcCode}',0,0,0,0,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),nullif(N'{cCusCode}',''),Null,Null,Null,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','银行托收','{apCloseBill.billDate}','{csign}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),'{RowGuid}',Null)");
                    }

                    #endregion


                    //查询凭证是否重复生成
                    string counts = _db.Ado.GetString(
                        $"select count(1) from {u8DbName}..gl_accvouch (nolock) where iperiod='{DateTime.Parse(apCloseBill.billDate).Month}' and isignseq='{isignseq}' and csign='{csign}' and iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ino_id='{ino_id}' and inid='1'");

                    if (int.Parse(counts) > 1)
                    {
                        throw new Exception("U8业务账套号[" + u8AccId + "]凭证号获取重复，请重试");
                    }

                    await _db.Ado.CommitTranAsync();

                    #endregion
                }
                else if (apCloseBill.tradetypeName == "贴现办理")
                {
                    #region 生成应收票据贴现凭证

                    string cCusCode = "";
                    //客户编码
                    if (string.IsNullOrWhiteSpace(apCloseBill.cDwCode))
                    {
                        throw new Exception("往来对应编码不允许为空");
                    }
                    else
                    {
                        //获取客户编码
                        cCusCode = _db.Ado.GetString(
                            $"select cCusCode from {u8DbName}..Customer (nolock) where cCusCode=@cCusCode",
                            new SugarParameter[]
                                { new SugarParameter("@cCusCode", apCloseBill.cDwCode.Remove(0, u8prefix.Length)) });
                        if (string.IsNullOrWhiteSpace(cCusCode))
                        {
                            throw new Exception("往来对应编码在U8业务账套号[" + u8AccId + "]客户档案中不存在");
                        }
                    }

                    if (string.IsNullOrWhiteSpace(cNatBankAccount))
                    {
                        throw new Exception("本企业单位银行账号不允许为空");
                    }

                    string isignseq = "2";
                    string csign = "银行收款";
                    if (apCloseBill.orgCode == "1004")
                    {
                        csign = "03";
                    }
                    else if (apCloseBill.orgCode == "1003")
                    {
                        isignseq = "3";
                    }

                    //判断凭证类别是否存在
                    string i_id =
                        _db.Ado.GetString(
                            $"select i_id from {u8DbName}..dsign (nolock) where csign='{csign}' and isignseq='{isignseq}'");
                    if (string.IsNullOrWhiteSpace(i_id))
                    {
                        throw new Exception("凭证类别字[" + csign + "]在U8账套号[" + u8AccId + "]中不存在");
                    }


                    //获取凭证业务号
                    string coutno_id =
                        _db.Ado.GetString(
                            $"declare @p4 nvarchar(17) set @p4=N'GL0000000000012' exec {u8DbName}..Ap_Proc_CancelNo N'PZ',N'GL',default,@p4 output select @p4");
                    //获取最大凭证号
                    string ino_id = _db.Ado.GetString(
                        $"SELECT isnull(max(ino_id),0)+1  from {u8DbName}..gl_accvouch (nolock) where iperiod='{DateTime.Parse(apCloseBill.billDate).Month}' and isignseq='{isignseq}' and csign='{csign}' and iyear='{DateTime.Parse(apCloseBill.billDate).Year}'");

                    message = "凭证号:" + csign + "-" + ino_id.PadLeft(4, '0'); //ino_id需要为四位编码，需要补零
                    code = csign + "-" + ino_id.PadLeft(4, '0');


                    //借方科目（根据结算方式获取应付银行结算科目）
                    string mdCode = _db.Ado.GetString($"select cOrgNo from {u8DbName}..Bank (nolock) where cBAccount=@cBAccount",
                        new SugarParameter[] { new SugarParameter("@cBAccount", cNatBankAccount) });

                    if (string.IsNullOrWhiteSpace(mdCode))
                    {
                        throw new Exception("银行账号[" + cNatBankAccount + "]在U8账套号[" + u8AccId + "]本企业单位银行档案未设置银行科目");
                    }

                    //借方现金科目
                    string mdxjcode = "01";

                    //贷方科目（应收票据科目）
                    string mcCode = "";
                    if (apCloseBill.orgCode == "1003" || apCloseBill.orgCode == "1004")
                    {
                        mcCode = "1121";
                        cCusCode = "";
                    }
                    else
                    {
                        if (apCloseBill.noteTypeCode == "10" || apCloseBill.noteTypeCode == "银行承兑汇票")
                        {
                            mcCode = "112101";
                        }
                        else if (apCloseBill.noteTypeCode == "20" || apCloseBill.noteTypeCode == "商业承兑汇票")
                        {
                            mcCode = "112103";
                        }
                        else if (apCloseBill.noteTypeCode == "30" || apCloseBill.noteTypeCode == "债权凭证")
                        {
                            mcCode = "112102";
                        }
                        else
                        {
                            mcCode = "112104";
                        }
                    }

                    //贴现利息科目
                    string LlixicCode = "660304";

                    //判断借方科目是否存在
                    string ccode =
                        _db.Ado.GetString(
                            $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{mdCode}' and bend=1");

                    if (string.IsNullOrWhiteSpace(ccode))
                    {
                        throw new Exception("科目编码[" + mdCode + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                    }

                    //判断贷方科目是否存在
                    ccode = _db.Ado.GetString(
                        $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{mcCode}' and bend=1");

                    if (string.IsNullOrWhiteSpace(ccode))
                    {
                        throw new Exception("科目编码[" + mcCode + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                    }

                    //判断借方利息科目是否存在
                    ccode = _db.Ado.GetString(
                        $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{LlixicCode}' and bend=1");

                    if (string.IsNullOrWhiteSpace(ccode))
                    {
                        throw new Exception("科目编码[" + LlixicCode + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                    }

                    //判断现金流量项目编码是否存在
                    string citemcode =
                        _db.Ado.GetString(
                            $"select citemcode from {u8DbName}..fitemss98 (nolock) where citemcode='{mdxjcode}'");
                    if (string.IsNullOrWhiteSpace(citemcode))
                    {
                        throw new Exception("现金流量项目编码[" + mdxjcode + "]在U8账套号[" + u8AccId + "]中不存在");
                    }

                    await _db.Ado.BeginTranAsync();

                    int inid = 1;

                    string RowGuid = Guid.NewGuid().ToString().Replace("-", "") + "00000000";

                    //摘要
                    string cdigest = apCloseBill.cVouchCode + "/票据贴现";

                    #region 总账应收票据凭证生成

                    string cDepCode = "";
                    string cPersonCode = "";

                    //银行存款科目金额
                    decimal bankiAmount = apCloseBill.iAmount - apCloseBill.discountInterest;

                    //凭证借方（银行存款科目）
                    _db.Ado.ExecuteCommand(
                        $"insert into {u8DbName}..GL_accvouch(iperiod,csign,isignseq,ino_id,inid,dbill_date,idoc,cbill,ibook,cdigest,ccode,md,mc,md_f,mc_f,nfrat,nd_s,nc_s,csettle,dt_date,cdept_id,cperson_id,ccus_id,csup_id,citem_id,citem_class,cname,ccode_equal,bdelete,coutaccset,ioutyear,coutsysname,doutbilldate,ioutperiod,coutsign,coutno_id,doutdate,coutbillsign,coutid,bvouchedit,bvouchAddordele,bvouchmoneyhold,bvalueedit,bcodeedit,ccodecontrol,bPCSedit,bDeptedit,bItemedit,bCusSupInput,cDefine12,cDefine13,cDefine14,bFlagOut,RowGuid,iyear,iYPeriod,tvouchtime,ccodeexch_equal) values('{DateTime.Parse(apCloseBill.billDate).Month}','{csign}','{isignseq}','{ino_id}','{inid}','{apCloseBill.billDate}','-1','{apCloseBill.CMAKER}',0,'{cdigest}','{mdCode}','{bankiAmount}',0,0,0,0,0,0,Null,Null,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),Null,Null,Null,Null,Null,'{mcCode}',0,Null,Null,Null,'{apCloseBill.billDate}',Null,'','{coutno_id}',Null,Null,Null,1,0,0,1,1,Null,1,1,1,0,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','贴现办理',0,'{RowGuid}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),getdate(),'{mcCode}')");


                    RowGuid = Guid.NewGuid().ToString().Replace("-", "") + "00000000";

                    //借方银行存款科目现金流量
                    _db.Ado.ExecuteCommand(
                        $"insert into GL_CashTable([iPeriod],[iSignSeq],[iNo_id],[inid],[cCashItem],[md],[mc],[ccode],[md_f],[mc_f],[nd_s],[nc_s],[cdept_id],[cperson_id],[ccus_id],[csup_id],[citem_class],[citem_id],[cDefine12],[cDefine13],[cDefine14],[dbill_date],[csign],[iyear],[iYPeriod],[RowGuid],[cexch_name]) values('{DateTime.Parse(apCloseBill.billDate).Month}','{isignseq}','{ino_id}','{inid}','{mdxjcode}','{bankiAmount}',0,'{mdCode}',0,0,0,0,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),Null,Null,Null,Null,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','贴现办理','{apCloseBill.billDate}','{csign}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),'{RowGuid}',Null)");

                    if (apCloseBill.discountInterest > 0)
                    {
                        inid = inid + 1;

                        RowGuid = Guid.NewGuid().ToString().Replace("-", "") + "00000000";


                        //凭证借方利息科目
                        _db.Ado.ExecuteCommand(
                            $"insert into {u8DbName}..GL_accvouch(iperiod,csign,isignseq,ino_id,inid,dbill_date,idoc,cbill,ibook,cdigest,ccode,md,mc,md_f,mc_f,nfrat,nd_s,nc_s,csettle,dt_date,cdept_id,cperson_id,ccus_id,csup_id,citem_id,citem_class,cname,ccode_equal,bdelete,coutaccset,ioutyear,coutsysname,doutbilldate,ioutperiod,coutsign,coutno_id,doutdate,coutbillsign,coutid,bvouchedit,bvouchAddordele,bvouchmoneyhold,bvalueedit,bcodeedit,ccodecontrol,bPCSedit,bDeptedit,bItemedit,bCusSupInput,cDefine12,cDefine13,cDefine14,bFlagOut,RowGuid,iyear,iYPeriod,tvouchtime,ccodeexch_equal) values('{DateTime.Parse(apCloseBill.billDate).Month}','{csign}','{isignseq}','{ino_id}','{inid}','{apCloseBill.billDate}','-1','{apCloseBill.CMAKER}',0,'{cdigest}','{LlixicCode}','{apCloseBill.discountInterest}',0,0,0,0,0,0,Null,Null,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),Null,Null,Null,Null,Null,'{mcCode}',0,Null,Null,Null,'{apCloseBill.billDate}',Null,'','{coutno_id}',Null,Null,Null,1,0,0,1,1,Null,1,1,1,0,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','贴现办理',0,'{RowGuid}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),getdate(),'{mcCode}')");
                    }




                    inid = inid + 1;

                    RowGuid = Guid.NewGuid().ToString().Replace("-", "") + "00000000";

                    //贷方（应收票据科目）
                    _db.Ado.ExecuteCommand(
                        $"insert into {u8DbName}..GL_accvouch(iperiod,csign,isignseq,ino_id,inid,dbill_date,idoc,cbill,ibook,cdigest,ccode,md,mc,md_f,mc_f,nfrat,nd_s,nc_s,csettle,dt_date,cdept_id,cperson_id,ccus_id,csup_id,citem_id,citem_class,cname,ccode_equal,bdelete,coutaccset,ioutyear,coutsysname,doutbilldate,ioutperiod,coutsign,coutno_id,doutdate,coutbillsign,coutid,bvouchedit,bvouchAddordele,bvouchmoneyhold,bvalueedit,bcodeedit,ccodecontrol,bPCSedit,bDeptedit,bItemedit,bCusSupInput,cDefine12,cDefine13,cDefine14,bFlagOut,RowGuid,iyear,iYPeriod,tvouchtime,ccodeexch_equal) values('{DateTime.Parse(apCloseBill.billDate).Month}','{csign}','{isignseq}','{ino_id}','{inid}','{apCloseBill.billDate}','-1','{apCloseBill.CMAKER}',0,'{cdigest}','{mcCode}','0','{apCloseBill.iAmount}',0,0,0,0,0,Null,Null,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),nullif(N'{cCusCode}',''),Null,Null,Null,Null,'{mdCode + "," + LlixicCode}',0,Null,Null,Null,'{apCloseBill.billDate}',Null,'','{coutno_id}',Null,Null,Null,1,0,0,1,1,Null,1,1,1,0,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','贴现办理',0,'{RowGuid}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),getdate(),'{mdCode + "," + LlixicCode}')");


                    //贷方（应收票据科目）现金流量
                    _db.Ado.ExecuteCommand(
                        $"insert into GL_CashTable([iPeriod],[iSignSeq],[iNo_id],[inid],[cCashItem],[md],[mc],[ccode],[md_f],[mc_f],[nd_s],[nc_s],[cdept_id],[cperson_id],[ccus_id],[csup_id],[citem_class],[citem_id],[cDefine12],[cDefine13],[cDefine14],[dbill_date],[csign],[iyear],[iYPeriod],[RowGuid],[cexch_name]) values('{DateTime.Parse(apCloseBill.billDate).Month}','{isignseq}','{ino_id}','{inid}','{mdxjcode}','0','{apCloseBill.iAmount}','{mcCode}',0,0,0,0,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),nullif(N'{cCusCode}',''),Null,Null,Null,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','贴现办理','{apCloseBill.billDate}','{csign}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),'{RowGuid}',Null)");

                    #endregion


                    //查询凭证是否重复生成
                    string counts = _db.Ado.GetString(
                        $"select count(1) from {u8DbName}..gl_accvouch (nolock) where iperiod='{DateTime.Parse(apCloseBill.billDate).Month}' and isignseq='{isignseq}' and csign='{csign}' and iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ino_id='{ino_id}' and inid='1'");

                    if (int.Parse(counts) > 1)
                    {
                        throw new Exception("U8业务账套号[" + u8AccId + "]凭证号获取重复，请重试");
                    }

                    await _db.Ado.CommitTranAsync();

                    #endregion
                }
                else
                {
                    string cDepCode = "";
                    //部门编码
                    if (string.IsNullOrWhiteSpace(apCloseBill.cDepCode))
                    {
                        throw new Exception("部门编码不允许为空");
                    }
                    else
                    {
                        //获取部门编码
                        cDepCode = _db.Ado.GetString(
                            $"select cDepCode from {u8DbName}..Department (nolock) where cDepCode=@cDepCode and bDepEnd=1",
                            new SugarParameter[]
                                { new SugarParameter("@cDepCode", apCloseBill.cDepCode.Remove(0, u8prefix.Length)) });
                        if (string.IsNullOrWhiteSpace(cDepCode))
                        {
                            throw new Exception("部门编码在U8业务账套号[" + u8AccId + "]部门档案中不存在");
                        }
                    }

                    string cPersonCode = "";
                    //人员编码
                    if (!string.IsNullOrWhiteSpace(apCloseBill.cPersonCode))
                    {
                        //获取人员编码
                        cPersonCode = _db.Ado.GetString(
                            $"select cPersonCode from {u8DbName}..Person (nolock) where cPersonCode=@cPersonCode",
                            new SugarParameter[]
                            {
                                new SugarParameter("@cPersonCode", apCloseBill.cPersonCode.Remove(0, u8prefix.Length))
                            });
                    }


                    if (string.IsNullOrWhiteSpace(apCloseBill.cVouchType))
                    {
                        throw new Exception("单据类型不允许为空");
                    }
                    else
                    {
                        if (apCloseBill.cVouchType != "资金收款" && apCloseBill.cVouchType != "资金付款")
                        {
                            throw new Exception("单据款项类型必须为（资金收款，资金付款）中的其中一种");
                        }
                    }

                    string cSSCode = "";
                    //结算方式编码
                    if (string.IsNullOrWhiteSpace(apCloseBill.cSSName))
                    {
                        throw new Exception("结算方式名称不允许为空");
                    }
                    else
                    {
                        //获取结算方式编码
                        cSSCode = _db.Ado.GetString(
                            $"select cSSCode from {u8DbName}..SettleStyle (nolock) where cSSName=@cSSName and bSSEnd=1",
                            new SugarParameter[] { new SugarParameter("@cSSName", apCloseBill.cSSName) });
                        if (string.IsNullOrWhiteSpace(cSSCode))
                        {
                            throw new Exception("结算方式名称在U8业务账套号[" + u8AccId + "]结算方式档案中不存在");
                        }
                    }

                    if (string.IsNullOrWhiteSpace(apCloseBill.cDwType))
                    {
                        throw new Exception("往来对象类型不允许为空");
                    }
                    else
                    {
                        if (apCloseBill.cDwType != "客户" && apCloseBill.cDwType != "供应商")
                        {
                            throw new Exception("往来对象类型必须为（客户，供应商）中的其中一种");
                        }
                    }

                    DataTable dtId = getU8VouchId(u8DbName, u8AccId, "SK", 1);

                    //收款单主表ID
                    string ZBID = dtId.Rows[0][0].ToString();
                    //收款单子表id
                    string ZBIDs = dtId.Rows[0][1].ToString();

                    if (apCloseBill.cVouchType == "资金收款")
                    {
                        #region 资金收款业务

                        if (apCloseBill.cDwType == "客户")
                        {
                            #region 蓝字收款单

                            string cCusCode = "";
                            //客户编码
                            if (string.IsNullOrWhiteSpace(apCloseBill.cDwCode))
                            {
                                throw new Exception("往来对应编码不允许为空");
                            }
                            else
                            {
                                //获取客户编码
                                cCusCode = _db.Ado.GetString(
                                    $"select cCusCode from {u8DbName}..Customer (nolock) where cCusCode=@cCusCode",
                                    new SugarParameter[]
                                    {
                                        new SugarParameter("@cCusCode", apCloseBill.cDwCode.Remove(0, u8prefix.Length))
                                    });
                                if (string.IsNullOrWhiteSpace(cCusCode))
                                {
                                    throw new Exception("往来对应编码在U8业务账套号[" + u8AccId + "]客户档案中不存在");
                                }
                            }


                            //获取蓝字收款单号
                            string cSeed = DateTime.Now.ToString("yyMMdd").ToString();
                            string cSeedShow = "sk" + cSeed;
                            string cSeedCode = DateTime.Now.ToString("yyyyMM").ToString();
                            //收款单蓝字单据号
                            string cVouchID = getU8VouchCode(u8DbName, "RR", "单据日期", "月", cSeedCode, cSeedShow, 3);

                            message = "蓝字收款单号:" + cVouchID;
                            code = cVouchID;

                            await _db.Ado.BeginTranAsync();

                            //科目编码（根据结算方式获取应收结算科目）
                            string KmcCode = _db.Ado.GetString(
                                $"select cCode from {u8DbName}..Ap_SStyleCode (nolock) where iyear={DateTime.Parse(apCloseBill.billDate).Year} and cSettleStyle='{cSSCode}' and cFlag='AR'");

                            //汇率
                            string iExchRate = "1";
                            //币种
                            string cexch_name = "人民币";

                            #region 蓝字收款单主表

                            //来源票据号
                            if (!string.IsNullOrWhiteSpace(apCloseBill.cNoteCode))
                            {
                                //表头结算科目（应收票据科目）
                                if (apCloseBill.orgCode == "1003" || apCloseBill.orgCode == "1004")
                                {
                                    KmcCode = "1121";
                                }
                                else
                                {
                                    if (apCloseBill.cSSName == "银行承兑汇票")
                                    {
                                        KmcCode = "112101";
                                    }
                                    else if (apCloseBill.cSSName == "商业承兑汇票")
                                    {
                                        KmcCode = "112103";
                                    }
                                    else if (apCloseBill.cSSName == "债权凭证")
                                    {
                                        KmcCode = "112102";
                                    }
                                    else
                                    {
                                        KmcCode = "112104";
                                    }
                                }
                            }

                            //判断主表结算科目
                            if (!string.IsNullOrWhiteSpace(KmcCode))
                            {
                                //判断借方科目是否存在
                                string ccode = _db.Ado.GetString(
                                    $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{KmcCode}' and bend=1");

                                if (string.IsNullOrWhiteSpace(ccode))
                                {
                                    throw new Exception("科目编码[" + KmcCode + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                                }
                            }


                            //收款单主表
                            _db.Ado.ExecuteCommand(
                                $"Insert Into {u8DbName}..Ap_CloseBill (cVouchType,cVouchID,dVouchDate,iPeriod,cDwCode,cDeptCode,cPerson,cItem_Class,cSSCode,cNoteNo,cCoVouchType,cCoVouchID,cDigest,cexch_name,iExchRate,iAmount,iAmount_f,iRAmount,iRAmount_f,cOperator,cCancelMan,bStartFlag,cCode,iPayForOther,cPzID,cFlag,iID,cCancelNo,bFromBank,bToBank,bSure,VT_ID,cCheckMan,cDefine1,iAmount_s,IsWfControlled,iSource,iverifystate,dcreatesystime,dverifysystime,dverifydate,cPZNum,doutbilldate,iPayType,csysbarcode,cBank,cBankAccount,cNatBank,cNatBankAccount,cDefine13,cDefine14,cDefine12) select N'48',N'{cVouchID}','{apCloseBill.billDate}','{DateTime.Parse(apCloseBill.billDate).Month}',N'{cCusCode}','{cDepCode}',nullif(N'{cPersonCode}',''),null,N'{cSSCode}',null,null,null,@cDigest,N'{cexch_name}',{iExchRate},{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},@CMAKER,null,0,nullif(N'{KmcCode}',''),0,null,N'AR','{ZBID}',null,0,0,0,8052,@CMAKER,@ID,0,null,null,null,GETDATE(),GETDATE(),'{apCloseBill.billDate}',null,null,0,N'||ar48|{cVouchID}',cCusBank,cCusAccount,@cNatBank,@cNatBankAccount,@cNoteCode,@bizbillno,@cVouchCode from {u8DbName}..Customer (nolock) where cCusCode='{cCusCode}'",
                                new SugarParameter[]
                                {
                                    new SugarParameter("@cDigest", apCloseBill.cDigest),
                                    new SugarParameter("@CMAKER", apCloseBill.CMAKER),
                                    new SugarParameter("@ID", apCloseBill.ID),
                                    new SugarParameter("@cNatBank", apCloseBill.cNatBank),
                                    new SugarParameter("@cNatBankAccount", apCloseBill.cNatBankAccount),
                                    new SugarParameter("@cNoteCode", apCloseBill.cNoteCode),
                                    new SugarParameter("@bizbillno", apCloseBill.bizbillno),
                                    new SugarParameter("@cVouchCode", apCloseBill.cVouchCode)
                                });


                            //是否存在扩展自定义项表
                            string sqlext = _db.Ado.GetString(
                                $"IF EXISTS(SELECT name FROM {u8DbName}..[sysobjects] WHERE name = 'Ap_CloseBill_extradefine') select '1' ELSE select '0'");
                            if (sqlext == "1")
                            {
                                _db.Ado.ExecuteCommand(
                                    $"insert into {u8DbName}..Ap_CloseBill_extradefine(iID) values('{ZBID}')");
                            }

                            //主表明细账
                            _db.Ado.ExecuteCommand(
                                $"Insert Into  {u8DbName}..Ar_Detail(iPeriod,cVouchType,cVouchID,dVouchDate,dRegDate,cDwCode,cDeptCode,cPerson,iBVid,cCode,iSignSeq,ino_id,cDigest,iPrice,cExch_name,iExchRate,iDAmount,iCAmount,iDAmount_f,iCAmount_f,iDAmount_s,iCAmount_s,cOrderNo,cSSCode,cProcStyle,cCancelNo,cPZid,bPrePay,iFlag,cCoVouchType,cCoVouchID,cFlag,iClosesID,iCoClosesID,cGLSign,iGLno_id,dPZDate,cOperator,cCheckMan,iAmount,iAmount_f,iAmount_s,iVouchAmount,iVouchAmount_f,iVouchAmount_s,cBusType) values('{DateTime.Parse(apCloseBill.billDate).Month}','48','{cVouchID}','{apCloseBill.billDate}','{apCloseBill.billDate}','{cCusCode}','{cDepCode}',nullif(N'{cPersonCode}',''),0,nullif(N'{KmcCode}',''),Null,Null,@cDigest,0,'{cexch_name}','{iExchRate}',{apCloseBill.iAmount},0,{apCloseBill.iAmount},0,0,0,Null,'{cSSCode}','48','AR48{cVouchID}',Null,0,6,'48','{cVouchID}','AR',0,0,Null,Null,'{apCloseBill.billDate}',@CMAKER,@CMAKER,Null,Null,Null,Null,Null,Null,Null)",
                                new SugarParameter[]
                                {
                                    new SugarParameter("@cDigest", apCloseBill.cDigest),
                                    new SugarParameter("@CMAKER", apCloseBill.CMAKER)
                                });

                            #endregion

                            #region 蓝字收款单子表

                            string iType = "0";
                            string bPrePay = "0";
                            string iFlag = "0";
                            //子表科目编码
                            string cKm = "";
                            if (apCloseBill.quickTypeName == "预收款")
                            {
                                bPrePay = "1";
                                iType = "1";
                            }
                            else if (apCloseBill.quickTypeName == "费用")
                            {
                                iFlag = "10";
                                iType = "2";
                            }

                            if (iType == "0")
                            {
                                //获取应收款基本科目
                                cKm = _db.Ado.GetString(
                                    $"select cArCode from {u8DbName}..Ap_InputCode (nolock) where iyear={DateTime.Parse(apCloseBill.billDate).Year} and cFlag='R' and cNote_f='kzkm'");
                            }
                            else if (iType == "1")
                            {
                                //获取预收款基本科目
                                cKm = _db.Ado.GetString(
                                    $"select cArCode from {u8DbName}..Ap_InputCode (nolock) where iyear={DateTime.Parse(apCloseBill.billDate).Year} and cFlag='R' and cNote_f='prekm'");
                            }

                            //判断子表科目
                            if (!string.IsNullOrWhiteSpace(cKm))
                            {
                                //判断贷方科目是否存在
                                string ccode = _db.Ado.GetString(
                                    $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{cKm}' and bend=1");

                                if (string.IsNullOrWhiteSpace(ccode))
                                {
                                    throw new Exception("科目编码[" + cKm + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                                }
                            }

                            //收款单子表
                            _db.Ado.ExecuteCommand(
                                $"insert into {u8DbName}..Ap_CloseBills(iID,ID,iType,bPrePay,cCusVen,iAmt_f,iAmt,iRAmt_f,iRAmt,cKm,cXmClass,cDepCode,cPersonCode,iAmt_s,iRAmt_s,iOrderType,ccItemCode,RegisterFlag,iSrcClosesID,ifaresettled_f) values('{ZBID}','{ZBIDs}','{iType}','{bPrePay}','{cCusCode}',{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},nullif(N'{cKm}',''),Null,'{cDepCode}',nullif(N'{cPersonCode}',''),0,0,Null,Null,0,0,0)");

                            //是否存在扩展自定义项表
                            sqlext = _db.Ado.GetString(
                                $"IF EXISTS(SELECT name FROM {u8DbName}..[sysobjects] WHERE name = 'Ap_CloseBills_extradefine') select '1' ELSE select '0'");
                            if (sqlext == "1")
                            {
                                _db.Ado.ExecuteCommand(
                                    $"insert into {u8DbName}..Ap_CloseBills_extradefine(ID) values('{ZBIDs}')");
                            }

                            //子表明细账
                            _db.Ado.ExecuteCommand(
                                $"Insert Into  {u8DbName}..Ar_Detail(iPeriod,cVouchType,cVouchID,dVouchDate,dRegDate,cDwCode,cDeptCode,cPerson,iBVid,cCode,iSignSeq,ino_id,cDigest,iPrice,cExch_name,iExchRate,iDAmount,iCAmount,iDAmount_f,iCAmount_f,iDAmount_s,iCAmount_s,cOrderNo,cSSCode,cProcStyle,cCancelNo,cPZid,bPrePay,iFlag,cCoVouchType,cCoVouchID,cFlag,iClosesID,iCoClosesID,cGLSign,iGLno_id,dPZDate,cOperator,cCheckMan,iAmount,iAmount_f,iAmount_s,iVouchAmount,iVouchAmount_f,iVouchAmount_s,cBusType) values('{DateTime.Parse(apCloseBill.billDate).Month}','48','{cVouchID}','{apCloseBill.billDate}','{apCloseBill.billDate}','{cCusCode}','{cDepCode}',nullif(N'{cPersonCode}',''),0,nullif(N'{cKm}',''),Null,Null,@cDigest,0,'{cexch_name}','{iExchRate}',0,{apCloseBill.iAmount},0,{apCloseBill.iAmount},0,0,Null,'{cSSCode}','48','AR48{cVouchID}',Null,'{bPrePay}','{iFlag}','48','{cVouchID}','AR','{ZBIDs}','{ZBIDs}',Null,Null,'{apCloseBill.billDate}',@CMAKER,@CMAKER,{apCloseBill.iAmount},{apCloseBill.iAmount},0,{apCloseBill.iAmount},{apCloseBill.iAmount},0,Null)",
                                new SugarParameter[]
                                {
                                    new SugarParameter("@cDigest", apCloseBill.cDigest),
                                    new SugarParameter("@CMAKER", apCloseBill.CMAKER)
                                });

                            #endregion

                            await _db.Ado.CommitTranAsync();

                            #endregion
                        }
                        else
                        {
                            #region 红字付款单

                            string cVenCode = "";
                            //供应商编码
                            if (string.IsNullOrWhiteSpace(apCloseBill.cDwCode))
                            {
                                throw new Exception("往来对应编码不允许为空");
                            }
                            else
                            {
                                //获取供应商编码
                                cVenCode = _db.Ado.GetString(
                                    $"select cVenCode from {u8DbName}..VenDor (nolock) where cVenCode=@cVenCode",
                                    new SugarParameter[]
                                    {
                                        new SugarParameter("@cVenCode", apCloseBill.cDwCode.Remove(0, u8prefix.Length))
                                    });
                                if (string.IsNullOrWhiteSpace(cVenCode))
                                {
                                    throw new Exception("往来对应编码在U8业务账套号[" + u8DbName + "]供应商档案中不存在");
                                }
                            }

                            //获取红字付款单号
                            string cSeed = DateTime.Now.ToString("yyMMdd").ToString();
                            string cSeedShow = "YFSK" + cSeed;
                            string cSeedCode = DateTime.Now.ToString("yyyyMM").ToString();
                            //付款单红字单据号
                            string cVouchID = getU8VouchCode(u8DbName, "PR", "单据日期", "月", cSeedCode, cSeedShow, 3);
                            message = "红字付款单号:" + cVouchID;
                            code = cVouchID;

                            await _db.Ado.BeginTranAsync();

                            //科目编码（根据结算方式获取应付结算科目）
                            string KmcCode = _db.Ado.GetString(
                                $"select cCode from {u8DbName}..Ap_SStyleCode (nolock) where iyear={DateTime.Parse(apCloseBill.billDate).Year} and cSettleStyle='{cSSCode}' and cFlag='AP'");

                            //来源票据号
                            if (!string.IsNullOrWhiteSpace(apCloseBill.cNoteCode))
                            {
                                //表头结算科目（应付票据科目）
                                if (apCloseBill.orgCode == "1003" || apCloseBill.orgCode == "1004")
                                {
                                    KmcCode = "2201";
                                }
                                else
                                {
                                    if (apCloseBill.cSSName == "银行承兑汇票")
                                    {
                                        KmcCode = "220101";
                                    }
                                    else if (apCloseBill.cSSName == "商业承兑汇票")
                                    {
                                        KmcCode = "220103";
                                    }
                                    else if (apCloseBill.cSSName == "债权凭证")
                                    {
                                        KmcCode = "220104";
                                    }
                                    else
                                    {
                                        KmcCode = "220102";
                                    }
                                }
                            }


                            //判断主表结算科目
                            if (!string.IsNullOrWhiteSpace(KmcCode))
                            {
                                //判断借方科目是否存在
                                string ccode = _db.Ado.GetString(
                                    $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{KmcCode}' and bend=1");

                                if (string.IsNullOrWhiteSpace(ccode))
                                {
                                    throw new Exception("科目编码[" + KmcCode + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                                }
                            }

                            //汇率
                            string iExchRate = "1";
                            //币种
                            string cexch_name = "人民币";

                            #region 红字付款单主表

                            //红字付款单主表
                            _db.Ado.ExecuteCommand(
                                $"Insert Into {u8DbName}..Ap_CloseBill (cVouchType,cVouchID,dVouchDate,iPeriod,cDwCode,cDeptCode,cPerson,cItem_Class,cSSCode,cNoteNo,cCoVouchType,cCoVouchID,cDigest,cexch_name,iExchRate,iAmount,iAmount_f,iRAmount,iRAmount_f,cOperator,cCancelMan,bStartFlag,cCode,iPayForOther,cPzID,cFlag,iID,cCancelNo,bFromBank,bToBank,bSure,VT_ID,cCheckMan,cDefine1,iAmount_s,IsWfControlled,iSource,iverifystate,dcreatesystime,dverifysystime,dverifydate,cPZNum,doutbilldate,iPayType,csysbarcode,cBank,cBankAccount,cNatBank,cNatBankAccount,cDefine13,cDefine14,cDefine12) select N'48',N'{cVouchID}','{apCloseBill.billDate}','{DateTime.Parse(apCloseBill.billDate).Month}',N'{cVenCode}','{cDepCode}',nullif(N'{cPersonCode}',''),null,N'{cSSCode}',null,null,null,@cDigest,N'{cexch_name}',{iExchRate},{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},@CMAKER,null,0,nullif(N'{KmcCode}',''),0,null,N'AP','{ZBID}',null,0,0,0,8051,@CMAKER,@ID,0,null,null,null,GETDATE(),GETDATE(),'{apCloseBill.billDate}',null,null,0,N'||ap48|{cVouchID}',cVenBank,cVenAccount,@cNatBank,@cNatBankAccount,@cNoteCode,@bizbillno,@cVouchCode from {u8DbName}..Vendor (nolock) where cVenCode='{cVenCode}'",
                                new SugarParameter[]
                                {
                                    new SugarParameter("@cDigest", apCloseBill.cDigest),
                                    new SugarParameter("@CMAKER", apCloseBill.CMAKER),
                                    new SugarParameter("@ID", apCloseBill.ID),
                                    new SugarParameter("@cNatBank", apCloseBill.cNatBank),
                                    new SugarParameter("@cNatBankAccount", apCloseBill.cNatBankAccount),
                                    new SugarParameter("@cNoteCode", apCloseBill.cNoteCode),
                                    new SugarParameter("@bizbillno", apCloseBill.bizbillno),
                                    new SugarParameter("@cVouchCode", apCloseBill.cVouchCode)
                                });

                            //是否存在扩展自定义项表
                            string sqlext = _db.Ado.GetString(
                                $"IF EXISTS(SELECT name FROM {u8DbName}..[sysobjects] WHERE name = 'Ap_CloseBill_extradefine') select '1' ELSE select '0'");
                            if (sqlext == "1")
                            {
                                _db.Ado.ExecuteCommand(
                                    $"insert into {u8DbName}..Ap_CloseBill_extradefine(iID) values('{ZBID}')");
                            }

                            //主表明细账
                            _db.Ado.ExecuteCommand(
                                $"Insert Into  {u8DbName}..AP_Detail(iPeriod,cVouchType,cVouchID,dVouchDate,dRegDate,cDwCode,cDeptCode,cPerson,iBVid,cCode,iSignSeq,ino_id,cDigest,iPrice,cExch_name,iExchRate,iDAmount,iCAmount,iDAmount_f,iCAmount_f,iDAmount_s,iCAmount_s,cOrderNo,cSSCode,cProcStyle,cCancelNo,cPZid,bPrePay,iFlag,cCoVouchType,cCoVouchID,cFlag,iClosesID,iCoClosesID,cGLSign,iGLno_id,dPZDate,cOperator,cCheckMan,iAmount,iAmount_f,iAmount_s,iVouchAmount,iVouchAmount_f,iVouchAmount_s,cBusType) values('{DateTime.Parse(apCloseBill.billDate).Month}','48','{cVouchID}','{apCloseBill.billDate}','{apCloseBill.billDate}','{cVenCode}','{cDepCode}',nullif(N'{cPersonCode}',''),0,nullif(N'{KmcCode}',''),Null,Null,@cDigest,0,'{cexch_name}','{iExchRate}',0,{apCloseBill.iAmount * -1},0,{apCloseBill.iAmount * -1},0,0,Null,'{cSSCode}','48','AP48{cVouchID}',Null,0,6,'48','{cVouchID}','AP',0,0,Null,Null,'{apCloseBill.billDate}',@CMAKER,@CMAKER,Null,Null,Null,Null,Null,Null,Null)",
                                new SugarParameter[]
                                {
                                    new SugarParameter("@cDigest", apCloseBill.cDigest),
                                    new SugarParameter("@CMAKER", apCloseBill.CMAKER)
                                });

                            #endregion

                            #region 红字付款单子表

                            string iType = "0";
                            string bPrePay = "0";
                            string iFlag = "0";
                            //子表科目编码
                            string cKm = "";
                            if (apCloseBill.quickTypeName == "预付款")
                            {
                                bPrePay = "1";
                                iType = "1";
                            }
                            else if (apCloseBill.quickTypeName == "费用")
                            {
                                iFlag = "10";
                                iType = "2";
                            }

                            if (iType == "0")
                            {
                                //获取应付款基本科目
                                cKm = _db.Ado.GetString(
                                    $"select cApCode from {u8DbName}..Ap_InputCode (nolock) where iyear={DateTime.Parse(apCloseBill.billDate).Year} and cFlag='P' and cNote_f='kzkm'");
                            }
                            else if (iType == "1")
                            {
                                //获取预付款基本科目
                                cKm = _db.Ado.GetString(
                                    $"select cApCode from {u8DbName}..Ap_InputCode (nolock) where iyear={DateTime.Parse(apCloseBill.billDate).Year} and cFlag='P' and cNote_f='prekm'");
                            }

                            //判断子表科目
                            if (!string.IsNullOrWhiteSpace(cKm))
                            {
                                //判断贷方科目是否存在
                                string ccode = _db.Ado.GetString(
                                    $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{cKm}' and bend=1");

                                if (string.IsNullOrWhiteSpace(ccode))
                                {
                                    throw new Exception("科目编码[" + cKm + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                                }
                            }

                            //红字付款单子表
                            _db.Ado.ExecuteCommand(
                                $"insert into {u8DbName}..Ap_CloseBills(iID,ID,iType,bPrePay,cCusVen,iAmt_f,iAmt,iRAmt_f,iRAmt,cKm,cXmClass,cDepCode,cPersonCode,iAmt_s,iRAmt_s,iOrderType,ccItemCode,RegisterFlag,iSrcClosesID,ifaresettled_f) values('{ZBID}','{ZBIDs}','{iType}','{bPrePay}','{cVenCode}',{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},nullif(N'{cKm}',''),Null,'{cDepCode}',nullif(N'{cPersonCode}',''),0,0,Null,Null,0,0,0)");

                            //是否存在扩展自定义项表
                            sqlext = _db.Ado.GetString(
                                $"IF EXISTS(SELECT name FROM {u8DbName}..[sysobjects] WHERE name = 'Ap_CloseBills_extradefine') select '1' ELSE select '0'");
                            if (sqlext == "1")
                            {
                                _db.Ado.ExecuteCommand(
                                    $"insert into {u8DbName}..Ap_CloseBills_extradefine(ID) values('{ZBIDs}')");
                            }

                            //子表明细账
                            _db.Ado.ExecuteCommand(
                                $"Insert Into  {u8DbName}..AP_Detail(iPeriod,cVouchType,cVouchID,dVouchDate,dRegDate,cDwCode,cDeptCode,cPerson,iBVid,cCode,iSignSeq,ino_id,cDigest,iPrice,cExch_name,iExchRate,iDAmount,iCAmount,iDAmount_f,iCAmount_f,iDAmount_s,iCAmount_s,cOrderNo,cSSCode,cProcStyle,cCancelNo,cPZid,bPrePay,iFlag,cCoVouchType,cCoVouchID,cFlag,iClosesID,iCoClosesID,cGLSign,iGLno_id,dPZDate,cOperator,cCheckMan,iAmount,iAmount_f,iAmount_s,iVouchAmount,iVouchAmount_f,iVouchAmount_s,cBusType) values('{DateTime.Parse(apCloseBill.billDate).Month}','48','{cVouchID}','{apCloseBill.billDate}','{apCloseBill.billDate}','{cVenCode}','{cDepCode}',nullif(N'{cPersonCode}',''),0,nullif(N'{cKm}',''),Null,Null,@cDigest,0,'{cexch_name}','{iExchRate}',{apCloseBill.iAmount * -1},0,{apCloseBill.iAmount * -1},0,0,0,Null,'{cSSCode}','48','AP48{cVouchID}',Null,{bPrePay},{iFlag},'48','{cVouchID}','AP','{ZBIDs}','{ZBIDs}',Null,Null,'{apCloseBill.billDate}',@CMAKER,@CMAKER,{apCloseBill.iAmount * -1},{apCloseBill.iAmount * -1},0,{apCloseBill.iAmount * -1},{apCloseBill.iAmount * -1},0,Null)",
                                new SugarParameter[]
                                {
                                    new SugarParameter("@cDigest", apCloseBill.cDigest),
                                    new SugarParameter("@CMAKER", apCloseBill.CMAKER)
                                });


                            await _db.Ado.CommitTranAsync();

                            #endregion

                            #endregion
                        }

                        #endregion
                    }
                    else
                    {
                        #region 资金付款业务

                        if (apCloseBill.cDwType == "客户")
                        {
                            #region 红字收款单

                            string cCusCode = "";
                            //客户编码
                            if (string.IsNullOrWhiteSpace(apCloseBill.cDwCode))
                            {
                                throw new Exception("往来对应编码不允许为空");
                            }
                            else
                            {
                                //获取客户编码
                                cCusCode = _db.Ado.GetString(
                                    $"select cCusCode from {u8DbName}..Customer (nolock) where cCusCode=@cCusCode",
                                    new SugarParameter[]
                                    {
                                        new SugarParameter("@cCusCode", apCloseBill.cDwCode.Remove(0, u8prefix.Length))
                                    });
                                if (string.IsNullOrWhiteSpace(cCusCode))
                                {
                                    throw new Exception("往来对应编码在U8业务账套号[" + u8AccId + "]客户档案中不存在");
                                }
                            }


                            //获取红字收款单号
                            string cSeed = DateTime.Now.ToString("yyMMdd").ToString();
                            string cSeedShow = "fk" + cSeed;
                            string cSeedCode = DateTime.Now.ToString("yyyyMM").ToString();
                            //收款单蓝字单据号
                            string cVouchID = getU8VouchCode(u8DbName, "RP", "单据日期", "月", cSeedCode, cSeedShow, 3);

                            message = "红字收款单号:" + cVouchID;
                            code = cVouchID;

                            await _db.Ado.BeginTranAsync();

                            //科目编码（根据结算方式获取应收结算科目）
                            string KmcCode = _db.Ado.GetString(
                                $"select cCode from {u8DbName}..Ap_SStyleCode (nolock) where iyear={DateTime.Parse(apCloseBill.billDate).Year} and cSettleStyle='{cSSCode}' and cFlag='AR'");

                            //来源票据号
                            if (!string.IsNullOrWhiteSpace(apCloseBill.cNoteCode))
                            {
                                //表头结算科目（应付票据科目）
                                if (apCloseBill.orgCode == "1003" || apCloseBill.orgCode == "1004")
                                {
                                    KmcCode = "1121";
                                }
                                else
                                {
                                    if (apCloseBill.cSSName == "银行承兑汇票")
                                    {
                                        KmcCode = "112101";
                                    }
                                    else if (apCloseBill.cSSName == "商业承兑汇票")
                                    {
                                        KmcCode = "112103";
                                    }
                                    else if (apCloseBill.cSSName == "债权凭证")
                                    {
                                        KmcCode = "112102";
                                    }
                                    else
                                    {
                                        KmcCode = "112104";
                                    }
                                }
                            }

                            //判断主表结算科目
                            if (!string.IsNullOrWhiteSpace(KmcCode))
                            {
                                //判断借方科目是否存在
                                string ccode = _db.Ado.GetString(
                                    $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{KmcCode}' and bend=1");

                                if (string.IsNullOrWhiteSpace(ccode))
                                {
                                    throw new Exception("科目编码[" + KmcCode + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                                }
                            }

                            //汇率
                            string iExchRate = "1";
                            //币种
                            string cexch_name = "人民币";

                            #region 红字收款单主表

                            //红字收款单主表
                            _db.Ado.ExecuteCommand(
                                $"Insert Into {u8DbName}..Ap_CloseBill (cVouchType,cVouchID,dVouchDate,iPeriod,cDwCode,cDeptCode,cPerson,cItem_Class,cSSCode,cNoteNo,cCoVouchType,cCoVouchID,cDigest,cexch_name,iExchRate,iAmount,iAmount_f,iRAmount,iRAmount_f,cOperator,cCancelMan,bStartFlag,cCode,iPayForOther,cPzID,cFlag,iID,cCancelNo,bFromBank,bToBank,bSure,VT_ID,cCheckMan,cDefine1,iAmount_s,IsWfControlled,iSource,iverifystate,dcreatesystime,dverifysystime,dverifydate,cPZNum,doutbilldate,iPayType,csysbarcode,cBank,cBankAccount,cNatBank,cNatBankAccount,cDefine13,cDefine14,cDefine12) select N'49',N'{cVouchID}','{apCloseBill.billDate}','{DateTime.Parse(apCloseBill.billDate).Month}',N'{cCusCode}','{cDepCode}',nullif(N'{cPersonCode}',''),null,N'{cSSCode}',null,null,null,@cDigest,N'{cexch_name}',{iExchRate},{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},@CMAKER,null,0,nullif(N'{KmcCode}',''),0,null,N'AR','{ZBID}',null,0,0,0,8055,@CMAKER,@ID,0,null,null,null,GETDATE(),GETDATE(),'{apCloseBill.billDate}',null,null,0,N'||ar49|{cVouchID}',cCusBank,cCusAccount,@cNatBank,@cNatBankAccount,@cNoteCode,@bizbillno,@cVouchCode from {u8DbName}..Customer (nolock) where cCusCode='{cCusCode}'",
                                new SugarParameter[]
                                {
                                    new SugarParameter("@cDigest", apCloseBill.cDigest),
                                    new SugarParameter("@CMAKER", apCloseBill.CMAKER),
                                    new SugarParameter("@ID", apCloseBill.ID),
                                    new SugarParameter("@cNatBank", apCloseBill.cNatBank),
                                    new SugarParameter("@cNatBankAccount", apCloseBill.cNatBankAccount),
                                    new SugarParameter("@cNoteCode", apCloseBill.cNoteCode),
                                    new SugarParameter("@bizbillno", apCloseBill.bizbillno),
                                    new SugarParameter("@cVouchCode", apCloseBill.cVouchCode)
                                });

                            //是否存在扩展自定义项表
                            string sqlext = _db.Ado.GetString(
                                $"IF EXISTS(SELECT name FROM {u8DbName}..[sysobjects] WHERE name = 'Ap_CloseBill_extradefine') select '1' ELSE select '0'");
                            if (sqlext == "1")
                            {
                                _db.Ado.ExecuteCommand(
                                    $"insert into {u8DbName}..Ap_CloseBill_extradefine(iID) values('{ZBID}')");
                            }

                            //红字主表明细账
                            _db.Ado.ExecuteCommand(
                                $"Insert Into  {u8DbName}..Ar_Detail(iPeriod,cVouchType,cVouchID,dVouchDate,dRegDate,cDwCode,cDeptCode,cPerson,iBVid,cCode,iSignSeq,ino_id,cDigest,iPrice,cExch_name,iExchRate,iDAmount,iCAmount,iDAmount_f,iCAmount_f,iDAmount_s,iCAmount_s,cOrderNo,cSSCode,cProcStyle,cCancelNo,cPZid,bPrePay,iFlag,cCoVouchType,cCoVouchID,cFlag,iClosesID,iCoClosesID,cGLSign,iGLno_id,dPZDate,cOperator,cCheckMan,iAmount,iAmount_f,iAmount_s,iVouchAmount,iVouchAmount_f,iVouchAmount_s,cBusType) values('{DateTime.Parse(apCloseBill.billDate).Month}','49','{cVouchID}','{apCloseBill.billDate}','{apCloseBill.billDate}','{cCusCode}','{cDepCode}',nullif(N'{cPersonCode}',''),0,nullif(N'{KmcCode}',''),Null,Null,@cDigest,0,'{cexch_name}','{iExchRate}',{apCloseBill.iAmount * -1},0,{apCloseBill.iAmount * -1},0,0,0,Null,'{cSSCode}','49','AR49{cVouchID}',Null,0,6,'49','{cVouchID}','AR',0,0,Null,Null,'{apCloseBill.billDate}',@CMAKER,@CMAKER,Null,Null,Null,Null,Null,Null,Null)",
                                new SugarParameter[]
                                {
                                    new SugarParameter("@cDigest", apCloseBill.cDigest),
                                    new SugarParameter("@CMAKER", apCloseBill.CMAKER)
                                });

                            #endregion

                            #region 红字收款单子表

                            string bPrePay = "0";
                            string iFlag = "0";
                            string iType = "0";
                            //子表科目编码
                            string cKm = "";
                            if (apCloseBill.quickTypeName == "预收款")
                            {
                                bPrePay = "1";
                                iType = "1";
                            }
                            else if (apCloseBill.quickTypeName == "费用")
                            {
                                iFlag = "10";
                                iType = "2";
                            }

                            if (iType == "0")
                            {
                                //获取应收款基本科目
                                cKm = _db.Ado.GetString(
                                    $"select cArCode from {u8DbName}..Ap_InputCode (nolock) where iyear={DateTime.Parse(apCloseBill.billDate).Year} and cFlag='R' and cNote_f='kzkm'");
                            }
                            else if (iType == "1")
                            {
                                //获取预收款基本科目
                                cKm = _db.Ado.GetString(
                                    $"select cArCode from {u8DbName}..Ap_InputCode (nolock) where iyear={DateTime.Parse(apCloseBill.billDate).Year} and cFlag='R' and cNote_f='prekm'");
                            }

                            //判断子表科目
                            if (!string.IsNullOrWhiteSpace(cKm))
                            {
                                //判断贷方科目是否存在
                                string ccode = _db.Ado.GetString(
                                    $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{cKm}' and bend=1");

                                if (string.IsNullOrWhiteSpace(ccode))
                                {
                                    throw new Exception("科目编码[" + cKm + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                                }
                            }

                            //红字收款单子表
                            _db.Ado.ExecuteCommand(
                                $"insert into {u8DbName}..Ap_CloseBills(iID,ID,iType,bPrePay,cCusVen,iAmt_f,iAmt,iRAmt_f,iRAmt,cKm,cXmClass,cDepCode,cPersonCode,iAmt_s,iRAmt_s,iOrderType,ccItemCode,RegisterFlag,iSrcClosesID,ifaresettled_f) values('{ZBID}','{ZBIDs}','{iType}','{bPrePay}','{cCusCode}',{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},nullif(N'{cKm}',''),Null,'{cDepCode}',nullif(N'{cPersonCode}',''),0,0,Null,Null,0,0,0)");

                            //是否存在扩展自定义项表
                            sqlext = _db.Ado.GetString(
                                $"IF EXISTS(SELECT name FROM {u8DbName}..[sysobjects] WHERE name = 'Ap_CloseBills_extradefine') select '1' ELSE select '0'");
                            if (sqlext == "1")
                            {
                                _db.Ado.ExecuteCommand(
                                    $"insert into {u8DbName}..Ap_CloseBills_extradefine(ID) values('{ZBIDs}')");
                            }

                            //红字子表明细账
                            _db.Ado.ExecuteCommand(
                                $"Insert Into  {u8DbName}..Ar_Detail(iPeriod,cVouchType,cVouchID,dVouchDate,dRegDate,cDwCode,cDeptCode,cPerson,iBVid,cCode,iSignSeq,ino_id,cDigest,iPrice,cExch_name,iExchRate,iDAmount,iCAmount,iDAmount_f,iCAmount_f,iDAmount_s,iCAmount_s,cOrderNo,cSSCode,cProcStyle,cCancelNo,cPZid,bPrePay,iFlag,cCoVouchType,cCoVouchID,cFlag,iClosesID,iCoClosesID,cGLSign,iGLno_id,dPZDate,cOperator,cCheckMan,iAmount,iAmount_f,iAmount_s,iVouchAmount,iVouchAmount_f,iVouchAmount_s,cBusType) values('{DateTime.Parse(apCloseBill.billDate).Month}','49','{cVouchID}','{apCloseBill.billDate}','{apCloseBill.billDate}','{cCusCode}','{cDepCode}',nullif(N'{cPersonCode}',''),0,nullif(N'{cKm}',''),Null,Null,@cDigest,0,'{cexch_name}','{iExchRate}',0,{apCloseBill.iAmount * -1},0,{apCloseBill.iAmount * -1},0,0,Null,'{cSSCode}','49','AR49{cVouchID}',Null,'{bPrePay}','{iFlag}','49','{cVouchID}','AR','{ZBIDs}','{ZBIDs}',Null,Null,'{apCloseBill.billDate}',@CMAKER,@CMAKER,{apCloseBill.iAmount * -1},{apCloseBill.iAmount * -1},0,{apCloseBill.iAmount * -1},{apCloseBill.iAmount * -1},0,Null)",
                                new SugarParameter[]
                                {
                                    new SugarParameter("@cDigest", apCloseBill.cDigest),
                                    new SugarParameter("@CMAKER", apCloseBill.CMAKER)
                                });

                            #endregion

                            await _db.Ado.CommitTranAsync();

                            #endregion
                        }
                        else
                        {
                            #region 蓝字付款单

                            string cVenCode = "";
                            //供应商编码
                            if (string.IsNullOrWhiteSpace(apCloseBill.cDwCode))
                            {
                                throw new Exception("往来对应编码不允许为空");
                            }
                            else
                            {
                                //获取供应商编码
                                cVenCode = _db.Ado.GetString(
                                    $"select cVenCode from {u8DbName}..VenDor (nolock) where cVenCode=@cVenCode",
                                    new SugarParameter[]
                                    {
                                        new SugarParameter("@cVenCode", apCloseBill.cDwCode.Remove(0, u8prefix.Length))
                                    });
                                if (string.IsNullOrWhiteSpace(cVenCode))
                                {
                                    throw new Exception("往来对应编码在U8业务账套号[" + u8AccId + "]供应商档案中不存在");
                                }
                            }

                            //获取蓝字付款单号
                            string cSeed = DateTime.Now.ToString("yyMMdd").ToString();
                            string cSeedShow = "FK" + cSeed;
                            string cSeedCode = DateTime.Now.ToString("yyyyMM").ToString();
                            //付款单蓝字单据号
                            string cVouchID = getU8VouchCode(u8DbName, "PP", "单据日期", "月", cSeedCode, cSeedShow, 3);
                            message = "蓝字付款单号:" + cVouchID;
                            code = cVouchID;

                            await _db.Ado.BeginTranAsync();

                            //科目编码（根据结算方式获取应收结算科目）
                            string KmcCode = _db.Ado.GetString(
                                $"select cCode from {u8DbName}..Ap_SStyleCode (nolock) where iyear={DateTime.Parse(apCloseBill.billDate).Year} and cSettleStyle='{cSSCode}' and cFlag='AP'");

                            //背书票据客户编码
                            string PayerCode = "";
                            //业务流水号
                            string coutno_id = "";
                            //凭证号
                            string ino_id = "";
                            //凭证类别
                            string isignseq = "";
                            string csign = "";
                            string cPZNum = "";
                            //凭证日期
                            string doutbilldate = "";
                            //凭证期间
                            string iSignSeqmonth = "";
                            string iSignino_id = "";

                            //是否生成凭证标识
                            bool boolisPZ = false;

                            //单据来源票据
                            if (!string.IsNullOrWhiteSpace(apCloseBill.cNoteCode))
                            {
                                //表头结算科目（应付票据科目）
                                if (apCloseBill.orgCode == "1003" || apCloseBill.orgCode == "1004")
                                {
                                    KmcCode = "2201";
                                }
                                else
                                {
                                    if (apCloseBill.cSSName == "银行承兑汇票")
                                    {
                                        KmcCode = "220101";
                                    }
                                    else if (apCloseBill.cSSName == "商业承兑汇票")
                                    {
                                        KmcCode = "220103";
                                    }
                                    else if (apCloseBill.cSSName == "债权凭证")
                                    {
                                        KmcCode = "220104";
                                    }
                                    else
                                    {
                                        KmcCode = "220102";
                                    }
                                }

                                //票证方向（1-收到票据（对应应收票据科目），2-开出票据（对应应付票据科目））
                                if (apCloseBill.receiptDirection == "1")
                                {
                                    //背书票据判定客户编码是否存在
                                    if (!string.IsNullOrWhiteSpace(apCloseBill.PayerCode))
                                    {
                                        //获取客户编码
                                        PayerCode = _db.Ado.GetString(
                                            $"select cCusCode from {u8DbName}..Customer (nolock) where cCusCode=@PayerCode",
                                            new SugarParameter[]
                                            {
                                        new SugarParameter("@PayerCode", apCloseBill.PayerCode.Remove(0, u8prefix.Length))
                                            });
                                        if (string.IsNullOrWhiteSpace(PayerCode))
                                        {
                                            throw new Exception("客户编码[" + PayerCode + "]在U8业务账套号[" + u8AccId + "]客户档案中不存在");
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception("背书票据结算单的客户编码未传入");
                                    }

                                    isignseq = "5";
                                    csign = "转账";

                                    if (apCloseBill.orgCode == "1004")
                                    {
                                        csign = "转";
                                    }

                                    //判断凭证类别是否存在
                                    string i_id =
                                        _db.Ado.GetString(
                                            $"select i_id from {u8DbName}..dsign (nolock) where csign='{csign}' and isignseq='{isignseq}'");
                                    if (string.IsNullOrWhiteSpace(i_id))
                                    {
                                        throw new Exception("凭证类别字[" + csign + "]在U8账套号[" + u8AccId + "]中不存在");
                                    }

                                    //获取凭证业务号
                                    coutno_id =
                                        _db.Ado.GetString(
                                            $"declare @p4 nvarchar(17) set @p4=N'AP0000000000001' exec {u8DbName}..Ap_Proc_CancelNo N'PZ',N'AP',default,@p4 output select @p4");

                                    //获取最大凭证号
                                    ino_id = _db.Ado.GetString(
                                        $"SELECT isnull(max(ino_id),0)+1  from {u8DbName}..gl_accvouch (nolock) where iperiod='{DateTime.Parse(apCloseBill.billDate).Month}' and isignseq='{isignseq}' and csign='{csign}' and iyear='{DateTime.Parse(apCloseBill.billDate).Year}'");

                                    cPZNum = csign + "-" + ino_id.PadLeft(4, '0');

                                    doutbilldate = apCloseBill.billDate;

                                    iSignSeqmonth = DateTime.Parse(apCloseBill.billDate).Month.ToString();

                                    iSignino_id = "1";

                                    boolisPZ = true;
                                }
                            }


                            //判断主表结算科目
                            if (!string.IsNullOrWhiteSpace(KmcCode))
                            {
                                //判断借方科目是否存在
                                string ccode = _db.Ado.GetString(
                                    $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{KmcCode}' and bend=1");

                                if (string.IsNullOrWhiteSpace(ccode))
                                {
                                    throw new Exception("科目编码[" + KmcCode + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                                }
                            }

                            //汇率
                            string iExchRate = "1";
                            //币种
                            string cexch_name = "人民币";

                            #region 蓝字付款单主表

                            //审核人
                            string cCheckMan = apCloseBill.CMAKER;

                            if (apCloseBill.cSSName == "农行重庆白市支行")
                            {
                                #region 银企联账户对应的付款单不审核
                                //付款单主表
                                _db.Ado.ExecuteCommand(
                                    $"Insert Into {u8DbName}..Ap_CloseBill (cVouchType,cVouchID,dVouchDate,iPeriod,cDwCode,cDeptCode,cPerson,cItem_Class,cSSCode,cNoteNo,cCoVouchType,cCoVouchID,cDigest,cexch_name,iExchRate,iAmount,iAmount_f,iRAmount,iRAmount_f,cOperator,cCancelMan,bStartFlag,cCode,iPayForOther,cPzID,cFlag,iID,cCancelNo,bFromBank,bToBank,bSure,VT_ID,cCheckMan,cDefine1,iAmount_s,IsWfControlled,iSource,iverifystate,dcreatesystime,dverifysystime,dverifydate,cPZNum,doutbilldate,iPayType,csysbarcode,cBank,cBankAccount,cNatBank,cNatBankAccount,cDefine13,cDefine14,cDefine12) select N'49',N'{cVouchID}','{apCloseBill.billDate}','{DateTime.Parse(apCloseBill.billDate).Month}',N'{cVenCode}','{cDepCode}',nullif(N'{cPersonCode}',''),null,N'{cSSCode}',null,null,null,@cDigest,N'{cexch_name}',{iExchRate},{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},@CMAKER,null,0,nullif(N'{KmcCode}',''),0,null,N'AP','{ZBID}',null,0,0,0,8053,null,@ID,0,null,null,null,GETDATE(),null,null,null,null,0,N'||ap49|{cVouchID}',cVenBank,cVenAccount,@cNatBank,@cNatBankAccount,@cNoteCode,@bizbillno,@cVouchCode from {u8DbName}..Vendor (nolock) where cVenCode='{cVenCode}'",
                                    new SugarParameter[]
                                    {
                                    new SugarParameter("@cDigest", apCloseBill.cDigest),
                                    new SugarParameter("@CMAKER", apCloseBill.CMAKER),
                                    new SugarParameter("@ID", apCloseBill.ID),
                                    new SugarParameter("@cNatBank", apCloseBill.cNatBank),
                                    new SugarParameter("@cNatBankAccount", apCloseBill.cNatBankAccount),
                                    new SugarParameter("@cNoteCode", apCloseBill.cNoteCode),
                                    new SugarParameter("@bizbillno", apCloseBill.bizbillno),
                                    new SugarParameter("@cVouchCode", apCloseBill.cVouchCode)
                                    });

                                //是否存在扩展自定义项表
                                string sqlextx = _db.Ado.GetString(
                                    $"IF EXISTS(SELECT name FROM {u8DbName}..[sysobjects] WHERE name = 'Ap_CloseBill_extradefine') select '1' ELSE select '0'");
                                if (sqlextx == "1")
                                {
                                    _db.Ado.ExecuteCommand(
                                        $"insert into {u8DbName}..Ap_CloseBill_extradefine(iID) values('{ZBID}')");
                                }

                                #endregion
                            }
                            else
                            {
                                //付款单主表
                                _db.Ado.ExecuteCommand(
                                    $"Insert Into {u8DbName}..Ap_CloseBill (cVouchType,cVouchID,dVouchDate,iPeriod,cDwCode,cDeptCode,cPerson,cItem_Class,cSSCode,cNoteNo,cCoVouchType,cCoVouchID,cDigest,cexch_name,iExchRate,iAmount,iAmount_f,iRAmount,iRAmount_f,cOperator,cCancelMan,bStartFlag,cCode,iPayForOther,cPzID,cFlag,iID,cCancelNo,bFromBank,bToBank,bSure,VT_ID,cCheckMan,cDefine1,iAmount_s,IsWfControlled,iSource,iverifystate,dcreatesystime,dverifysystime,dverifydate,cPZNum,doutbilldate,iPayType,csysbarcode,cBank,cBankAccount,cNatBank,cNatBankAccount,cDefine13,cDefine14,cDefine12) select N'49',N'{cVouchID}','{apCloseBill.billDate}','{DateTime.Parse(apCloseBill.billDate).Month}',N'{cVenCode}','{cDepCode}',nullif(N'{cPersonCode}',''),null,N'{cSSCode}',null,null,null,@cDigest,N'{cexch_name}',{iExchRate},{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},@CMAKER,null,0,nullif(N'{KmcCode}',''),0,nullif(N'{coutno_id}',''),N'AP','{ZBID}',null,0,0,0,8053,@CMAKER,@ID,0,null,null,null,GETDATE(),GETDATE(),'{apCloseBill.billDate}',nullif(N'{cPZNum}',''),nullif(N'{doutbilldate}',''),0,N'||ap49|{cVouchID}',cVenBank,cVenAccount,@cNatBank,@cNatBankAccount,@cNoteCode,@bizbillno,@cVouchCode from {u8DbName}..Vendor (nolock) where cVenCode='{cVenCode}'",
                                    new SugarParameter[]
                                    {
                                    new SugarParameter("@cDigest", apCloseBill.cDigest),
                                    new SugarParameter("@CMAKER", apCloseBill.CMAKER),
                                    new SugarParameter("@ID", apCloseBill.ID),
                                    new SugarParameter("@cNatBank", apCloseBill.cNatBank),
                                    new SugarParameter("@cNatBankAccount", apCloseBill.cNatBankAccount),
                                    new SugarParameter("@cNoteCode", apCloseBill.cNoteCode),
                                    new SugarParameter("@bizbillno", apCloseBill.bizbillno),
                                    new SugarParameter("@cVouchCode", apCloseBill.cVouchCode)
                                    });


                                //是否存在扩展自定义项表
                                string sqlextx = _db.Ado.GetString(
                                    $"IF EXISTS(SELECT name FROM {u8DbName}..[sysobjects] WHERE name = 'Ap_CloseBill_extradefine') select '1' ELSE select '0'");
                                if (sqlextx == "1")
                                {
                                    _db.Ado.ExecuteCommand(
                                        $"insert into {u8DbName}..Ap_CloseBill_extradefine(iID) values('{ZBID}')");
                                }

                                //主表明细账
                                _db.Ado.ExecuteCommand(
                                    $"Insert Into  {u8DbName}..AP_Detail(iPeriod,cVouchType,cVouchID,dVouchDate,dRegDate,cDwCode,cDeptCode,cPerson,iBVid,cCode,iSignSeq,ino_id,cDigest,iPrice,cExch_name,iExchRate,iDAmount,iCAmount,iDAmount_f,iCAmount_f,iDAmount_s,iCAmount_s,cOrderNo,cSSCode,cProcStyle,cCancelNo,cPZid,bPrePay,iFlag,cCoVouchType,cCoVouchID,cFlag,iClosesID,iCoClosesID,cGLSign,iGLno_id,dPZDate,cOperator,cCheckMan,iAmount,iAmount_f,iAmount_s,iVouchAmount,iVouchAmount_f,iVouchAmount_s,cBusType) values('{DateTime.Parse(apCloseBill.billDate).Month}','49','{cVouchID}','{apCloseBill.billDate}','{apCloseBill.billDate}','{cVenCode}','{cDepCode}',nullif(N'{cPersonCode}',''),0,nullif(N'{KmcCode}',''),nullif(N'{iSignSeqmonth}',''),Null,@cDigest,0,'{cexch_name}','{iExchRate}',0,{apCloseBill.iAmount},0,{apCloseBill.iAmount},0,0,Null,'{cSSCode}','49','AP49{cVouchID}',nullif(N'{coutno_id}',''),0,6,'49','{cVouchID}','AP',0,0,nullif(N'{csign}',''),nullif(N'{ino_id}',''),'{apCloseBill.billDate}',@CMAKER,@CMAKER,Null,Null,Null,Null,Null,Null,Null)",
                                    new SugarParameter[]
                                    {
                                    new SugarParameter("@cDigest", apCloseBill.cDigest),
                                    new SugarParameter("@CMAKER", apCloseBill.CMAKER)
                                    });

                            }

                            #endregion

                            #region 蓝字付款单子表

                            string iType = "0";
                            string bPrePay = "0";
                            string iFlag = "0";
                            //子表科目编码
                            string cKm = "";
                            if (apCloseBill.quickTypeName == "预付款")
                            {
                                bPrePay = "1";
                                iType = "1";
                            }
                            else if (apCloseBill.quickTypeName == "费用")
                            {
                                iFlag = "10";
                                iType = "2";
                            }

                            if (iType == "0")
                            {
                                //获取应付款基本科目
                                cKm = _db.Ado.GetString(
                                    $"select cApCode from {u8DbName}..Ap_InputCode (nolock) where iyear={DateTime.Parse(apCloseBill.billDate).Year} and cFlag='P' and cNote_f='kzkm'");
                            }
                            else if (iType == "1")
                            {
                                //获取预付款基本科目
                                cKm = _db.Ado.GetString(
                                    $"select cApCode from {u8DbName}..Ap_InputCode (nolock) where iyear={DateTime.Parse(apCloseBill.billDate).Year} and cFlag='P' and cNote_f='prekm'");
                            }

                            //判断子表科目
                            if (!string.IsNullOrWhiteSpace(cKm))
                            {
                                //判断贷方科目是否存在
                                string ccode = _db.Ado.GetString(
                                    $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{cKm}' and bend=1");

                                if (string.IsNullOrWhiteSpace(ccode))
                                {
                                    throw new Exception("科目编码[" + cKm + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                                }
                            }

                            //蓝字付款单子表
                            _db.Ado.ExecuteCommand(
                                $"insert into {u8DbName}..Ap_CloseBills(iID,ID,iType,bPrePay,cCusVen,iAmt_f,iAmt,iRAmt_f,iRAmt,cKm,cXmClass,cDepCode,cPersonCode,iAmt_s,iRAmt_s,iOrderType,ccItemCode,RegisterFlag,iSrcClosesID,ifaresettled_f) values('{ZBID}','{ZBIDs}','{iType}','{bPrePay}','{cVenCode}',{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},{apCloseBill.iAmount},nullif(N'{cKm}',''),Null,'{cDepCode}',nullif(N'{cPersonCode}',''),0,0,Null,Null,0,0,0)");

                            //是否存在扩展自定义项表
                            string sqlext = _db.Ado.GetString(
                                 $"IF EXISTS(SELECT name FROM {u8DbName}..[sysobjects] WHERE name = 'Ap_CloseBills_extradefine') select '1' ELSE select '0'");
                            if (sqlext == "1")
                            {
                                _db.Ado.ExecuteCommand(
                                    $"insert into {u8DbName}..Ap_CloseBills_extradefine(ID) values('{ZBIDs}')");
                            }

                            if (apCloseBill.cSSName != "农行重庆白市支行")
                            {
                                //子表明细账
                                _db.Ado.ExecuteCommand(
                                    $"Insert Into  {u8DbName}..AP_Detail(iPeriod,cVouchType,cVouchID,dVouchDate,dRegDate,cDwCode,cDeptCode,cPerson,iBVid,cCode,iSignSeq,ino_id,cDigest,iPrice,cExch_name,iExchRate,iDAmount,iCAmount,iDAmount_f,iCAmount_f,iDAmount_s,iCAmount_s,cOrderNo,cSSCode,cProcStyle,cCancelNo,cPZid,bPrePay,iFlag,cCoVouchType,cCoVouchID,cFlag,iClosesID,iCoClosesID,cGLSign,iGLno_id,dPZDate,cOperator,cCheckMan,iAmount,iAmount_f,iAmount_s,iVouchAmount,iVouchAmount_f,iVouchAmount_s,cBusType) values('{DateTime.Parse(apCloseBill.billDate).Month}','49','{cVouchID}','{apCloseBill.billDate}','{apCloseBill.billDate}','{cVenCode}','{cDepCode}',nullif(N'{cPersonCode}',''),0,nullif(N'{cKm}',''),nullif(N'{iSignSeqmonth}',''),nullif(N'{iSignino_id}',''),@cDigest,0,'{cexch_name}','{iExchRate}',{apCloseBill.iAmount},0,{apCloseBill.iAmount},0,0,0,Null,'{cSSCode}','49','AP49{cVouchID}',nullif(N'{coutno_id}',''),{bPrePay},{iFlag},'49','{cVouchID}','AP','{ZBIDs}','{ZBIDs}',nullif(N'{csign}',''),nullif(N'{ino_id}',''),'{apCloseBill.billDate}',@CMAKER,@CMAKER,{apCloseBill.iAmount},{apCloseBill.iAmount},0,{apCloseBill.iAmount},{apCloseBill.iAmount},0,Null)",
                                    new SugarParameter[]
                                    {
                                    new SugarParameter("@cDigest", apCloseBill.cDigest),
                                    new SugarParameter("@CMAKER", apCloseBill.CMAKER)
                                    });
                            }

                            #endregion


                            #region 票据背书凭证生成

                            //单据来源票据
                            if (boolisPZ)
                            {
                                //借方科目（应付账款or预付账款）
                                string mdCode = cKm;

                                //贷方科目（应收票据）
                                string mcCode = "";
                                //表头结算科目（应收票据科目）
                                if (apCloseBill.orgCode == "1003" || apCloseBill.orgCode == "1004")
                                {
                                    mcCode = "1121";
                                }
                                else
                                {
                                    if (apCloseBill.cSSName == "银行承兑汇票")
                                    {
                                        mcCode = "112101";
                                    }
                                    else if (apCloseBill.cSSName == "商业承兑汇票")
                                    {
                                        mcCode = "112103";
                                    }
                                    else if (apCloseBill.cSSName == "债权凭证")
                                    {
                                        mcCode = "112102";
                                    }
                                    else
                                    {
                                        mcCode = "112104";
                                    }
                                }

                                //判断借方科目是否存在
                                string ccode =
                                    _db.Ado.GetString(
                                        $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{mdCode}' and bend=1");

                                if (string.IsNullOrWhiteSpace(ccode))
                                {
                                    throw new Exception("科目编码[" + mdCode + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                                }

                                //判断贷方科目是否存在
                                ccode = _db.Ado.GetString(
                                    $"select ccode from {u8DbName}..code (nolock) where iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ccode='{mcCode}' and bend=1");

                                if (string.IsNullOrWhiteSpace(ccode))
                                {
                                    throw new Exception("科目编码[" + mcCode + "]在U8账套号[" + u8AccId + "]会计科目中不存在");
                                }

                                await _db.Ado.BeginTranAsync();

                                int inid = 1;

                                string RowGuid = Guid.NewGuid().ToString().Replace("-", "") + "00000000";

                                //摘要
                                string cdigest = apCloseBill.cDigest;

                                #region 付款单票据背书凭证生成

                                apCloseBill.CMAKER = "ys同步";

                                //凭证借方（应付账款or预付账款）
                                _db.Ado.ExecuteCommand(
                                    $"insert into {u8DbName}..GL_accvouch(iperiod,csign,isignseq,ino_id,inid,dbill_date,idoc,cbill,ibook,cdigest,ccode,md,mc,md_f,mc_f,nfrat,nd_s,nc_s,csettle,dt_date,cdept_id,cperson_id,ccus_id,csup_id,citem_id,citem_class,cname,ccode_equal,bdelete,coutaccset,ioutyear,coutsysname,doutbilldate,ioutperiod,coutsign,coutno_id,doutdate,coutbillsign,coutid,bvouchedit,bvouchAddordele,bvouchmoneyhold,bvalueedit,bcodeedit,ccodecontrol,bPCSedit,bDeptedit,bItemedit,bCusSupInput,cDefine12,cDefine13,cDefine14,bFlagOut,RowGuid,iyear,iYPeriod,tvouchtime,ccodeexch_equal) values('{DateTime.Parse(apCloseBill.billDate).Month}','{csign}','{isignseq}','{ino_id}','{inid}','{apCloseBill.billDate}','1','{apCloseBill.CMAKER}',0,'{cdigest}','{mdCode}','{apCloseBill.iAmount}',0,0,0,0,0,0,Null,Null,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),Null,'{cVenCode}',Null,Null,'-','{mcCode}',0,'{u8AccId}','{DateTime.Parse(apCloseBill.billDate).Year}','AP','{apCloseBill.billDate}','{DateTime.Parse(apCloseBill.billDate).Month}','RP','{coutno_id}','{apCloseBill.billDate}','49','{cVouchID}',1,1,0,0,1,'AP',0,1,1,0,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','票据背书',0,'{RowGuid}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),getdate(),'{mcCode}')");

                                inid = inid + 1;

                                RowGuid = Guid.NewGuid().ToString().Replace("-", "") + "00000000";

                                //贷方（应收票据科目）
                                _db.Ado.ExecuteCommand(
                                    $"insert into {u8DbName}..GL_accvouch(iperiod,csign,isignseq,ino_id,inid,dbill_date,idoc,cbill,ibook,cdigest,ccode,md,mc,md_f,mc_f,nfrat,nd_s,nc_s,csettle,dt_date,cdept_id,cperson_id,ccus_id,csup_id,citem_id,citem_class,cname,ccode_equal,bdelete,coutaccset,ioutyear,coutsysname,doutbilldate,ioutperiod,coutsign,coutno_id,doutdate,coutbillsign,coutid,bvouchedit,bvouchAddordele,bvouchmoneyhold,bvalueedit,bcodeedit,ccodecontrol,bPCSedit,bDeptedit,bItemedit,bCusSupInput,cDefine12,cDefine13,cDefine14,bFlagOut,RowGuid,iyear,iYPeriod,tvouchtime,ccodeexch_equal) values('{DateTime.Parse(apCloseBill.billDate).Month}','{csign}','{isignseq}','{ino_id}','{inid}','{apCloseBill.billDate}','1','{apCloseBill.CMAKER}',0,'{cdigest}','{mcCode}','0','{apCloseBill.iAmount}',0,0,0,0,0,Null,Null,nullif(N'{cDepCode}',''),nullif(N'{cPersonCode}',''),'{PayerCode}',Null,Null,Null,Null,'{mdCode}',0,'{u8AccId}','{DateTime.Parse(apCloseBill.billDate).Year}','AP','{apCloseBill.billDate}','{DateTime.Parse(apCloseBill.billDate).Month}','RP','{coutno_id}','{apCloseBill.billDate}','49','{cVouchID}',1,1,0,1,1,'! AR AP #',1,1,1,1,'{apCloseBill.cVouchCode}','{apCloseBill.cNoteCode}','票据背书',0,'{RowGuid}','{DateTime.Parse(apCloseBill.billDate).Year}',CONVERT(varchar(6),cast('{apCloseBill.billDate}' as datetime),112),getdate(),'{mdCode}')");

                                #endregion

                                //查询凭证是否重复生成
                                string counts = _db.Ado.GetString(
                                    $"select count(1) from {u8DbName}..gl_accvouch (nolock) where iperiod='{DateTime.Parse(apCloseBill.billDate).Month}' and isignseq='{isignseq}' and csign='{csign}' and iyear='{DateTime.Parse(apCloseBill.billDate).Year}' and ino_id='{ino_id}' and inid='1'");

                                if (int.Parse(counts) > 1)
                                {
                                    throw new Exception("U8业务账套号[" + u8AccId + "]凭证号获取重复，请重试");
                                }
                            }
                            #endregion 

                            await _db.Ado.CommitTranAsync();

                            #endregion
                        }

                        #endregion
                    }
                }

                return new resultDto
                {
                    success = true,
                    msg = "操作成功！" + message,
                    code = code
                };
            }
            else
            {
                throw new Exception("请传入有效的数据");
            }

            #endregion
        }
        catch (Exception ex)
        {
            await _db.Ado.RollbackTranAsync();
            return new resultDto
            {
                success = false,
                msg = "操作失败: " + ex.Message,
                code = ""
            };
        }
    }


    /// <summary>
    /// 获取U8单据Id
    /// </summary>
    /// <param name="cAccId">账套号</param>
    /// <param name="cVouchType">单据类型编码</param>
    /// <param name="iAmount">行数</param>
    /// <param name="RemoteId">远程号(默认00)</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public DataTable getU8VouchId(string orgDbName, string cAccId, string cVouchType, int iAmount,
        string RemoteId = "00")
    {
        try
        {
            string sql =
                $"declare @p5 int set @p5=1000000002 declare @p6 int set @p6=1000000004 exec {orgDbName}..sp_getID N'{RemoteId}',N'{cAccId}',N'{cVouchType}',{iAmount},@p5 output,@p6 output select @p5, @p6";
            var data = _db.Ado.GetDataTable(sql, new SugarParameter[] { });
            return data;
        }
        catch (Exception ex)
        {
            throw new Exception("获取ID异常:" + ex.Message);
        }
    }

    /// <summary>
    /// 获取U8单据编号
    /// </summary>
    /// <param name="CardNumber">单据类型编码</param>
    /// <param name="cContent">流水号依据 </param>
    /// <param name="cContentRule">流水号规则 </param>
    /// <param name="cSeed">流水依据项</param>
    /// <param name="cSeedShow">单据显示项</param>
    /// <param name="flowNum">流水位数</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public string getU8VouchCode(string orgDbName, string CardNumber, string cContent, string cContentRule,
        string cSeed, string cSeedShow, int flowNum)
    {
        try
        {
            int flowNo = 1;
            string cCode = cSeedShow + flowNo.ToString().PadLeft(flowNum, '0');
            string where = "";
            if (!string.IsNullOrWhiteSpace(cSeed))
            {
                where += " and cSeed='" + cSeed + "'";
            }

            if (!string.IsNullOrWhiteSpace(cContent))
            {
                where += " and cContent='" + cContent + "'";
            }
            else
            {
                where += " and cContent is NULL";
            }

            string sql =
                $"select (isnull(cNumber,0)+1) as Maxnumber From {orgDbName}..VoucherHistory  with (NOLOCK) Where  CardNumber=@CardNumber {where} ";
            var dt = _db.Ado.GetDataTable(sql, new { CardNumber = CardNumber });
            if (dt.Rows.Count > 0)
            {
                _db.Ado.ExecuteCommand(
                    $"update {orgDbName}..VoucherHistory set cNumber=isnull(cNumber,0)+1 Where  CardNumber='{CardNumber}' {where} ");
                cCode = cSeedShow + dt.Rows[0]["Maxnumber"].ToString().PadLeft(flowNum, '0');
            }
            else
            {
                _db.Ado.ExecuteCommand(
                    $"Insert into {orgDbName}..VoucherHistory(CardNumber,cContent,cContentRule,cSeed,cNumber) values('{CardNumber}','{cContent}','{cContentRule}','{cSeed}','1')");
            }

            return cCode;
        }
        catch (Exception ex)
        {
            throw new Exception("获取单号异常:" + ex.Message);
        }
    }
}