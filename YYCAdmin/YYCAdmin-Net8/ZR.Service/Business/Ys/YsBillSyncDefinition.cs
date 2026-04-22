namespace ZR.Service.Business.Ys
{
    /// <summary>
    /// 定义一种 YS 单据同步类型的接口信息与行为差异。
    /// </summary>
    public sealed class YsBillSyncDefinition
    {
        /// <summary>
        /// 初始化单据同步定义。
        /// </summary>
        private YsBillSyncDefinition(
            string interfaceName,
            string listPath,
            string detailPath,
            string bodyPropertyName,
            string vouchType,
            bool useSimpleVosVerifyFilter)
        {
            InterfaceName = interfaceName;
            ListPath = listPath;
            DetailPath = detailPath;
            BodyPropertyName = bodyPropertyName;
            VouchType = vouchType;
            UseSimpleVosVerifyFilter = useSimpleVosVerifyFilter;
        }

        /// <summary>
        /// 同步日志中使用的接口标识名称。
        /// </summary>
        public string InterfaceName { get; }

        /// <summary>
        /// 列表接口相对路径。
        /// </summary>
        public string ListPath { get; }

        /// <summary>
        /// 详情接口相对路径。
        /// </summary>
        public string DetailPath { get; }

        /// <summary>
        /// 详情返回中对应的表体属性名称。
        /// </summary>
        public string BodyPropertyName { get; }

        /// <summary>
        /// 写入中间表的单据类型名称。
        /// </summary>
        public string VouchType { get; }

        /// <summary>
        /// 是否使用 simpleVOs 方式传递审核状态过滤条件。
        /// </summary>
        public bool UseSimpleVosVerifyFilter { get; }

        /// <summary>
        /// 当前支持的全部同步定义集合。
        /// </summary>
        public static readonly IReadOnlyList<YsBillSyncDefinition> All =
        [
            new(
                "YS_FundPayment",
                "/yonbip/fi/fundpayment/list",
                "/yonbip/fi/fundpayment/detail",
                "FundPayment_b",
                "\u8d44\u91d1\u4ed8\u6b3e\u5355",
                true),
            new(
                "YS_FundCollection",
                "/yonbip/fi/fundcollection/list",
                "/yonbip/fi/fundcollection/detail",
                "FundCollection_b",
                "\u8d44\u91d1\u6536\u6b3e\u5355",
                false)
        ];
    }
}
