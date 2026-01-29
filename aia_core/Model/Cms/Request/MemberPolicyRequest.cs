using aia_core.Converter;
using aia_core.Model.Cms.Request.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class MemberPolicyRequest
    {
        public string? MemberName { get; set; }

        public string? MemberId { get; set; }

        public string? MemberPhone { get; set; }

        public string? MemberEmail { get; set; }

        public string? MemberIden { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public EnumIndividualMemberType? MemberType { get; set; }

        //[JsonConverter(typeof(JsonStringEnumConverter))]
        //public EnumIdenType[]? MemberIdenType { get; set; }

        public bool? MemberIsActive { get; set; }

        [JsonIgnore]
        public EnumSqlQueryType? QueryType { get; set; }

        public string? MemberIdenType { get; set; }
    }

    public class MemberPolicyListRequest : MemberPolicyRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        [DefaultValue(1)]
        public int? Page { get; set; }

        [Required]
        [Range(10, 100)]
        [DefaultValue(10)]
        public int? Size { get; set; }
    }
}
