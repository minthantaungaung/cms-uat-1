using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Cms.Response.Common
{
    public class QueryStringsResponse
    {
        public string? CountQuery { get; set; }
        public string? ListQuery { get; set; }
    }

    public class CountResponse
    {
        public long SelectCount { get; set; }
    }
}
