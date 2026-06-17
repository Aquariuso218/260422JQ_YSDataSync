using Newtonsoft.Json;

namespace ZR.Service.Business.Ys.Dtos
{
    /// <summary>
    /// 组织档案返回对象。
    /// </summary>
    public class YsOrgInfoDto
    {
        /// <summary>
        /// 组织编码。
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }
    }

    /// <summary>
    /// 部门档案返回对象。
    /// </summary>
    public class YsDeptInfoDto
    {
        /// <summary>
        /// 部门编码。
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }
    }

    /// <summary>
    /// 人员档案返回对象。
    /// </summary>
    public class YsStaffInfoDto
    {
        /// <summary>
        /// 人员编码。
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }
    }

    /// <summary>
    /// YS 多语言名称对象。
    /// </summary>
    public class YsLocalizedNameDto
    {
        /// <summary>
        /// 简体中文名称。
        /// </summary>
        [JsonProperty("zh_CN")]
        public string ZhCn { get; set; }
    }

    /// <summary>
    /// 通用分页记录列表对象。
    /// </summary>
    public class YsPagedRecordListDto<T>
    {
        /// <summary>
        /// 当前页码。
        /// </summary>
        [JsonProperty("pageIndex")]
        public int PageIndex { get; set; }

        /// <summary>
        /// 每页条数。
        /// </summary>
        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        /// <summary>
        /// 总记录数。
        /// </summary>
        [JsonProperty("recordCount")]
        public int RecordCount { get; set; }

        /// <summary>
        /// 记录集合。
        /// </summary>
        [JsonProperty("recordList")]
        public List<T> RecordList { get; set; }
    }

    /// <summary>
    /// 银行档案批量查询请求对象。
    /// </summary>
    public class YsEnterpriseBankBatchRequestDto
    {
        /// <summary>
        /// 页码。
        /// </summary>
        [JsonProperty("pageIndex")]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 每页条数。
        /// </summary>
        [JsonProperty("pageSize")]
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// 待查询 Id 集合。
        /// </summary>
        [JsonProperty("ids")]
        public List<string> Ids { get; set; }
    }

    /// <summary>
    /// 企业银行账户档案记录。
    /// </summary>
    public class YsEnterpriseBankRecordDto
    {
        /// <summary>
        /// 银行账号。
        /// </summary>
        [JsonProperty("account")]
        public string Account { get; set; }

        /// <summary>
        /// 账户名称。
        /// </summary>
        [JsonProperty("acctName")]
        public YsLocalizedNameDto AcctName { get; set; }
    }

    /// <summary>
    /// 结算方式档案记录。
    /// </summary>
    public class YsSettleMethodRecordDto
    {
        /// <summary>
        /// 结算方式编码。
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }
    }

    /// <summary>
    /// 客户档案查询请求对象。
    /// </summary>
    public class YsMerchantLookupRequestDto
    {
        /// <summary>
        /// 客户 Id。
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// 每页条数。
        /// </summary>
        [JsonProperty("pageSize")]
        public int PageSize { get; set; } = 50;

        /// <summary>
        /// 页码。
        /// </summary>
        [JsonProperty("pageIndex")]
        public int PageIndex { get; set; } = 1;
    }

    /// <summary>
    /// 客户档案记录。
    /// </summary>
    public class YsMerchantRecordDto
    {
        /// <summary>
        /// 客户编码。
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }
    }

    /// <summary>
    /// 供应商档案详情对象。
    /// </summary>
    public class YsVendorDetailDto
    {
        /// <summary>
        /// 供应商编码。
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }
    }
}
