using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Cms.Request.DocConfig
{
    public class DocConfigRequest
    { 
        [Required]
        public string? DocType { get; set; }

        [Required]
        public string? DocTypeId { get; set; }

        [Required]
        public string? DocName { get; set; }

        [Required]
        public EnumDocShowingFor? ShowingFor { get; set; }
    }

    public class DocConfigUpdateRequest : DocConfigRequest
    {
        [Required]
        public Guid? Id { get; set; }
    }
}
