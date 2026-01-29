using aia_core.Entities;
using aia_core.Model.Cms.Request;
using System.Reflection;

namespace aia_core.Model.Cms.Response.MemberPolicyResponse
{
    #region MemberPolicyResponse
    public class AppRegistrationInfo
    {
        public string? MemberName { get; set; }
        public string? MemberId { get; set; }
        public string? MemberPhone { get; set; }
        public string? MemberEmail { get; set; }
        public string? MemberIdNrc { get; set; }
        public string? MemberIdPassport { get; set; }
        public string? MemberIdOther { get; set; }
        public bool? MemberIsActive { get; set; }
        public string? MemberType { get; set; }
        public string? MemberGender { get; set; }
        public DateTime? MemberDob { get; set; }
        public string? MemberIndCop { get; set; }
        public string? GroupMemberID { get; set; }

        public string? IndividualMemberID { get; set; }

        public DateTime? RegisterDate { get; set; }
        public DateTime? LastActiveDate { get; set; }

        public AppRegistrationInfo() { }
        public AppRegistrationInfo(Entities.Member entity)
        {
            
            MemberName = entity.Name;
            MemberEmail = entity.Email;
            MemberPhone = entity.Mobile;
            MemberIdNrc = entity.Nrc;
            MemberIdPassport = entity.Passport;
            MemberIdOther = entity.Others;
            MemberIsActive = entity.IsActive;
            MemberDob = entity.Dob;
            MemberGender = entity.Gender;
            RegisterDate = entity.RegisterDate;
            LastActiveDate = entity.LastActiveDate;

            

        }

        public AppRegistrationInfo(Entities.Client entity)
        {
            MemberId = entity.ClientNo;
            MemberName = entity.Name;
            MemberEmail = entity.Email;
            MemberPhone = entity.PhoneNo;
            MemberIdNrc = entity.Nrc;
            MemberIdPassport = entity.PassportNo;
            MemberIdOther = entity.Other;
            MemberIsActive = true;
            MemberDob = entity.Dob;
            MemberGender = Utils.GetGender(entity.Gender);
            RegisterDate = entity.CreatedDate;
            LastActiveDate = Utils.GetDefaultDate();

        }
    }

    public class Policies
    {
        public string? InsuredName { get; set; }
        public string? InsuredId { get; set; }
        public List<InsuredPolicies>? InsuredPolicies { get; set; }
    }

    public class InsuredPolicies
    {
        internal string? InsuredName { get; set; }
        internal string? InsuredId { get; set; }
        public string? PolicyId { get; set; }
        public DateTime? PolicyDate { get; set; }
        public string? PolicyUnits { get; set; }
        public double? SumAssured { get; set; }
        public string? ProductName { get; set; }

        public string? ProductLogo { get; set; }
        public int? NumberOfDaysForDue { get; set; }
        public bool? IsPolicyActive { get; set; }

        public string? ProductCode { get; set; }
        public string? ComponentCodes { get; set; }

        public bool? IsDued
        {
            get
            {
                if (NumberOfDaysForDue < 0 /*&& NumberOfDaysForDue > -5*/ )
                {
                    return true;
                }

                return false;
            }

            set { }
        }
        public bool? IsUpcoming
        {
            get
            {
                if (NumberOfDaysForDue > 0 /*&& NumberOfDaysForDue <= 5*/ )
                {
                    return true;
                }

                return false;
            }

            set { }
        }
    }


    public class MemberPolicyResponse
    {
        public AppRegistrationInfo? AppRegistrationInfo { get; set; }
        public List<Policies>? Policies { get; set; }

    }

    #endregion

    #region PolicyDetailResponse
    public class PolicyDetailResponse
    {
        public string? insuredName { get; set; }
        public string? policyNumber { get; set; }
        public string? policyName { get; set; }
        public PolicyInfo? PolicyInfo { get; set; }
        public PolicyHolderInfo? PolicyHolderInfo { get; set; }
        public InsuredInfo? InsuredInfo { get; set; }
        public List<BeneficiaryInfo>? BeneficiaryInfo { get; set; }
        public PaymentInfo? PaymentInfo { get; set; }
        public List<PolicyDocuments>? PolicyDocuments { get; set; }
    }

    public class PolicyInfo
    {
        public string? PolicyNumber { get; set; }
        public string? PolicyStatus { get; set; }
        public string? PolicyHolder { get; set; }
        public string? PolicyUnits { get; set; }
        public double? SumAssured { get; set; }
        public DateTime? PremiumDueDate { get; set; }
        public string? PaymentFrequency { get; set; }
        public string? PolicyACP { get; set; }
        public double? OutstandingPremium { get; set; }
        public double? OutstandingInterest { get; set; }
        public string? AgentName { get; set; }
        public string? AgentEmail { get; set; }
        public string? AgentPhone { get; set; }
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

        public string? CoverageIcon { get; set; }

        public bool? IsCovered { get; set; }

        public PolicyCoveragesResponse()
        {

        }
    }

    public class MemberPolicyListResponse
    {
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }

        public string? MemberPhone { get; set; }

        public string? MemberEmail { get; set; }

        public string? MemberIdNrc { get; set; }

        public string? MemberIdPassport { get; set; }

        public string? MemberIdOther { get; set; }

        public bool? MemberIsActive { get; set; }

        public string? MemberType { get; set; }

        public string? MemberGender { get; set; }

        public DateTime? MemberDob { get; set; }

        public string? MemberIndCop { get; set; }

        public string? IndividualMemberID { get; set; }

        public string? GroupMemberID { get; set; }

        public DateTime? RegisterDate { get; set; }

        public DateTime? LastActiveDate { get; set; }

        public MemberPolicyListResponse() { }
        public MemberPolicyListResponse(Entities.ClientCorporate entity)
        {
            MemberId = entity.ClientNo;
            MemberName = entity.Name;
            MemberEmail = entity.Email;
            MemberPhone = entity.PhoneNo;
            MemberIdNrc = entity.Nrc;
            MemberIdPassport = entity.PassportNo;
            MemberIdOther = entity.Other;
            MemberIsActive = true;
            MemberDob = entity.Dob;

            MemberGender = entity.Gender;
            RegisterDate = entity.CreatedDate;
            LastActiveDate = Utils.GetDefaultDate();

            MemberType = entity.MemberTierType;
            GroupMemberID = entity.CorporateClientNo;
            MemberIndCop = entity.MemberType;
        }


        public MemberPolicyListResponse(Entities.Member entity)
        {
            if (entity != null)
            {
                MemberName = entity?.Name;
                MemberEmail = entity?.Email;
                MemberPhone = entity?.Mobile;
                MemberIdNrc = entity?.Nrc;
                MemberIdPassport = entity?.Passport;
                MemberIdOther = entity?.Others;
                MemberIsActive = true;
                MemberDob = entity?.Dob;

                MemberGender = entity?.Gender;
                RegisterDate = entity?.RegisterDate;
                LastActiveDate = Utils.GetDefaultDate();


                if (entity.MemberClients != null && entity.MemberClients.Any())
                {
                    MemberId = entity.MemberClients.First().ClientNo;

                    MemberType = entity.MemberClients.Any(x => x.Client.VipFlag == "Y")
                        ? EnumIndividualMemberType.Ruby.ToString() : EnumIndividualMemberType.Member.ToString();
                }
            }

        }
    }
}

    
