using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request.Crm
{
    public class UpdateClaimCrmRequest
    {
        public string? Claim_id { get; set; }
        public string? Claim_id_IL { get; set; }
        public string? Policy_number {get;set;}
        public string? Product_type { get; set; }
        // public string Bank_name { get; set; }
        // public string Account_no { get; set; }
        public string? Claiment_client_no { get; set; }
        public string? Claim_type { get; set; }
        public string Status { get; set; }
        public decimal? Claim_Amount {get;set;} 
        public DateTime Received_date { get; set; }
        public string? Reject_reason { get; set; }
        public string? Generate_id { get; set; }
        public string? Request_date {get;set;}
        //public UpdateBankCrmRequest Bank_info { get; set; }
        public string? case_category {get;set;}
        public string? service_id {get;set;}
        public string channel {get;set;}
        public string? comment {get;set;}
    }

    public class UpdateBankCrmRequest
    {
        public string? Bank_code { get; set; }
        public string? Bank_accound_holder_name { get; set; }
        public string? Bank_account_number { get; set; }
    }
}