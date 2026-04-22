namespace ZR.Service.Business.Ys
{
    /// <summary>
    /// YS 接口调用所需的配置项。
    /// </summary>
    internal class YsConfigOptions
    {
        /// <summary>
        /// 鉴权接口地址。
        /// </summary>
        public string AuthBaseUrl { get; set; }

        /// <summary>
        /// 业务网关基础地址。
        /// </summary>
        public string GatewayBaseUrl { get; set; }

        /// <summary>
        /// YS 应用标识。
        /// </summary>
        public string AppKey { get; set; }

        /// <summary>
        /// YS 应用密钥。
        /// </summary>
        public string AppSecret { get; set; }
    }
}
