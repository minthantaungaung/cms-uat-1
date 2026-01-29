using aia_core.Entities;
using DocumentFormat.OpenXml.Vml.Office;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace aia_core.Model.Cms.Response
{
    public class ClaimCount
    {
        public long SelectCount { get; set; }
    }

    public class ClaimResponse
    {
        public Guid? MainClaimId { get; set; }
        public Guid? ClaimId { get; set; }
        public string? MemberName { get; set; }
        public string? ClientNo { get; set; }
        public string? GroupClientNo { get; set; }
        public string? MemberType { get; set; }
        public string? MemberPhone { get; set; }
        public string? PolicyNo { get; set; }
        public string? ClaimType { get; set; }
        public string? ClaimStatus { get; set; }
        public string? ClaimStatusCode { get; set; }
        public string? RemainingHour { get; set; }
        public string? ILStatus { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDt { get; set; }
        public DateTime? TranDate { get; set; }
        public string? ProductType { get; set; }

        public DateTime? CreatedDt { get; set; }

        public Guid? AppMemberId { get; set; }

        public Guid? DiagnosisId { get; set; }
        public string? DiagnosisName { get; set; }

        public string? ClaimFormType { get; set; }

        public string? DiagnosisNameEn { get; set; }
        public string? CausedByNameEn { get; set; }

        //ClaimFormType


        public ClaimResponse() { }
        public ClaimResponse(Entities.ClaimTran entity)
        {
            MainClaimId = entity.MainId;
            ClaimId = entity.ClaimId;
            MemberName = entity.Client?.Name;
            ClientNo = entity.HolderClientNo;
            GroupClientNo = "NotImpl";
            MemberType = "NotImpl";
            MemberPhone = entity.Client?.PhoneNo;
            PolicyNo = entity.PolicyNo;
            ClaimType = entity.ClaimType;
            //ProductType = entity.Product?.TitleEn;
            //ClaimStatus = entity.Claim.Status;
            ILStatus = entity.Ilstatus;
            UpdatedBy = entity.UpdatedBy;
            UpdatedDt = entity.UpdatedOn;
            TranDate = entity.TransactionDate;
        }

    }

    public class ClaimDetailResponse
    {
        public ClaimHeader? ClaimHeader { get; set; }

        public ClaimCommon? ClaimCommon { get; set; }

        public ClaimRequestDetail? ClaimRequestDetail { get; set; }
        public PolicyOwnerDetail? PolicyOwnerDetail { get; set; }
        public InsuredPersonDetail? InsuredPersonDetail { get; set; }

        public bool? HasFollowupData { get; set; } = false;
        public FollowupData? FollowupData { get; set; }

        public List<MedicalBillClaimProcessResponse>? MedicalBillClaimProcess { get; set; }
    }

    public class ClaimRequestDetail
    {
        public string? ClaimType { get; set; }
        public string? PolicyNo { get; set; }
        public string? ProductType { get; set; }
        public string? RemainingHour { get; set; }
        public string? ClaimStatus { get; set; }
        public string? ClaimStatusCode { get; set; }
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? BankAcc { get; set; }
        public string? BankAccName { get; set; }
        public decimal? EligibleAmount { get; set; }
        public string? ILStatus { get; set; }
        public string? ILErrMessage { get; set; }

        public string? ClaimDocPolicyHolderName { get; set; }
        public ClaimDocs[]? ClaimDocs { get; set; }
        public ImagingLogError[]? ImagingLogErrors { get; set; }
        public ClaimSummary? ClaimSummary { get; set; }
        public IncurredDetail? IncurredDetail { get; set; }

        public string? Reason { get; set; }

        public string? ILRemark { get; set; }
    }

    public class FollowupData
    {
        public string? PolicyHolderName { get; set; }
        public string? RequiredInfo { get; set; }
        public List<string> AttachedFiles  { get; set; }

        public List<Doc2>? AttachedFiles2 { get; set; }
    }
    public class ClaimDocs
    {
        [JsonIgnore]
        public string? Type { get; set; }
        public string? TypeName { get; set; }

        [JsonIgnore]
        public string? Doc { get; set; }
        [JsonIgnore]
        public string? DocName2 { get; set; }
        public string[]? Docs { get; set; }

        public List<Doc2>? Docs2 { get; set; }
    }

    public class Doc2
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
    }
    public class ClaimHeader
    {
        public string? PolicyNo { get; set; }
        public string? ClaimType { get; set; }
        public string? ClaimStatus { get; set; }
        public string? ClaimStatusCode { get; set; }
        public string? ClaimBy { get; set; }

        public string? ILRemark { get; set; }
    }

    public class ClaimCommon
    {
        

    }
    public class ImagingLogError
    {
        public string? DocName { get; set; }
        public string? message { get; set; }
    }
    public class IncurredDetail
    {
        public string? Hospital { get; set; }
        public string? Location { get; set; }
        public string? Doctor { get; set; }
        public string? Summary { get; set; }
    }
    public class ClaimSummary
    {
        public string? ProductCode { get; set; }
        public EnumBenefitFormType? ClaimFormType { get; set; }
        public string? InsuredPerson { get; set; }
        public string? Diagnosis { get; set; }
        public int? HospitalVisits { get; set; }
        
        public string? CausedByType { get; set; }
        public Guid? CausedById { get; set; }
        public string? CausedByNameEn { get; set; }
        public string? CausedByNameMm { get; set; }
        public string? CausedByCode { get; set; }
        public DateTime? CausedByDate { get; set; }

        public string[]? TreatmentDates { get; set; }
        public DateTime? TreatmentFromDate { get; set; }
        public DateTime? TreatmentToDate { get; set; }
        public decimal? IncurredAmount { get; set; }

        public virtual ICollection<ClaimBenefit> ClaimBenefits { get; set; } = new List<ClaimBenefit>();
        #region #Common

        public string? ClaimForPolicyNo { get; set; }
        public string? ClaimantName { get; set; }
        public DateTime? ClaimantDob { get; set; }
        public string? ClaimantGender { get; set; }
        public string? ClaimantRelationship { get; set; }
        public string? ClaimantRelationshipMm { get; set; }
        public string? ClaimantEmail { get; set; }
        public string? ClaimantPhone { get; set; }
        public string? ClaimantAddress { get; set; }
        public string? ClaimantIdenType { get; set; }
        public string? ClaimantIdenValue { get; set; }

        #endregion
    }

    public class PolicyOwnerDetail
    {
        public string? Name { get; set; }
        public string? IdValue { get; set; }
        public string? ClientNo { get; set; }
        public string? Married { get; set; }
        public string? Dob { get; set; }
        public string? Gender { get; set; }
        public string? Father { get; set; }
        public string? Occupation { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class InsuredPersonDetail
    {
        public string? Name { get; set; }
        public string? IdValue { get; set; }
        public string? ClientNo { get; set; }
        public string? Married { get; set; }
        public string? Dob { get; set; }
        public string? Gender { get; set; }
        public string? Father { get; set; }
        public string? Occupation { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class FailedLogResponse
    {
        public Guid? MainClaimId { get; set; }
        public Guid? ClaimId { get; set; }
        public string? MemberName { get; set; }
        public string? ClientNo { get; set; }
        public string? GroupClientNo { get; set; }
        public string? MemberType { get; set; }
        public string? MemberPhone { get; set; }
        public string? PolicyNo { get; set; }
        public string? ClaimType { get; set; }
        public string? ClaimStatus { get; set; }
        public string? ClaimStatusCode { get; set; }
        public DateTime? TranDate { get; set; }
        public string? ProductType { get; set; }
        public string? Code { get; set; }
        public string? Message { get; set; }

        public string? ILRequest { get; set; }
        public string? ILResponse { get; set; }

        public string? FERequest { get; set; }

        public FailedLogResponse() { }
        public FailedLogResponse(Entities.ClaimTran entity)
        {
            MainClaimId = entity.MainId;
            ClaimId = entity.ClaimId;
            ClientNo = entity.IndividualMemberID;
            GroupClientNo = entity.GroupMemberID;
            MemberPhone = entity.MemberPhone;
            PolicyNo = entity.PolicyNo;
            ClaimType = entity.ClaimType;
            Code = entity.IlerrorMessage;
            Message = entity.IlerrorMessage;
            ProductType = "";
            TranDate = entity.TransactionDate;
            ILRequest = entity.Ilrequest;
            ILResponse = entity.Ilresponse;
            FERequest = entity.Ferequest;
        }
    }

    public class CrmFailedLogResponse
    {
        public Guid? MainClaimId { get; set; }
        public Guid? ClaimId { get; set; }
        public string? MemberName { get; set; }
        public string? ClientNo { get; set; }
        public string? GroupClientNo { get; set; }
        public string? MemberType { get; set; }
        public string? MemberPhone { get; set; }
        public string? PolicyNo { get; set; }
        public string? ClaimType { get; set; }
        public string? ClaimStatus { get; set; }
        public string? ClaimStatusCode { get; set; }
        public DateTime? TranDate { get; set; }
        public string? ProductType { get; set; }
        public string? Code { get; set; }
        public string? Message { get; set; }

        public string? CrmRequest { get; set; }
        public string? CrmResponse { get; set; }

        public string? CrmStatus { get; set; }

        public CrmFailedLogResponse() { }
        public CrmFailedLogResponse(Entities.ClaimTran entity)
        {
            MainClaimId = entity.MainId;
            ClaimId = entity.ClaimId;
            ClientNo = entity.HolderClientNo;
            GroupClientNo = "";
            MemberPhone = entity.MemberPhone;
            PolicyNo = entity.PolicyNo;
            ClaimType = entity.ClaimType;
            ProductType = "";
            TranDate = entity.TransactionDate;
            CrmRequest = entity.CrmRequest;
            CrmResponse = entity.CrmResponse;

            CrmStatus = "failed";
            Message = entity.CrmResponse;
            Code = "failed";

            if (!string.IsNullOrEmpty(entity.CrmResponse))
            {
                if (entity.CrmResponse.Contains("200"))
                {
                    CrmStatus = "success";
                    Message = entity.CrmResponse;
                    Code = "success";
                }
            }
        }
    }
    public class ImagingLogResponse
    {
        public Guid? MainClaimId { get; set; }
        public Guid? ClaimId { get; set; }
        public Guid? UploadId { get; set; }
        public string? PolicyNo { get; set; }
        public string? ClaimType { get; set; }
        public DateTime? TranDate { get; set; }
        public string? ProductType { get; set; }
        public string? Code { get; set; }
        public string? Message { get; set; }
        public string? DocName { get; set; }
        public string? DocTypeName { get; set; }
        public string? FormID { get; set; }
        public ImagingLogResponse() { }
        public ImagingLogResponse(Entities.ClaimDocument entity)
        {
            
        }
    }

    public class ImagePolicyDetail
    {
        public string? ClaimId { get; set; }
        public string? ProductType { get; set; }
        public string? InsurredId { get; set; }
        public string? InsurredName { get; set; }        
        public string? HolderId { get; set; }
        public string? HolderName { get; set; }
        public string? PaymentFrequency { get; set; }
        public string? PolicyStatus { get; set; }
        public DateTime? SinceDate { get; set; }
        public DateTime? CommenceDate { get; set; }
        public string? Components { get; set; }
    }

    public class ImageRequestData
    {
        public string? PhoneNo { get; set; }
        public string? ClaimType { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? TreatmentDate { get; set; }
        public DateTime? ClaimDate { get; set; }
    }

    public class ImageLogDetail
    {
        public ImagePolicyDetail? ImagePolicyDetail { get; set; }
        public ImageRequestData? ImageRequestData { get; set; }
    }

    public class ClaimStatusResp
    {
        public EnumClaimStatus? Code { get; set; }
        public string? Message { get; set; }
    }

    public class CrmSuccessResponse
    {
        public string Code { get; set; }
        public object Message { get; set; }
        public string Data { get; set; }
    }


    public class ClaimValidateMessageResponse
    {
        public Guid? ID { get; set; }
        public DateTime? Date { get; set; }
        public string? PolicyNumber { get; set; }
        public string? MemberID { get; set; }
        public string? MemberName { get; set; }
        public string? MemberPhone { get; set; }
        public string? Message { get; set; }
        public string? ClaimType { get; set; }
    }

    public class MedicalBillClaimProcessResponse
    {
        public Guid? Id { get; set; }
        public Guid? claimId { get; set; }
        public string? admissionDate { get; set; }
        public string? billType { get; set; }
        public string? billingDate { get; set; }
        public string? dischargeDate { get; set; }
        public string? doctorName { get; set; }
        public string? hospitalName { get; set; }
        public string? netAmount { get; set; }
        public string? patientName { get; set; }
        public string? fileName { get; set; }
        public string? response { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string? status { get; set; }
    }
}
