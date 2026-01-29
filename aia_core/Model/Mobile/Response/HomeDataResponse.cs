using aia_core.Model.Mobile.Response.MemberPolicyResponse;
using System.Text.Json.Serialization;

namespace aia_core.Model.Mobile.Response
{
    public class HomeDataResponse
    {
        //public List<ClaimRequest> ClaimRequest {get;set;}
        public List<UpcomingPremiumList> UpcomingPremium {get;set;}
        public List<PropositionsCategoryResponse> PropositionsCategory {get;set;}
        public List<PromotionResponse> Promotion {get;set;}
        public List<ProductResponse> Product {get;set;}
        public SaleConsultantResponse SaleConsultantInfo { get; set; }
    }

    public class SaleConsultantResponse
    {
        public bool? hasSaleConsultant { get; set; }
        public SaleConsultant? SaleConsultant { get; set; }
    }

    public class SaleConsultant
    {
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? Name { get; set; }
    }

    public class ClaimRequest
    {
        public string Name { get; set; }
    }

    public class UpcomingPremiumList
    {
        public string? ProductName { get; set; }
        public string? ProductNameMM { get; set; }
        public string? ProductLogo { get; set; }
        public string? PolicyNumber { get; set; }        
        public double? PremiumDue { get; set; }
        public bool? IsUpcoming { get; set; }
        public bool? IsDued { get; set; }
        public int? NumberOfDaysForDue { get; set; }

        public DateTime? DueDate { get; set; }
    }

    public class UpcomingPremiumDetail
    {
        public string? ProductName { get; set; }
        public string? ProductNameMm { get; set; }
        public string? ProductLogo { get; set; }
        public string? PolicyNumber { get; set; }
        public double? PremiumDue { get; set; }
        public bool? IsUpcoming { get; set; }
        public bool? IsDued { get; set; }
        public int? NumberOfDaysForDue { get; set; }
        public DateTime? DueDate { get; set; }
        public string? InsuredName { get; set; }
        public string? InsuredId { get; set; }
        public string? InsuredNrc { get; set; }
        public string? SaleConsultantPhone { get; set; }
        public PolicyAgentInfo? agentInfo { get; set; }
    }

    public class PropositionsCategoryResponse
    {
        public string ID {get;set;}
        public string Name_EN {get;set;}
        public string Name_MM {get;set;}
        public string BackgroudImage{get;set;}
        public string IconImage {get;set;}
        public int TotalBenefits {get;set;}
        public int TotalPropositions { get;set;}

        public string Eligibility { get; set; }
        public DateTime? CreatedOn { get; set; }
    }

    public class PropositionsResponse
    {
        public string ID {get;set;}
        public string Name_EN {get;set;}
        public string Name_MM {get;set;}
        public string BackgroudImage{get;set;}
        public string LogoImage {get;set;}
        public int TotalBenefits {get;set;}
        public int? Sort { get; set; }
        public string Eligibility { get; set; }
        public string Type { get; set; }
        public int TotalPropositions { get; set; }
    }

    public class PromotionResponse
    {
        public string ID {get;set;}
        public string Title_EN { get; set; }
        public string Title_MM { get; set; }
        public string Topic_EN {get;set;}
        public string Topic_MM { get; set; }
        public string ReadMin_EN {get;set;}
        public string ReadMin_MM {get;set;}
        public string CoverImage {get;set;}
        public DateTime? PromotionEnd { get; set; }
        public string? CategoryType { get; set; }        
    }

    public class ProductResponse
    {
        public string ID {get;set;} 
        public string ProductName_EN {get;set;}
        public string ProductName_MM {get;set;}
        public string TagLine_EN {get;set;}
        public string TagLine_MM {get;set;}
        public string IssuedAgeFrom_EN {get;set;}
        public string IssuedAgeFrom_MM {get;set;}
        public string IssuedAgeEnd_EN {get;set;}
        public string IssuedAgeEnd_MM {get;set;}

        public string? IconImage { get; set; }
        public string? LogoImage { get; set; }

        public string? CoverImage { get; set; }

        public string? ProductCode  { get; set; }
    }


    public class HomeRecentRequestResponse
    {  
        public string? RequestType { get; set; }
        public string? ClaimType { get; set; }
        public string? ServiceType { get; set; }
        public Guid? RequestId { get; set; }
        public string? RequestNameEn { get; set; }
        public string? RequestNameMm { get; set; }
        public string? PolicyNumber { get; set; }
        public string? Status { get; set; }

        [JsonIgnore]
        public string? RequestType2 { get; set; }
    }
}
