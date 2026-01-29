using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Coverage
{
    public Guid CoverageId { get; set; }

    public string? CoverageNameEn { get; set; }

    public string? CoverageNameMm { get; set; }

    public string? CoverageIcon { get; set; }

    public DateTime? CreatedDate { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDelete { get; set; }

    public virtual ICollection<ProductCoverage> ProductCoverages { get; set; } = new List<ProductCoverage>();
}
