using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class AuthorizationPerson
{
    public Guid Id { get; set; }

    public string PersonType { get; set; } = null!;

    public bool Registration { get; set; }

    public bool Login { get; set; }

    public bool ViewMyPolicies { get; set; }

    public bool Proposition { get; set; }
    public bool Claim { get; set; }

    public bool PolicyHolderDetails { get; set; }

    public bool InsuredDetails { get; set; }

    public bool BeneficiaryInfo { get; set; }

    public bool PaymentFrequency { get; set; }

    public bool Acp { get; set; }

    public bool LoanRepayment { get; set; }

    public bool AdhocTopup { get; set; }

    public bool HealthRenewal { get; set; }

    public bool LapseReinstatement { get; set; }

    public bool PartialWithdrawal { get; set; }

    public bool PolicyLoan { get; set; }

    public bool PolicyPaidup { get; set; }

    public bool PolicySurrender { get; set; }

    public bool RefundofPayment { get; set; }

    public bool SumAssuredChange { get; set; }

    public bool PolicyLoanRepayment { get; set; }
}
