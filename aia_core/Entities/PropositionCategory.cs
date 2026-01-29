using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class PropositionCategory
{
    public Guid Id { get; set; }

    public string? NameEn { get; set; }

    public string? NameMm { get; set; }

    public string? IconImage { get; set; }

    public string? BackgroundImage { get; set; }
    public bool? IsDelete { get; set; }

    public bool? IsAiaBenefitCategory { get; set; }

    public DateTime? CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }

    public virtual ICollection<Proposition> Propositions { get; set; } = new List<Proposition>();
}
