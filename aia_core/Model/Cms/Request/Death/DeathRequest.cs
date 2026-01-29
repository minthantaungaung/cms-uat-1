using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request.Death
{
    public class DeathRequest
    {
        public string Name { get; set; }
        public string Name_MM { get; set; }
        public string Code {get;set;}
        public bool IsActive { get; set; }
    }

    public class CreateDeathRequest: DeathRequest
    {
        
    }

    public class UpdateDeathRequest : DeathRequest
    {
        public Guid ID { get; set; }
    }
}