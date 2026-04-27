namespace ZR.Model.Business.Model.Dto;

public class ApCloseBillDto
{
     /// <summary>
        /// 业务组织（账套号）
        /// </summary>
        public string orgCode { get; set; }

        /// <summary>
        /// 单据子表唯一id
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// 单据编码
        /// </summary>
        public string cVouchCode { get; set; }

        /// <summary>
        /// 款项类型
        /// </summary>
        public string quickTypeName { get; set; }//款项类型（应收款，预收款，预付款，应付款，其他费用）

        /// <summary>
        /// 单据类型
        /// </summary>
        public string cVouchType { get; set; }//单据类型（资金收款，资金付款）

        /// <summary>
        /// 往来对象类型
        /// </summary>
        public string cDwType { get; set; } //单位类型（客户，供应商）

        /// <summary>
        /// 往来对象单位编码
        /// </summary>
        public string cDwCode { get; set; }

        /// <summary>
        /// 单据日期
        /// </summary>
        public string billDate { get; set; }

        /// <summary>
        /// 本单位银行账号
        /// </summary>
        public string cNatBankAccount { get; set; }

        /// <summary>
        /// 本单位银行名称
        /// </summary>
        public string cNatBank { get; set; }

        /// <summary>
        /// 结算方式
        /// </summary>
        public string cSSName { get; set; }

        /// <summary>
        /// 部门编码
        /// </summary>
        public string cDepCode { get; set; }

        /// <summary>
        /// 人员编码
        /// </summary>
        public string cPersonCode { get; set; }

        /// <summary>
        /// 摘要
        /// </summary>
        public string cDigest { get; set; }
        /// <summary>
        /// 制单人
        /// </summary>
        public string CMAKER { get; set; }

        /// <summary>
        ///票据号
        /// </summary>
        public string cNoteCode { get; set; }

        /// <summary>
        /// 票证方向
        /// </summary>
        public string receiptDirection { get; set; }

        /// <summary>
        /// 来源交易类型
        /// </summary>
        public string tradetypeName { get; set; }//单据类型（贴现办理，到期兑付,银行托收）

        /// <summary>
        /// 金额
        /// </summary>
        public decimal iAmount { get; set; }
        
        
        public decimal discountInterest { get; set; }
        
        public string noteTypeCode { get; set; }
}