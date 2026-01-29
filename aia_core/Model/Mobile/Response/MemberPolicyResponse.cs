using aia_core.Converter;
using aia_core.Entities;
using aia_core.Model.Cms.Request;
using CsvHelper;
using Newtonsoft.Json;
using System.Reflection;

namespace aia_core.Model.Mobile.Response.MemberPolicyResponse
{
    #region MemberPolicyResponse
    public class Policies
    {
        public string? InsuredName { get; set; }
        public string? InsuredId { get; set; }
        public string? InsuredNrc { get; set; }
        public List<InsuredPolicies>? InsuredPolicies { get; set; }
    }

    public class InsuredPolicies
    {
        internal string? InsuredName { get; set; }
        internal string? InsuredId { get; set; }
        internal string? InsuredNrc { get; set; }
        public string? PolicyNumber { get; set; }
        public DateTime? PolicyDate { get; set; }
        public string? PolicyUnits { get; set; }
        public double? SumAssured { get; set; }
        public double? PremiumDue { get; set; }
        public string? ProductName { get; set; }
        public string? ProductNameMM { get; set; }
        public string? ProductLogo { get; set; }
        public int? NumberOfDaysForDue { get; set; }
        public bool? IsPolicyActive { get; set; }
        public bool? IsDued { get; set; }        
        public bool? IsUpcoming { get; set; }
        public string? PolicyStatus { get; set; }

        public string? SumAssuredByUnitOrAmt { get; set;}

        public string? ProductStatus { get; set; } //NEW
        public string? productStatusMM { get; set; } //NEW

        public DateTime? PremiumDueDate { get; set; }
        public string? Premium { get; set; }
        public string? PlanName { get; set; }
        public string? ProductCode { get; set; }

        public bool IsCOASTPolicy { get; set; }
    }

    public class MemberPolicyResponse
    {
        public List<Policies>? ActivePolicies { get; set; }
        public List<Policies>? InactivePolicies { get; set; }

    }

    #endregion

    #region PolicyDetailResponse
    public class PolicyDetailResponse
    {
        public string? insuredName { get; set; }
        public string? InsuredNrc { get; set; }
        public string? policyNumber { get; set; }
        public string? policyName { get; set; }
        public string? policyNameMM { get; set; }
        public string? PolicyStatus { get; set; }
        public int? NumberOfDaysForDue { get; set; }
        public bool? IsDued { get; set; }
        public bool? IsUpcoming { get; set; }
        public string? ProductLogo { get; set; }
        public double? PremiumDue { get; set; }

        public string? ProductStatus { get; set; } //NEW
        public string? productStatusMM { get; set; } //NEW

        public PolicyInfo? PolicyInfo { get; set; }
        public PolicyHolderInfo? PolicyHolderInfo { get; set; }
        public InsuredInfo? InsuredInfo { get; set; }
        public List<BeneficiaryInfo>? BeneficiaryInfo { get; set; }
        public PaymentInfo? PaymentInfo { get; set; }
        public List<PolicyDocuments>? PolicyDocuments { get; set; }

        public string? PolicyUnits { get; set; }
        public double? SumAssured { get; set; }

        public string? SumAssuredByUnitOrAmt { get; set; }

        public string? DBDataLog { get; set; }
    }

    public class PolicyInfo
    {
        public string? PolicyNumber { get; set; }
        public DateTime? PolicyDate { get; set; }
        public string? PolicyStatus { get; set; }
        public string? PolicyHolder { get; set; }
        public string? PolicyUnits { get; set; }
        public double? SumAssured { get; set; }
        public DateTime? PremiumDueDate { get; set; }
        public string? PaymentFrequency { get; set; }
        public bool? PolicyACP { get; set; }
        public double? OutstandingPremium { get; set; }
        public double? OutstandingInterest { get; set; }
        //public string? AgentName { get; set; }
        //public string? AgentEmail { get; set; }
        //public string? AgentPhone { get; set; }

        public string? PlanName { get; set; }
        public string? ProductCode { get; set; }

        public PolicyAgentInfo? agentInfo { get; set; }
        public PolicyLoanDetail? PolicyLoanDetail { get; set; }
        public PremiumDueAmountDetail? PremiumDueAmountDetail { get; set; }
        public LapseReinstatementDetail? LapseReinstatementDetail { get; set; }
        public ACPDetail? ACPDetail { get; set; }

        public RenewalDetail? RenewalDetail { get; set; }

        public bool IsCoastPolicy   { get; set; }
    }

    public class PolicyLoanDetail
    {
        public string? InfoText { get; set; }
        public string? InfoTextMm { get; set; }
        public string? OutstandingPolicyLoan { get; set; }
        public string? LoanInterest { get; set; }
    }

    public class PremiumDueAmountDetail
    {
        public string? InfoText { get; set; }
        public string? InfoTextMm { get; set; }
        public string? PremiumDue { get; set; }
    }

    public class RenewalDetail
    {
        public string? InfoText { get; set; }
        public string? InfoTextMm { get; set; }
        public string? RenewalAmount { get; set; }
    }

