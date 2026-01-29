using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ClaimStatus
{
    public decimal Id { get; set; }

    public string? ShortDesc { get; set; } = null!;

    public string? LongDesc { get; set; } = null!;
    public string? CrmCode { get; set; } = null!;
}
