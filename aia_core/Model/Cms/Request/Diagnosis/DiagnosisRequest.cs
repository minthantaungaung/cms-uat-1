using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request.Diagnosis
{
    public class DiagnosisRequest
    {
        public string Name { get; set; }
        public string Name_MM { get; set; }
        public string Code {get;set;}
        public bool IsActive { get; set; }
    }

    public class CreateDiagnosisRequest: DiagnosisRequest
    {
        
    }

    public class UpdateDiagnosisRequest : DiagnosisRequest
    {
        public Guid ID { get; set; }
    }
}