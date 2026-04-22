using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZR.Model.Business.Model
{
    [SugarTable("base_SimulatedData", "产品列表")]
    [Tenant("1")]
    public class SimulatedData
    {
        /// <summary>
        /// id
        /// </summary>
        /// 
        [SugarColumn(IsIdentity = true, IsPrimaryKey = true)]
        [JsonConverter(typeof(ValueToStringConverter))]
        public int id { get; set; }

        /// <summary>
        /// 流转卡
        /// </summary>
        public string flowCard { get; set; }

        /// <summary>
        /// 产品编码
        /// </summary>
        public string invCode { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int quantity { get; set; }
             

    }
}
