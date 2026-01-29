using System.ComponentModel.DataAnnotations;
using aia_core.Entities;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Routing.Patterns;

namespace aia_core.Model.Mobile.Request.Servicing
{
    public class ServicingRequestModel
    {       
        public ClaimOtp? ClaimOtp { get; set; }

        public List<string> ClientNo {get;set;}
        public EnumServicingType ServicingType { get; set; }

        public ServicingUpdateModel MaritalStatus {get;set;}
        public ServicingUpdateModel FatherName { get; set; }
        public ServicingUpdateModel PhoneNumber { get; set; }
        public ServicingUpdateModel EmailAddress { get; set; }
        public ServicingUpdateModel Country { get; set; }
        public ServicingUpdateModel Province { get; set; }
        public ServicingUpdateModel Distinct { get; set; }
        public ServicingUpdateModel Township { get; set; }
        public ServicingUpdateModel Building { get; set; }
        public ServicingUpdateModel Street { get; set; }
        public string? SignatureImage { get; set; }

        public bool? IsAllProfileUpdate { get; set; }

        [Ignore]
        public Guid? MainId { get; set; }

        [Ignore]
        public Guid? MemberId { get; set; }

        [Ignore]
        public string? ServicingEmail { get; set; }

        [Ignore]
        public bool IsSkipOtpValidation { get; set; } = false;
    }

    public class ClaimOtp
    {
        [Required]
        public string? ReferenceNo { get; set; }

        [Required]
        public string? OtpCode { get; set; }
    }

    public class ServicingUpdateModel
    {
        public string? Old { get; set; }
        public string? New { get; set; }
    }
}