using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Product
{
    public Guid ProductId { get; set; }

    public decimal? ProductTypeId { get; set; }

    public string? ProductTypeShort { get; set; }

    public string? TitleEn { get; set; }

    public string? TitleMm { get; set; }

    public string? ShortEn { get; set; }

    public string? ShortMm { get; set; }

    public string? LogoImage { get; set; }

    public string? CoverImage { get; set; }

    public string? IntroEn { get; set; }

    public string? IntroMm { get; set; }

    public string? TaglineEn { get; set; }

    public string? TaglineMm { get; set; }

    public string? IssuedAgeFrom { get; set; }

    public string? IssuedAgeTo { get; set; }
    public string? IssuedAgeFromMm { get; set; }

    public string? IssuedAgeToMm { get; set; }

    public string? PolicyTermUpToEn { get; set; }

    public string? PolicyTermUpToMm { get; set; }

    public string? WebsiteLink { get; set; }

    public string? Brochure { get; set; }

    public string? CreditingLink { get; set; }

    public DateTime? CreatedDate { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDelete { get; set; }

    public bool? NotAllowedInProductList { get; set; }

    public virtual ICollection<ProductBenefit> ProductBenefits { get; set; } = new List<ProductBenefit>();

    public virtual ICollection<ProductCoverage> ProductCoverages { get; set; } = new List<ProductCoverage>();

}
