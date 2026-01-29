using aia_core.Model.Cms.Request.Notification;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace aia_core.Entities
{
    public partial class CmsNotification
    {
        public Guid? Id { get; set; }
        public string? TitleEn { get; set; }
        public string? TitleMm { get; set; }
        public string? DescEn { get; set; }
        public string? DescMm { get; set; }
        public string? Image { get; set; }
        public string? FullImageUrl { get; set; }
        public DateTime? SendDateAndTime { get; set; }
        public string? Audience { get; set; }
        [JsonIgnore]
        public string? AudienceCount { get; set; }
        public string? MemberType { get; set; }
        public string? ProductType { get; set; }
        public string? PolicyStatus { get; set; }
        public string? Country { get; set; }
        public string? Province { get; set; }
        public string? District { get; set; }
        public string? Township { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public int? JobId { get; set; }
        public string? SendingStatus { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
