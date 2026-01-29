using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ClaimsStatusUpdate
{
    public Guid Id { get; set; }

    public string? ClaimId { get; set; }

    public string? OldStatus { get; set; }

    public string? NewStatus { get; set; }

    public DateTime? CreatedDate { get; set; }

    public bool? IsDone { get; set; }

    public bool? IsDeleted { get; set; }

    public bool? ChangedByAiaPlus { get; set; }

    public virtual Claim? Claim { get; set; }

    public string? NewStatusDesc { get; set; }
    public string? NewStatusDescMm { get; set; }

    public string? Reason { get; set; }

    public string? RemarkFromIL { get; set; }
    public decimal? PayableAmountFromIL { get; set; }

    public string? FormattedReason { get; set; }
}
