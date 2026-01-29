using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ClaimBenefit
{
    public Guid Id { get; set; }
    public Guid? MainClaimId { get; set; }

    public Guid? ClaimId { get; set; }

    public string? BenefitName { get; set; }

    public DateTime? BenefitFromDate { get; set; }

    public DateTime? BenefitToDate { get; set; }

    public decimal? BenefitAmount { get; set; }

    public decimal? TotalCalculatedAmount { get; set; }
    public string? BenefitCode { get; set; }
    public int? NumOfDays { get; set; }

    public virtual ClaimTran? Claim { get; set; }
}
