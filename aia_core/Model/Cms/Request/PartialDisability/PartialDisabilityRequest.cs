using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request.PartialDisability
{
    public class PartialDisabilityRequest
    {
        public string Name { get; set; }
        public string Name_MM { get; set; }
        public string Code {get;set;}
        public bool IsActive { get; set; }
        public List<string>? ProductCodeList { get; set; }
    }

    public class CreatePartialDisabilityRequest: PartialDisabilityRequest
    {
        
    }

    public class UpdatePartialDisabilityRequest : PartialDisabilityRequest
    {
        public Guid ID { get; set; }
    }
}