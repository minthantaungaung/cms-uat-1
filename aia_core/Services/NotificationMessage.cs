using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Services
{
    //public class NotificationMessage
    //{
    //    public string? Title { get; set; }
    //    public string? Message { get; set; }
    //    public string? ImageUrl { get; set; }
    //}


    public class LocalizationMessage
    {
        public string? En { get; set; }
        public string? Mm { get; set; }
    }

    public class NotificationMessage
    {
        public Guid? MemberId { get; set; } = null;
        public Guid? ClaimId { get; set; } = null;
        public Guid? ServicingId { get; set; } = null;
        public EnumServiceType? ServiceType { get; set; } = null;
        public EnumNotificationType? NotificationType { get; set; } = null;
        public string? Title { get; set; }
        public string? Message { get; set; }
        public string? ImageUrl { get; set; }
        public string? ProductId { get; set; } = null;
        public string? PromotionId { get; set; } = null;
        public string? PropositionId { get; set; } = null;
        public string? ActivityId { get; set; } = null;
        public bool? IsSytemNoti { get; set; } = false;
        public EnumSystemNotiType? SystemNotiType { get; set; } = null;

        public string? NotificationId { get; set; } = null;
        public string? PolicyNumber { get; set; } = null;

        public List<string>? PushTokenList { get; set; }

        public string? CommonKeyId { get; set; }
    }
}
