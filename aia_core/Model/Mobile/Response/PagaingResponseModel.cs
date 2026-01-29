using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response
{
    public class PagaingResponseModel<T>
    {
        public int TotalCount { get; set; }
        public int CountPerPage { get; set; }
        public int TotalPages { get { return (int)Math.Ceiling(TotalCount * 1.0 / CountPerPage); } }
        public int CurrentPage { get; set; }
        public bool hasPrevPage { get {
            if(TotalCount==0)
                return false;
            else 
                return CurrentPage != 1; 
        } }
        public bool hasNextPage { get { 
            if(TotalCount==0)
                return false;
            else
                return CurrentPage != TotalPages; 
        } }

        public int? PrevPage
        {
            get
            {
                if (hasPrevPage)
                    return CurrentPage - 1;
                else return null;
            }
        }
        public int? NextPage
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
