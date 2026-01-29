using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ProductBenefit
{
    public Guid ProductBenefitId { get; set; }

    public Guid? ProductId { get; set; }

    public string? TitleEn { get; set; }

    public string? TitleMm { get; set; }

    public string? DescriptionEn { get; set; }

    public string? DescriptionMm { get; set; }

    public int? Sort { get; set; }

    public DateTime? CreatedDate { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDelete { get; set; }

    public virtual Product? Product { get; set; }
}
