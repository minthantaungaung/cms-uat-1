using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class PolicyAdditionalAmt
{
    public string PolicyNo { get; set; } = null!;

    public decimal? PremiumDueAmount { get; set; }

    public decimal? AcpPrincipalAmount { get; set; }

    public decimal? AcpInterestAmount { get; set; }

    public decimal? LoanPrincipalAmount { get; set; }

    public decimal? LoanInterestAmount { get; set; }

    public decimal? HealthRenewalAmount { get; set; }

    public decimal? ReinstatementPremiumAmount { get; set; }

    public decimal? ReinstatementInterestAmount { get; set; }
}
