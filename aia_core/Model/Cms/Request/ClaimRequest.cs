using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace aia_core.Model.Cms.Request
{
    public enum EnumQueryType
    { 
        List,
        Export
    }
    public class ClaimRequest
    {
        //[Required]
        [Range(1, int.MaxValue)]
        //[DefaultValue(1)]
        public int? Page { get; set; } = 1;

        //[Required]
        [Range(10, 100)]
        //[DefaultValue(10)]
        public int? Size { get; set; } = 10;

        [RegularExpression(@"^[^']*$")]
        public string? MemberName { get; set; }

        [RegularExpression(@"^[^']*$")]
        public string? ClientNo { get; set; }
        public string? RequestId { get; set; }
        public string? DetailId { get; set; }

        [RegularExpression(@"^[^']*$")]
        public string? PolicyNo { get; set; }

        [RegularExpression(@"^[^']*$")]
        public string? MemberPhone { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        //public List<EnumBenefitFormType>? ClaimType { get; set;}
        public string? ClaimStatus { get; set; }

        [JsonIgnore]
        public EnumQueryType? QueryType { get; set; }

        public EnumILStatus? ILStatus { get; set; }
        public object? ClaimType { get; set; }

        public List<string>? ClaimTypeList { get; set; }
    }

    public class FailedLogRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int? Page { get; set; }

        [Required]
        [Range(10, 100)]
        [DefaultValue(10)]
        public int? Size { get; set; }
        public string? ClaimId { get; set; }
        public string? MainClaimId { get; set; }
        public string? PolicyNo { get; set; }
        public DateTime? FromDt { get; set; }
        public DateTime? ToDt { get; set; }
        public List<EnumBenefitFormType>? ClaimType { get; set; }
        public string? ClaimStatus { get; set; }
        public string? PhoneNo { get; set; }
    }

    public class ImagingLogRequest
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
        public string? ClaimId { get; set; }
        public string? MainClaimId { get; set; }

        [RegularExpression(@"^[^']*$")]
        public string? PolicyNo { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<EnumBenefitFormType>? ClaimType { get; set; }
        public string? PhoneNo { get; set; }

        [JsonIgnore]
        public EnumQueryType? QueryType { get; set; }

        [RegularExpression(@"^[^']*$")]
        public string? FormID { get; set; }

        public string? ResponseStatus { get; set; }
        public string? ProductCode { get; set; }
    }


    public class ClaimValidateMessageRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int? Page { get; set; }
        [Required]
        [Range(10, 100)]
        [DefaultValue(10)]
        public int? Size { get; set; }

        
        public List<string>? ClaimType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? PolicyNumber { get; set; }
        public string? MemberID { get; set; }
        public string? MemberName { get; set; }
        public string? PhoneNo { get; set; }
    }
}
