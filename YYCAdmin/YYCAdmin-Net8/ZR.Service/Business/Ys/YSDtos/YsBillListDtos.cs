using Newtonsoft.Json;

namespace ZR.Service.Business.Ys.Dtos
{
    /// <summary>
    /// YS 单据列表接口请求参数。
    /// </summary>
    public class YsBillListRequestDto
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
        public int PageSize { get; set; } = 100;

        /// <summary>
        /// 开始业务日期。
        /// </summary>
        [JsonProperty("open_vouchdate_begin")]
        public string OpenVouchdateBegin { get; set; } = string.Empty;

        /// <summary>
        /// 结束业务日期。
        /// </summary>
        [JsonProperty("open_vouchdate_end")]
        public string OpenVouchdateEnd { get; set; } = string.Empty;

        /// <summary>
        /// simpleVOs 方式的动态查询条件。
        /// </summary>
        [JsonProperty("simpleVOs", NullValueHandling = NullValueHandling.Ignore)]
        public List<YsSimpleQueryConditionDto> SimpleVOs { get; set; }
    }

    /// <summary>
    /// simpleVOs 查询条件项。
    /// </summary>
    public class YsSimpleQueryConditionDto
    {
        /// <summary>
        /// 查询字段名。
        /// </summary>
        [JsonProperty("field")]
        public string Field { get; set; }

        /// <summary>
        /// 查询操作符。
        /// </summary>
        [JsonProperty("op")]
        public string Op { get; set; }

        /// <summary>
        /// 查询值 1。
        /// </summary>
        [JsonProperty("value1")]
        public string Value1 { get; set; }
    }

    /// <summary>
    /// YS 单据列表接口分页数据。
    /// </summary>
    public class YsBillListDataDto
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
        /// 总页数。
        /// </summary>
        [JsonProperty("pageCount")]
        public int PageCount { get; set; }

        /// <summary>
        /// 当前页记录集合。
        /// </summary>
        [JsonProperty("recordList")]
        public List<YsBillListRecordDto> RecordList { get; set; }
    }

    /// <summary>
    /// YS 单据列表接口中的单条记录。
    /// </summary>
    public class YsBillListRecordDto
    {
        /// <summary>
        /// 单据主键 Id。
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
