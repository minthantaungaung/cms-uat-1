using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request.Hospital
{
    public class HospitalRequest
    {
        public string Name { get; set; }
        public string Name_MM { get; set; }
        public string Code {get;set;}
        public bool IsActive { get; set; }
    }

    public class CreateHospitalRequest: HospitalRequest
    {
        
    }

    public class UpdateHospitalRequest : HospitalRequest
    {
        public Guid ID { get; set; }
    }
}