    public class LapseReinstatementDetail
    {
        public string? InfoText { get; set; }
        public string? InfoTextMm { get; set; }
        public string? PremiumDue { get; set; }
        public string? Interest { get; set; }
    }
    public class ACPDetail
    {
        public string? InfoText { get; set; }
        public string? InfoTextMm { get; set; }
        public string? OutstandingACPPremium { get; set; }
        public string? OutstandingACPInterest { get; set; }
    }

    public class PolicyAgentInfo
    {
        public string? AgentName { get; set; }
        public string? AgentEmail { get; set; }
        public string? AgentPhone { get; set; }
        public string? AgentNrc { get; set; }
        public string? AgentMarriedStatus { get; set; }
        public string? AgentGender { get; set; }
        public string? AgentFather { get; set; }
        public string? AgentOccupation { get; set; }
        public string? AgentAddress { get; set; }
    }

    public class PolicyHolderInfo
    {
        public string? PolicyHolder { get; set; }
        public string? PolicyHolderNrc { get; set; }
        public string? PolicyHolderMarriedStatus { get; set; }
        public string? PolicyHolderGender { get; set; }
        public string? PolicyHolderFather { get; set; }
        public string? PolicyHolderOccupation { get; set; }
        public string? PolicyHolderPhone { get; set; }
        public string? PolicyHolderEmail { get; set; }
        public string? PolicyHolderAddress { get; set; }
    }

    public class InsuredInfo
    {
        public string? InsuredGender { get; set; }
        public string? InsuredNrc { get; set; }
        public string? InsuredMarriedStatus { get; set; }
        public string? InsuredFather { get; set; }
        public string? InsuredOccupation { get; set; }
        public string? InsuredPhone { get; set; }
        public string? InsuredEmail { get; set; }
        public string? InsuredAddress { get; set; }
    }

    public class BeneficiaryInfo
    {
        public string? BeneficiaryName { get; set; }
        public string? BeneficiaryNrc { get; set; }
        public DateTime? BeneficiaryDob { get; set; }
        public string? BeneficiaryRelationship { get; set; }
        public string? BeneficiaryPhone { get; set; }
        public string? BeneficiaryEmail { get; set; }
        public string? BeneficiaryGender { get; set; }
        public int? BeneficiarySharedPercent { get; set; }
    }

    public class PaymentInfo
    {
        public string? PaymentFrequency { get; set; }
        public double? InstallmentPremium { get; set; }
    }

    public class PolicyDocuments
    {
        public string? Document { get; set; }
    }
    #endregion


    public class PolicyCoveragesResponse
    {
        public Guid CoverageId { get; set; }

        public string? CoverageNameEn { get; set; }
        public string? CoverageNameMM { get; set; }

        public string? CoverageIcon { get; set; }

        public bool? IsCovered { get; set; }

        public PolicyCoveragesResponse()
        {

        }
    }

    public class OwnershipPolicy
    {
        
        public EnumServiceStatus? ServiceStatus { get; set; } = EnumServiceStatus.Received;
        public Guid? ServicingId { get; set; }
        public string? InsuredName { get; set; }
        public string? InsuredClientNo { get; set; }
        public string? InsuredNrc { get; set; }
        public string? ProductName { get; set; }
        public string? ProductNameMm { get; set; }
        public string? ProductLogo { get; set; }
        public string? PolicyNumber { get; set; }
        public DateTime? PolicyDate { get; set; }
        public string? PolicyUnits { get; set; }
        public decimal? SumAssured { get; set; }
        public string? SumAssuredByUnitOrAmt { get; set; }
        public string? PaymentFrequency { get; set; }
        public decimal? InstallmentAmount { get; set; }
        public List<PaymentFrequency> PaymentFrequencies { get; set; } =
            new List<PaymentFrequency>
            {
                new PaymentFrequency { FrequencyCode = "12", FrequencyName = "Monthly" , IsCurrent = false },
                new PaymentFrequency { FrequencyCode = "04", FrequencyName = "Quarterly" , IsCurrent = false },
                new PaymentFrequency { FrequencyCode = "02", FrequencyName = "Semi Annually" , IsCurrent = false },
                new PaymentFrequency { FrequencyCode = "01", FrequencyName = "Annually" , IsCurrent = false }
            };
        public List<BeneficiaryShare> beneficiaries { get; set; }
    }


    public class PaymentFrequency
    {
        public string? FrequencyCode { get; set; }
        public string? FrequencyName { get; set; }        
        public int FrequencyAmount { get; set; }
        public bool? IsCurrent { get; set; }
    }

    public class BeneficiaryShare
    {
        public string? BeneficiaryClientNo { get; set; }
        public string? BeneficiaryName { get; set; }
        public string? RelationshipName { get; set; }
        public int? SharePercent { get; set; }
        public string? Gender { get; set; }
        public string? Nrc { get; set; }
        public DateTime? Dob { get; set; }

        public SeparateCountryCodeModel? Phone { get; set; }
    }

    public class CheckPaymentFrequencyResponse
    {
        public bool? IsValid { get; set; } = true;

        public List<ValidateResultMessage>? ValidationMessageList { get; set; }
    }

    public class ValidateResultMessage
    {
        public string? MessageEn { get; set; }

        public string? MessageMm  { get; set; }
    }
}
