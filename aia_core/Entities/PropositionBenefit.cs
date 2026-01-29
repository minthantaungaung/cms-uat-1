using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class PropositionBenefit
{
    public Guid Id { get; set; }

    public Guid? PropositionId { get; set; }

    public string? NameEn { get; set; }

    public string? NameMm { get; set; }

    public string? Type { get; set; }

    public string? GroupNameEn { get; set; }

    public string? GroupNameMm { get; set; }
    public int? Sort { get; set; }

    public virtual Proposition? Proposition { get; set; }
}
