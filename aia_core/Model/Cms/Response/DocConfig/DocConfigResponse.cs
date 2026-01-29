using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Cms.Response.DocConfig
{
    public class DocConfigResponse
    {
        public Guid? Id { get; set; }
        public string? DocType { get; set; }
        public string? DocTypeId { get; set; }
        public string? DocName { get; set; }
        public string? ShowingFor { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }
}
