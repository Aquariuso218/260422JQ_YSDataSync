using Infrastructure.Attribute;
using SqlSugar.IOC;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZR.Model.Business.Dto;
using ZR.Service.Business.IService;
using ZR.Service.Business.U8.Dtos;
using ZR.Service.Business.U8.U8Dtos;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace ZR.Service.Business.U8
{
    [AppService(ServiceType = typeof(IU8fundsService), ServiceLifetime = LifeTime.Transient)]
    public class U8fundsService : IU8fundsService
    {
        public ISqlSugarClient _db = DbScoped.SugarScope.GetConnectionScope("1");

        /// <summary>
        /// 获取U8资金期初数据
        /// </summary>
        /// <returns></returns>
        public async Task<resultDto> GetU8fundsYE()
        {
            try
            {
                //获取账套库名
                DataTable orgdt = _db.Ado.GetDataTable($"select * from [ZRAdmin]..OrgInfoSyn (nolock)");

                if (orgdt != null && orgdt.Rows.Count > 0)
                {
                    List<U8fundsDto> fundsDtos = new List<U8fundsDto>();

                    for (global::System.Int32 i = 0; i < orgdt.Rows.Count; i++)
                    {
                        //组织编码
                        string orgcode = orgdt.Rows[i]["orgcode"].ToString();

                        //U8数据库
                        string u8DbName = orgdt.Rows[i]["u8DbName"].ToString();

                        //当月第一天
                        DateTime firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

                        //获取最近2个月的期初数据
                        for (global::System.Int32 j = 0; j < 2; j++)
                        {
                            //日期
                            string cDate = firstDayOfMonth.AddMonths(j * -1).ToString("yyyy-MM-dd");

                            DataTable orgdtye = _db.Ado.GetDataTable($"exec " + u8DbName + "..proc_tmpuf_ysbankye_yyc '" + cDate + "'");

                            //锁定金额期初
                            decimal OLAmount = 0;
                            //锁定金额期末
                            decimal CLAmount = 0;
                            //可支配金额期初
                            decimal OAAmount = 0;
                            //可支配金额期末
                            decimal CAAmount = 0;
                            //开票锁定金额期初
                            decimal HOLAmount = 0;
                            //开票锁定金额期末
                            decimal HCLAmount = 0;

                            if (orgdtye != null && orgdtye.Rows.Count > 0)
                            {
                                //可支配金额期初
                                OAAmount = Convert.ToDecimal(orgdtye.Rows[0]["kzpqcmoney"]);
                                //可支配金额期末
                                CAAmount = Convert.ToDecimal(orgdtye.Rows[0]["kzpqmmoney"]);
                                //锁定金额期初
                                OLAmount = Convert.ToDecimal(orgdtye.Rows[0]["sdqcmoney"]);
                                //锁定金额期末
                                CLAmount = Convert.ToDecimal(orgdtye.Rows[0]["sdqmmoney"]);
                                //开票锁定金额期初
                                if (orgdtye.Columns.Contains("yhkpsdqcmoney"))
                                {
                                    HOLAmount = Convert.ToDecimal(orgdtye.Rows[0]["yhkpsdqcmoney"]);
                                }
                                //开票锁定金额期末
                                if (orgdtye.Columns.Contains("yhkpsdqmmoney"))
                                {
                                    HCLAmount = Convert.ToDecimal(orgdtye.Rows[0]["yhkpsdqmmoney"]);
                                }
                            }


                            U8fundsDto u8Funds = new U8fundsDto();

                            u8Funds.code = orgcode;
                            u8Funds.cDate = cDate;
                            u8Funds.OLAmount = OLAmount;
                            u8Funds.CLAmount = CLAmount;
                            u8Funds.OAAmount = OAAmount;
                            u8Funds.CAAmount = CAAmount;
                            u8Funds.HOLAmount = HOLAmount;
                            u8Funds.HCLAmount = HCLAmount;

                            fundsDtos.Add(u8Funds);

                        }

                    }

                    return new resultDto
                    {
                        success = true,
                        msg = "获取成功！",
                        u8Funds = fundsDtos
                    };
                }
                else
                {
                    throw new Exception("未获取到YS组织对应的数据库配置信息");
                }

            }
            catch (Exception ex)
            {
                return new resultDto
                {
                    success = false,
                    msg = "获取异常: " + ex.Message,
                };
            }
        }
    }
}
