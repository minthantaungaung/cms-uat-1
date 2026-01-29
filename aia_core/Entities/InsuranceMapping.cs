using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class InsuranceMapping
{
    public Guid Id { get; set; }

    public Guid? ClaimId { get; set; }

    public string? ProductCode { get; set; }

    public string? ComponentCode { get; set; }

    public virtual InsuranceBenefit? Benefit { get; set; }
}
