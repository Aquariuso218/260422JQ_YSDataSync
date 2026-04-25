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
            string bodyPropertyName)
        {
            InterfaceName = interfaceName;
            ListPath = listPath;
            DetailPath = detailPath;
            BodyPropertyName = bodyPropertyName;
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
        /// 当前支持的全部同步定义集合。
        /// </summary>
        public static readonly IReadOnlyList<YsBillSyncDefinition> All =
        [
            new(
                "YS_SettleBench",
                "/yonbip/ctm/stwb/settlebench",
                "/yonbip/ctm/settleBench/detail",
                "settleBench_b")
        ];
    }
}
