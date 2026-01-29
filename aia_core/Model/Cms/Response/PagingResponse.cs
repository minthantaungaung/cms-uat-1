using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Cms.Response
{
    public class PagingResponse<T>
    {
        public int TotalCount { get; set; }      
        public int TotalPages { get { return (int)Math.Ceiling(TotalCount * 1.0 / CountPerPage); } }
        public int CurrentPage { get; set; }
        public int CountPerPage { get; set; }
        internal bool hasPrevPage { get { return CurrentPage != 1; } }
        internal bool hasNextPage { get { return CurrentPage != TotalPages; } }

        internal int? PrevPage
        {
            get
            {
                if (hasPrevPage)
                    return CurrentPage - 1;
                else return null;
            }
        }
        internal int? NextPage
        {
            get
            {
                if (hasNextPage)
                    return CurrentPage + 1;
                else return null;

            }
        }

        public List<T> Data { get; set; }
    
    }
}
