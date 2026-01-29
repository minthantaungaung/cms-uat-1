using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using aia_core.Model.Cms.Request.Common;
using System.Text.Json.Serialization;

namespace aia_core.Model.Cms.Request.Servicing
{
    public class ServicingListRequest
    {
       
        public int Page { get; set; } = 1;

        
        public int Size { get; set; } = 10;
        public string? MemberName { get; set; }
        public string? MemberId { get; set; }
        public string? MemberPhone { get; set; }
        public string? PolicyNumber { get; set; }
        public string? PolicyStatus { get; set; }
        public string? MainId { get; set; }
        public string? ServiceId { get; set; }
        public List<string>? ServiceType { get; set; }
        public string? ServiceStatus { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class ServiceStatusUpdateRequest
    {
        [Required]
        public Guid? ServiceId { get; set; }

        [Required]
        public EnumServiceType? ServiceType { get; set; }

        [Required]
        [RegularExpression("^(Approved|NotApproved|Paid|Pending)$", ErrorMessage = "Status must be 'Approved' or 'NotApproved' or 'Paid' or 'Pending'")]
        public string? Status { get; set; }
        public string? InternalRemark { get; set; }
    }


    public class ServiceImagingLogRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int? Page { get; set; }

        [Required]
        [Range(10, 100)]
        [DefaultValue(10)]
        public int? Size { get; set; }

        [RegularExpression(@"^[^']*$")]
        public string? ServiceId { get; set; }

        [RegularExpression(@"^[^']*$")]
        public string? MainServiceId { get; set; }

        [RegularExpression(@"^[^']*$")]
        public string? PolicyNo { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public EnumServiceType[]? ServiceType { get; set; }
        public string? PhoneNo { get; set; }


        [JsonIgnore]
        public EnumSqlQueryType? QueryType { get; set; }
        public string? FormID { get; set; }
    }

    public class ServiceFailedLogRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int? Page { get; set; }

        [Required]
        [Range(10, 100)]
        [DefaultValue(10)]
        public int? Size { get; set; }
        public string? PolicyNo { get; set; }
        public DateTime? FromDt { get; set; }
        public DateTime? ToDt { get; set; }
        public string? PhoneNo { get; set; }
        public string? MemberId { get; set; }

        [JsonIgnore]
        public EnumSqlQueryType? QueryType { get; set; }
    }
}
