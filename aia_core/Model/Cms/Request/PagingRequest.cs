using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Cms.Request
{
    public class PagingRequest
    {
        public int PageSize { get; set; } = 10;
        public int PageIndex { get; set; } = 1;
        public string? SortBy { get; set; }
        public bool? IsDesc { get; set; } = true;

        public int GetSkip()
        { 
            return (PageIndex - 1) * PageSize; 
        }
    }
}
