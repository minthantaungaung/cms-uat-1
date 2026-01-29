using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request.PermanentDisability
{
    public class PermanentDisabilityRequest
    {
        public string Name { get; set; }
        public string Name_MM { get; set; }
        public string Code {get;set;}
        public bool IsActive { get; set; }
    }

    public class CreatePermanentDisabilityRequest: PermanentDisabilityRequest
    {
        
    }

    public class UpdatePermanentDisabilityRequest : PermanentDisabilityRequest
    {
        public Guid ID { get; set; }
    }
}