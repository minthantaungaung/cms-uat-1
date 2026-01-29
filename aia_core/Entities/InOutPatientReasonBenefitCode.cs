using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace aia_core.Entities;

public partial class InOutPatientReasonBenefitCode
{
    public string? ProductCode { get; set; }

    public string? ComponentCode { get; set; }

    public string? ClaimType { get; set; }

    public string? ReasonCode { get; set; }

    public string? BenefitCode { get; set; }

    public string? BenefitName { get; set; }

    public bool? CheckBenefit { get; set; }

    public Guid? Id { get; set; }

    [NotMapped]
    public DateTime? FromDate { get; set; }

    [NotMapped]
    public DateTime? ToDate { get; set; }

    [NotMapped]
    public decimal? Amount { get; set; }

    [NotMapped]
    public decimal? TotalAmount { get; set; }
}
