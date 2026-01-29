using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class PropositionBranchRequest
    {
        [Required]
        public string? NameEn { get; set; }

        [Required]
        public string? NameMm { get; set; }
    }
}
