using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Policy
{
    public string ProductType { get; set; } = null!;

    public string PolicyNo { get; set; } = null!;

    public string AgentCode { get; set; } = null!;

    public string? NumberOfUnit { get; set; }

    public string? PolicyHolderClientNo { get; set; }

    public string? InsuredPersonClientNo { get; set; }

    public string? PaymentFrequency { get; set; }

    public DateTime? PaidToDate { get; set; }

    public string? PolicyStatus { get; set; }

    public string? PremiumStatus { get; set; }

    public int? PolicyTerm { get; set; }

    public int? PremiumTerm { get; set; }

    public decimal? InstallmentPremium { get; set; }

    public decimal? AnnualizedPremium { get; set; }

    public decimal? SumAssured { get; set; }

    public DateTime? FirstIssueDate { get; set; }

    public DateTime? PolicyIssueDate { get; set; }

    public DateTime? OriginalCommencementDate { get; set; }

    public DateTime? RiskCommencementDate { get; set; }

    public string? Components { get; set; }

    public string? AcpModeFlag { get; set; }

    public decimal? PremiumDue { get; set; }

    public decimal? OutstandingPremium { get; set; }

    public decimal? OutstandingInterest { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public DateTime? PolicyExpiryDate { get; set; }

    public DateTime? PolicyLapsedDate { get; set; }

    public virtual Client? InsuredPersonClientNoNavigation { get; set; }

    public virtual Client? PolicyHolderClientNoNavigation { get; set; }

    public virtual PolicyStatus? PolicyStatusNavigation { get; set; }

    public virtual PremiumStatus? PremiumStatusNavigation { get; set; }

    public virtual ProductType ProductTypeNavigation { get; set; } = null!;
}
