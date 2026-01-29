using aia_core.Extension;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class CoverageRequest
    {
        [Required, MaxLength(250)]
        public string? CoverageNameEN { get; set; }

        [Required, MaxLength(250)]
        public string? CoverageNameMm { get; set; }
    }
    public class CreateCoverageRequest : CoverageRequest
    {
        [Required]
        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]
        public IFormFile? CoverageIcon { get; set; }
    }

    public class UpdateCoverageRequest: CoverageRequest
    {
        [Required]
        public Guid? CoverageId { get; set; }

        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]
        public IFormFile? CoverageIcon { get; set; }
    }
}
