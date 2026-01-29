using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Routing.Patterns;

namespace aia_core.Model.Mobile.Request.Servicing
{

    public class ILBeneficiariesRequest
    {
        public BeneficiariesPolicyModel policy { get; set; }
        public string requestType { get; set; }
        public List<BeneficiariesNoteListModel> noteList { get; set; }
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class ILBeneficiaryModel
    {
        public string action { get; set; }
        public string? clientNumber { get; set; }
        public DateTime dob { get; set; }
        public string gender { get; set; }
        public string idnumber { get; set; }
        public string idtype { get; set; }
        public string name { get; set; }
        public string percentage { get; set; }
        public string phone { get; set; }
        public string relation { get; set; }
        public string updateClientLevel { get; set; }
        public string townshipCode { get; set; }
    }

    public class BeneficiariesNoteListModel
    {
        public string noteType { get; set; }
        public List<object> notes { get; set; }
    }

    public class BeneficiariesPolicyModel
    {
        public List<ILBeneficiaryModel> beneficiaries { get; set; }
        public string policyNumber { get; set; }
    }




}