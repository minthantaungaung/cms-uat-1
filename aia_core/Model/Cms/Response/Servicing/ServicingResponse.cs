using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace aia_core.Model.Cms.Response.Servicing
{
    public class ServicingListResponse
    {
        public Guid? MainId { get; set; }
        public Guid? ServiceId { get; set; }
        public string? PolicyNumber { get; set; }
        public string? PolicyStatus { get; set; }
        public string? MemberName { get; set; }
        public string? MemberPhone { get; set; }
        public string? GroupMemberId { get; set; }
        public string? MemberType { get; set; }
        public string? MemberId { get; set; }
        public string? ServiceName { get; set; }
        public string? ServiceType { get; set; }
        public string? ServiceStatus { get; set; }
        public string? RemainingTime { get; set; }
        public DateTime? SubmissionDate { get; set; }
        public string? StatusUpdatedBy { get; set; }
        public DateTime? StatusUpdatedDate { get; set; }
        public DateTime? CreatedDate { get; set; }

        public bool? IsPending { get; set; }

        [JsonIgnore]
        public Guid? AppMemberId { get; set; }

        public string? UpdateChannel  { get; set; }
    }


    
}
