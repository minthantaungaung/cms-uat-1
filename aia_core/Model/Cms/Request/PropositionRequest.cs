using aia_core.Extension;
using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class PropositionRequest : IValidatableObject
    {
        [Required]
        public EnumPropositionType? Type { get; set; }
        
        [Required]
        public string? NameEn { get; set; }

        [Required]
        public string? NameMm { get; set; }

        public Guid? PropositionCategoryId { get; set; }

        [Required]
        public string? DescriptionEn { get; set; }

        [Required]
        public string? DescriptionMm { get; set; }

        [Required]
        public EnumPropositionBenefit? Eligibility { get; set; }

        public EnumHotlineType? HotlineType { get; set; }

        public string? PartnerPhoneNumber { get; set; }

        public string? PartnerWebsiteLink { get; set; }

        public string? PartnerFacebookUrl { get; set; }

        public string? PartnerInstagramUrl { get; set; }

        public string? PartnerTwitterUrl { get; set; }

        public string? HotlineButtonTextEn { get; set; }

        public string? HotlineButtonTextMm { get; set; }

        public string? HotlineNumber { get; set; }
        public string? AddressLabel { get; set; }
        public string? AddressLabelMm  { get; set; }

        public PropositionBranchRequest[]? PartnerBranches { get; set; }

        public PropositionAddressRequest[]? Addresses { get; set; }

        [Required]
        public PropositionBenefitRequest[]? PropositionBenefits { get; set; }

        public bool? AllowToShowCashlessClaim { get; set; }
        public EnumCashlessClaimProcedureInfo? CashlessClaimProcedureInfo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PropositionBenefits == null 
                || PropositionBenefits?.Any() == false) yield return new ValidationResult("required proposition benefits");

            if(Type == EnumPropositionType.partner)
            {
                if (PropositionCategoryId.HasValue == false) yield return new ValidationResult("required proposition category id.");
                if (string.IsNullOrEmpty(PartnerPhoneNumber)) yield return new ValidationResult("required partner phone number.");
                //if (string.IsNullOrEmpty(PartnerWebsiteLink)) yield return new ValidationResult("required partner website link.");
                //if (string.IsNullOrEmpty(PartnerFacebookUrl)) yield return new ValidationResult("required partner facebook url.");
                //if (string.IsNullOrEmpty(PartnerTwitterUrl)) yield return new ValidationResult("required partner twitter url.");
                //if (string.IsNullOrEmpty(PartnerInstagramUrl)) yield return new ValidationResult("required partner instgram url.");
                //if (PartnerBranches == null
                //    || PartnerBranches?.Any() == false) yield return new ValidationResult("required partner branches");
            }
            else if(Type == EnumPropositionType.aia)
            {
                if (HotlineType.HasValue == false) yield return new ValidationResult("required hotline type.");
                if(HotlineType.HasValue
                    && HotlineType.Value == EnumHotlineType.custom_number)
                {
                    if (string.IsNullOrEmpty(HotlineButtonTextEn)) yield return new ValidationResult("required hotline button text en.");
                    if (string.IsNullOrEmpty(HotlineButtonTextEn)) yield return new ValidationResult("required hotline button text en.");
                    if (string.IsNullOrEmpty(HotlineNumber)) yield return new ValidationResult("required hotline number.");
                }
                //if (string.IsNullOrEmpty(AddressLabel)) yield return new ValidationResult("required address label.");
                if (!string.IsNullOrEmpty(AddressLabel)
                    && (Addresses == null || Addresses?.Any() == false)) yield return new ValidationResult("required addresses");
            }
        }
    }
    public class CreatePropositionRequest : PropositionRequest 
    {
        [Required]
        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]
        public IFormFile? LogoImage { get; set; }

        [Required]
        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]
        public IFormFile? BackgroudImage { get; set; }
    }
    public class UpdatePropositionRequest : PropositionRequest 
    {
        [Required]
        public Guid? Id { get; set; }

        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]
        public IFormFile? LogoImage { get; set; }

        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]

        public IFormFile? BackgroudImage { get; set; }
    }
    public class PropositionOrderRequest
    {
        [Required]
        public Guid? Id { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int? Order { get; set; }
    }
}
