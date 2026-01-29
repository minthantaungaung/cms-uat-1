using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Beneficiary
{
    public string PolicyNo { get; set; } = null!;

    public string BeneficiaryClientNo { get; set; } = null!;

    public string? Relationship { get; set; }

    public decimal? Percentage { get; set; }

    

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual Client BeneficiaryClientNoNavigation { get; set; } = null!;

    public virtual Policy PolicyNoNavigation { get; set; } = null!;
}
