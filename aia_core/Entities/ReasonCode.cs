using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ReasonCode
{
    public string? ProductCode { get; set; }

    public string? ComponentCode { get; set; }

    public string? ClaimType { get; set; }

    public string? ReasonCode1 { get; set; }

    public Guid? Id { get; set; }
}
