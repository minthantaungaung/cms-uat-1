using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Blog
{
    public Guid Id { get; set; }

    public string? TitleEn { get; set; }

    public string? TitleMm { get; set; }

    public string? CategoryType { get; set; }

    public string? CoverImage { get; set; }

    public string? ThumbnailImage { get; set; }

    public string? TopicEn { get; set; }

    public string? TopicMm { get; set; }

    public string? ReadMinEn { get; set; }

    public string? ReadMinMm { get; set; }

    public string? BodyEn { get; set; }

    public string? BodyMm { get; set; }

    public DateTime? PromotionStart { get; set; }

    public DateTime? PromotionEnd { get; set; }

    public bool? IsFeature { get; set; }

    public DateTime? CreatedDate { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDelete { get; set; }

    public int? Sort { get; set; }
    public string? ShareableLink { get; set; }
    public string? JobId { get; set; }

    public virtual ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
}
