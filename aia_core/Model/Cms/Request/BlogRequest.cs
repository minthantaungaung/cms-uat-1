using aia_core.Extension;
using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class BlogRequest
    {
        [Required]
        public EnumCategoryType? CategoryType { get; set; }

        [Required]
        public string? TitleEn { get; set; }
        
        [Required]
        public string? TitleMm { get; set; }

        [Required]
        public string? TopicEn { get; set; }

        [Required]
        public string? TopicMm { get; set; }

        //[Required]
        public string? ReadMinEn { get; set; }

        //[Required]
        public string? ReadMinMm { get; set; }

        [Required]
        public string? BodyEn { get; set; }

        [Required]
        public string? BodyMm { get; set; }

        [Required]
        public bool? IsFeature { get; set; }

        public Guid[]? Products { get; set; }

        public string? ShareableLink { get; set; }
    }

    public class CreateBlogRequest: BlogRequest, IValidatableObject
    {
        [Required]
        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]
        public IFormFile? CoverImage { get; set; }

        [Required]
        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]
        public IFormFile? ThumbnailImage { get; set; }

        public DateTime? PromotionStart { get; set; }

        public DateTime? PromotionEnd { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CategoryType == EnumCategoryType.promotion)
            {
                if (!PromotionStart.HasValue) yield return new ValidationResult("required promotion start date/time");
                if (!PromotionEnd.HasValue) yield return new ValidationResult("required promotion end date/time");

                if(PromotionStart.HasValue 
                    && Utils.ConvertUtcDateToMMDate(PromotionStart.Value) < Utils.GetDefaultDate()) yield return new ValidationResult("invalid promotion start date/time");

                if (PromotionEnd.HasValue && PromotionStart.HasValue
                    && Utils.ConvertUtcDateToMMDate(PromotionStart.Value) > Utils.ConvertUtcDateToMMDate(PromotionEnd.Value)) yield return new ValidationResult("invalid promotion start and end date/time");
                
                if (Products == null) yield return new ValidationResult("required related products");
                if (Products != null
                    && !Products.Any()) yield return new ValidationResult("required related products");
            }
            else if (CategoryType == EnumCategoryType.activity)
            {
                if (string.IsNullOrEmpty(ReadMinEn)
                    || string.IsNullOrEmpty(ReadMinMm))
                {
                    yield return new ValidationResult("required read minutes");
                }
            }
        }
    }
    public class UpdateBlogRequest : BlogRequest, IValidatableObject
    {
        public Guid? Id { get; set; }

        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]
        public IFormFile? CoverImage { get; set; }

        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]
        public IFormFile? ThumbnailImage { get; set; }

        public DateTime? PromotionStart { get; set; }

        public DateTime? PromotionEnd { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CategoryType == EnumCategoryType.promotion)
            {
                if (!PromotionStart.HasValue) yield return new ValidationResult("required promotion start date/time");
                if (!PromotionEnd.HasValue) yield return new ValidationResult("required promotion end date/time");

                

                if (PromotionEnd.HasValue && PromotionStart.HasValue
                    && Utils.ConvertUtcDateToMMDate(PromotionStart.Value) > Utils.ConvertUtcDateToMMDate(PromotionEnd.Value)) yield return new ValidationResult("invalid promotion start and end date/time");


                if (Products == null) yield return new ValidationResult("required related products");
                if (Products != null
                    && !Products.Any()) yield return new ValidationResult("required related products");
            }
            else if (CategoryType == EnumCategoryType.activity)
            {
                if (string.IsNullOrEmpty(ReadMinEn)
                    || string.IsNullOrEmpty(ReadMinMm))
                {
                    yield return new ValidationResult("required read minutes");
                }
            }
        }
    }

    public class BlogOrderRequest
    {
        [Required]
        public Guid? Id { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int? Order { get; set; }
    }
}
