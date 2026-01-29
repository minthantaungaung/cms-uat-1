using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class PropositionBenefitRequest
    {
        [Required]
        public EnumPropositionBenefit? Type { get; set; }

        [Required]
        public string? NameEn { get; set; }

        [Required]
        public string? NameMm { get; set; }

        public string? GroupNameEn { get; set; }

        public string? GroupNameMm { get; set; }

        public int? Sort { get; set; }
    }
}
