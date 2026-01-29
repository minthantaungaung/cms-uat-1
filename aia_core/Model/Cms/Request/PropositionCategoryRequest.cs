using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class PropositionCategoryRequest
    {
        [Required]
        public string? NameEn { get; set; }

        [Required]
        public string? NameMm { get; set; }
    }
    public class CreatePropositionCategoryRequest : PropositionCategoryRequest 
    {
        [Required]
        public IFormFile? IconImage { get; set; }

        [Required]
        public IFormFile? BackgroundImage { get; set; }
    }
    public class UpdatePropositionCategoryRequest : PropositionCategoryRequest 
    {
        [Required]
        public Guid? Id { get; set; }

        public IFormFile? IconImage { get; set; }
        public IFormFile? BackgroundImage { get; set; }
    }
}
