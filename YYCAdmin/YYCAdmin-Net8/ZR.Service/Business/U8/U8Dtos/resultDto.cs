using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZR.Service.Business.U8.U8Dtos;

namespace ZR.Service.Business.U8.Dtos
{
    public class resultDto
    {
        public Boolean success { get; set; }
        public string msg { get; set; }
        public string code { get; set; }
        public List<U8fundsDto> u8Funds { get; set; }
    }
}
