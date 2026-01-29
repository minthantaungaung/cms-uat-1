using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class PropositionBranch
{
    public Guid Id { get; set; }

    public Guid? PropositionId { get; set; }

    public string? NameEn { get; set; }

    public string? NameMm { get; set; }

    public int? Sort { get; set; }

    public virtual Proposition? Proposition { get; set; }
}
