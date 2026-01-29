using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class CrmClaimCode
{
    public string? ClaimType { get; set; }

    public string? ClaimCode { get; set; }

    public Guid Id { get; set; }
}
