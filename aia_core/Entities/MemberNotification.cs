using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class MemberNotification
{
    public Guid Id { get; set; }

    public Guid MemberId { get; set; }

    public string Message { get; set; } = null!;

    public string? MessageMm { get; set; } = null!;

    public string? TitleEn { get; set; } = null!;
    public string? TitleMm { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? ClaimId { get; set; } = null!;

    public string? ServicingId { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public bool? IsRead { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual Claim? Claim { get; set; }

    public bool? IsSytemNoti { get; set; }

    public string? SystemNotiType { get; set; }

    public DateTime? PublishedDate { get; set; }
    public bool? IsScheduledDone { get; set; }
    public bool? IsScheduled { get; set; }
    public string? ProductId { get; set; }
    public string? PromotionId { get; set; }
    public string? PropositionId { get; set; }
    public string? ActivityId { get; set; }

    public string? JobId { get; set; }

    public string? ServiceType { get; set; }
    public string? ServiceStatus { get; set; }

    public string? ClaimStatus { get; set; }
    public string? ClaimType { get; set; }
    public string? ClaimStatusCode { get; set; }
    public string? PremiumPolicyNo { get; set; }
    public Guid? CmsNotificationId { get; set; }
    public string? ImageUrl { get; set; }

    public string? CommonKeyId { get; set; }

}
