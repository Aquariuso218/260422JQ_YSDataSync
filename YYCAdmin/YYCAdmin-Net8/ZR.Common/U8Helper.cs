using System;
using System.Data;
using SqlSugar;

namespace ZR.Common;

public class U8Helper
{
        public ISqlSugarClient _db { get; set; }
        
        public U8Helper(ISqlSugarClient db)
        {
            _db = db;
        }

        
}