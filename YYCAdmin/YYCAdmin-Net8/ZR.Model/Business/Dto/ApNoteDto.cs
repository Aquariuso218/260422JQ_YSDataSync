namespace ZR.Model.Business.Dto;

public class ApNoteDto
{
    /// <summary>
        /// 业务组织（账套号）
        /// </summary>
        public string orgCode { get; set; }

        /// <summary>
        ///票据编号
        /// </summary>
        public string cVouchID { get; set; }
        /// <summary>
        /// 收到日期
        /// </summary>
        public string dReceiptDate { get; set; }

        /// <summary>
        /// 出票日期
        /// </summary>
        public string dSignDate { get; set; }

        /// <summary>
        /// 到期日期 
        /// </summary>
        public string dExpireDate { get; set; }

        /// <summary>
        /// 部门编码
        /// </summary>
        public string cDepCode { get; set; }

        /// <summary>
        /// 人员编码
        /// </summary>
        public string cPersonCode { get; set; }

        /// <summary>
        /// 单据类型
        /// </summary>
        public string cVouchType { get; set; }//单据类型（应收票据，应付票据）

        /// <summary>
        /// 票据类型（银行承兑汇票，商业承兑汇票）
        /// </summary>
        public string cNoteType { get; set; } //票据类型（银行承兑汇票，商业承兑汇票）

        /// <summary>
        /// 往来对象类型
        /// </summary>
        public string cDwType { get; set; } //单位类型（客户，供应商）

        /// <summary>
        /// 本单位银行账号
        /// </summary>
        public string cNatBankAccount { get; set; }

        /// <summary>
        /// 本单位银行名称
        /// </summary>
        public string cNatBank { get; set; }

        /// <summary>
        /// 往来对象单位编码
        /// </summary>
        public string cDwCode { get; set; }

        /// <summary>
        /// 往来对象开户银行
        /// </summary>
        public string cDwBank { get; set; }

        /// <summary>
        /// 往来对象单位银行账号
        /// </summary>
        public string cDwBankAccount { get; set; }
        /// <summary>
        /// 摘要
        /// </summary>
        public string cDigest { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string cRemark { get; set; }
        /// <summary>
        /// 制单人
        /// </summary>
        public string CMAKER { get; set; }

        /// <summary>
        /// 金额
        /// </summary>
        public decimal iAmount { get; set; }
}