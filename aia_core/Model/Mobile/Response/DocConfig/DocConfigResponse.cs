using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response.DocConfig
{
    public class AiaCmsDocResponse
    {
        public string DocTypeId { get; set; }
        public string Format { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedDate { get; set; }
        public string DocumentId { get; set; }
        public string DocTypeName { get; set; }
    }


    public class AiaCmsDocBase64Response
    {
        public string base64 { get; set; }
    }


    public class AiaCmsGetDocListResponse
    { 
        public int rawDocTotalCount { get; set; }
        public int rawDocPageCount { get; set; }
        public int rawDocCurrentPage { get; set; }
        public bool rawDocHasNextPage { get; set; }
        public int filterDocCount { get; set;}
        public List<AiaCmsDocResponse>? filterDocList { get; set; }
        
    }
}
