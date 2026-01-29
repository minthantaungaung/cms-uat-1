using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class ClaimStatusUpdateRequest
    {
        public Guid ClaimId { get; set; }
        public string? Status { get; set; }

        public string? Reason { get; set; } = string.Empty;

        public decimal? EligibleAmount { get; set; }


        public bool? IsOnlyUpdateAmount { get; set; } = false;


    }
}