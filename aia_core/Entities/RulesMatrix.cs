using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class RulesMatrix
{
    public Guid Id { get; set; }

    public string? FrequencyName { get; set; }

    public string? FromFrequency { get; set; }

    public string? Monthly { get; set; }

    public string? Quarterly { get; set; }

    public string? SemiAnnually { get; set; }

    public string? Annually { get; set; }
}
