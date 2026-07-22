using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZR.Service.Business.U8.U8Dtos
{
    public class U8fundsDto
    {
        [JsonProperty("code")]
        public string code { get; set; }

        [JsonProperty("cDate")]
        public string cDate { get; set; }

        [JsonProperty("OLAmount")]
        public decimal OLAmount { get; set; }

        [JsonProperty("CLAmount")]
        public decimal CLAmount { get; set; }

        [JsonProperty("OAAmount")]
        public decimal OAAmount { get; set; }

        [JsonProperty("CAAmount")]
        public decimal CAAmount { get; set; }

        [JsonProperty("HOLAmount")]
        public decimal HOLAmount { get; set; }

        [JsonProperty("HCLAmount")]
        public decimal HCLAmount { get; set; }
    }
}
