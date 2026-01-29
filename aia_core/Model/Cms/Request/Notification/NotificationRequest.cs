using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;

namespace aia_core.Model.Cms.Request.Notification
{
    public enum EnumNotiStatus
    {
        Pending,
        Sending,
        Sent,
    }

    public enum EnumNotiAudience
    {
        All,
        Manual,
    }

    public class NotificationRequest
    {
        [Required]

        public int Page { get; set; }

        [Required]
        public int Size { get; set; }

        public string? Title { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set;}
        public EnumNotiStatus? SendingStatus { get; set; }
        public bool? IsActive { get; set; }
        public EnumNotiAudience? Audience { get; set; }
        public EnumIndividualMemberType? MemberType { get; set; }
        public string? ProductType { get; set; }
        public string? PolicyStatus { get; set; }
        public string? Country { get; set; }
        public string? Province { get; set; }
        public string? District { get; set;}
        public string? Township { get; set; }
    }

    public class CreateNotificationRequest
    {
        public string? TitleEn { get; set; }
        public string? TitleMm { get; set; }
        public string? DescEn { get; set; }
        public string? DescMm { get; set; }
        public IFormFile? Image { get; set; }

        public DateTime? SendDateAndTime { get; set; }


        public bool? SendNow { get; set; }
        public EnumNotiAudience? Audience { get; set; }
        public EnumIndividualMemberType? MemberType { get; set;  }

        public string? ProductType { get;  set ; }

        public string? PolicyStatus { get; set; }

        public string? Country { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Township { get; set; }

        public bool? IsActive { get; set; }

    }
    
    public class UpdateNotificationRequest : CreateNotificationRequest
    { 
        public Guid? Id { get; set; }
    }

    public class ThirdPartyNotificationRequest
    {
        public bool SMS { get; set; }
        public bool PushNotification { get; set; }
        public bool Email { get; set; }
        public bool Recipient { get; set; }
        public string ClientId { get; set; }
        public string Message { get; set; }
        public string Subject { get; set; }
    }
    public class ThirdPartyNotificationResponse
    {
        public string ClientId { get; set; }
        public bool isSuccess { get; set; }
    }
}
