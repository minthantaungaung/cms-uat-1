using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request.ClaimIncurredLocation
{
    public class ClaimIncurredLocationRequest
    {
        public string Name { get; set; }
        public string Name_MM { get; set; }
        public string Code {get;set;}
        public bool IsActive { get; set; }
    }

    public class CreateClaimIncurredLocationRequest: ClaimIncurredLocationRequest
    {
        
    }

    public class UpdateClaimIncurredLocationRequest : ClaimIncurredLocationRequest
    {
        public Guid ID { get; set; }
    }
}