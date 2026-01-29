using System.Text.Json.Serialization;

namespace aia_core.Model.Mobile.Response
{
    public class PropositionGroupListResponse
    {
        public string CategoryName_EN { get; set; }
        public string CategoryName_MM { get; set; }
        public bool? IsAiaBenefitCategory { get; set; }

        [JsonIgnore]
        public DateTime? CreatedOn { get; set; }
        public Guid? CategoryID {get;set;}
        public List<PropositionsResponse> list {get;set;}
        
    }

}
