using System.ComponentModel.DataAnnotations;
using aia_core.Services;

namespace aia_core.Model.Cms.Response.Diagnosis
{
    public class DiagnosisResponse
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string Name_MM { get; set; }
        public string Code {get;set;}
        public bool? IsActive {get;set;}

        public DiagnosisResponse() { }
        public DiagnosisResponse(Entities.Diagnosis entity) 
        {
            ID = entity.ID;
            Name = entity.Name;
            Name_MM = entity.Name_MM;
            Code = entity.Code;
            IsActive = entity.IsActive;
        }
    }
}