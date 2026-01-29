using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class InsuranceType
{
    public Guid InsuranceTypeId { get; set; }

    public string? InsuranceTypeEn { get; set; }

    public string? InsuranceTypeMm { get; set; }

    public string? InsuranceTypeImage { get; set; }

    public virtual ICollection<InsuranceBenefit> InsuranceBenefits { get; set; } = new List<InsuranceBenefit>();
}
