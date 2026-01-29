
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using aia_core.Converter;

namespace aia_core.Model.Mobile.Request.Notification
{
    public class NotificationRequest : PagingRequest
    {
        public EnumNotificationType? NotificationType { get; set; }
        //public EnumClaimStatusDesc? ClaimStatus { get; set; }
        public string? ServiceStatus { get; set; }

        public string? ClaimStatus { get; set; }
        public bool? IsRead { get; set; }

        public int? UnreadCount { get; set; }

        [JsonIgnore]
        public Guid? MemberId { get; set; }
    }
}
