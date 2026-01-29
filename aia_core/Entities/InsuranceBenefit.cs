using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class InsuranceBenefit
{
    public Guid ClaimId { get; set; }
    public string? ClaimNameEn { get; set; }
    public Guid? BenefitId { get; set; }
    public string? BenefitNameEn { get; set; }
    public string? BenefitNameMm { get; set; }
    public string? BenefitImage { get; set; }
    public Guid? InsuranceTypeId { get; set; }
    public string? BenefitFormType { get; set; }
    public virtual ICollection<InsuranceMapping> InsuranceMappings { get; set; } = new List<InsuranceMapping>();

    public virtual InsuranceType? InsuranceType { get; set; }
}
