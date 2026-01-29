using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Claim
{
    public string? ClaimId { get; set; }

    public string? ClaimIdIl { get; set; }

    public string PolicyNo { get; set; } = null!;

    public string? ProductType { get; set; }

    public string? ClaimType { get; set; }

    public string? BankName { get; set; }

    public string? AccountNo { get; set; }

    public string Status { get; set; } = null!;

    public string ClaimentClientNo { get; set; } = null!;

    public DateTime? ReceivedDate { get; set; }

    public string? RejectReason { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? FollowupReason { get; set; }

    public virtual Client ClaimentClientNoNavigation { get; set; } = null!;

    public virtual Policy PolicyNoNavigation { get; set; } = null!;

    public virtual ProductType? ProductTypeNavigation { get; set; }

    public virtual ClaimStatus StatusNavigation { get; set; } = null!;

    public virtual ICollection<ClaimsStatusUpdate> ClaimsStatusUpdates { get; set; } = new List<ClaimsStatusUpdate>();

    public virtual ICollection<MemberNotification> MemberNotifications { get; set; } = new List<MemberNotification>();

    //public virtual ClaimTran ClaimTran { get; set; }
}
