using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ServicePolicyMapping
{
    public Guid Id { get; set; }

    public string? ServiceType { get; set; }

    public string? ProductType { get; set; }

    public string? PolicyStatus { get; set; }
    public string? PremiumStatus { get; set; }
}
