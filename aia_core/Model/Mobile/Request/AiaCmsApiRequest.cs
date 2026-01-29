using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Request
{
    internal class AiaCmsApiRequest
    {
    }

    public class GetDocumentListRequest
    {
        public string templateId { get; set; }
        public string[] PolicyNo { get; set; }
        public string[] docTypeId { get; set; }
        public string receiveDateStart { get; set; }
        public string receiveDateEnd { get; set; }
        public int pageNum { get; set; }
        public int pageSize { get; set; }
    }

    public class UploadBase64Request
    {
        public string? file { get; set; }
        public string? PolicyNo { get; set; }

        [JsonProperty("templateId")]
        public string? templateId { get; set; }

        [JsonProperty("docTypeId")]
        public string? docTypeId { get; set; }

        [JsonProperty("fileName")]
        public string? fileName { get; set; }

        [JsonProperty("format")]
        public string? format { get; set; }

        [JsonProperty("membershipId")]
        public string membershipId { get; set; }

        [JsonProperty("claimId")]
        public Guid claimId { get; set; }
    }
}
