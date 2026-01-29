using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace aia_core.Model.Cms.Response.ServicingDetail
{
    #region #Service Detail
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Attachment
    {
        public string? fileName { get; set; }
        public string? errorMessage { get; set; }
        public string? fileUrl { get; set; }
        public string? FormId { get; set; }
    }

    public class Bank
    {
        public string? bankCode { get; set; }
        public string? bankName { get; set; }
        public string? bankAccountName { get; set; }
        public string? bankAccountNumber { get; set; }
    }

    public class Beneficiary
    {
        public List<ProfileUpdate>? profileUpdate { get; set; }
        public List<NewBeneficiary>? newBeneficiaries { get; set; }
        public List<BeneficiaryShare>? beneficiaryShare { get; set; }
    }

    public class BeneficiaryShare
    {
        public string? name { get; set; }
        public string? oldRelationship { get; set; }
        public string? oldPercentage { get; set; }
        public string? newRelationship { get; set; }
        public string? newPercentage { get; set; }
        public string? beneficiaryType { get; set; }
    }

    public class General
    {
        public string? label { get; set; }
        public string? oldInformation { get; set; }
        public string? newInformation { get; set; }
    }

    public class NewBeneficiary
    {
        public string? name { get; set; }
        public string? gender { get; set; }
        public string? mobileNo { get; set; }
        public string? dob { get; set; }
        public string? idType { get; set; }
        public string? idValue { get; set; }
        public string? idFrontImage { get; set; }
        public string? idBackImage { get; set; }
    }

    public class ProfileUpdate
    {
        public string? label { get; set; }
        public string? oldInformation { get; set; }
        public string? newInformation { get; set; }
        public string? idValue { get; set; }
        public string? idType { get; set; }
        public string? Name { get; set; }
    }

    public class RequestSummary
    {
        public string? submissionDate { get; set; }
        public string? amount { get; set; }
        public string? reasonOfRequest { get; set; }
        public string? remark { get; set; }
        public List<General>? general { get; set; }
        public Beneficiary? beneficiary { get; set; }
    }

    public class ServiceDetailResponse
    {
        public string? serviceName { get; set; }
        public string? serviceType { get; set; }
        public string? serviceStatus { get; set; }

        public List<string>? serviceStatusList { get; set; }
        public string? internalRemark { get; set; }
        public string? memberName { get; set; }
        public string? memberId { get; set; }
        public string? policyNumber { get; set; }
        public string? productType { get; set; }
        public string? policyHolderName { get; set; }
        public string? policyHolderClientNo { get; set; }
        public string? policyStatus { get; set; }
        public DateTime? paidToDate { get; set; }
        public DateTime? policyIssueDate { get; set; }
        public string? remainingTime { get; set; }
        public string? signatureImage { get; set; }
        public string? ilStatus { get; set; }
        public string? ilErrorMessage { get; set; }
        public Bank? bank { get; set; }
        public List<Attachment>? attachments { get; set; }
        public RequestSummary? requestSummary { get; set; }
    }



    public class BankCommon
    {
        public string? ServiceType { get; set; }
        public Guid? ServicingId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? ServiceStatus { get; set; }
        public double? Amount { get; set; }
        public string? Reason { get; set; }
        public string? BankCode { get; set; }
        public string? BankName { get; set; }
        public string? AccountName { get; set; }
        public string? AccountNumber { get; set; }
    }
    #endregion



    #region #ImagineLogError
    public class ServiceImagingLogResponse
    {
        public Guid? MainServiceId { get; set; }
        public Guid? ServiceId { get; set; }

        [JsonIgnore]
        public string? ServiceTypeEnum { get; set; }

        
        public string? ServiceType { get; set; }
        public DateTime? TranDate { get; set; }
        public string? Code { get; set; }
        public string? Message { get; set; }
        public string? DocName { get; set; }
        public string? PolicyNumber { get; set; }
        public string? MemberId { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FormID { get; set; }
    }
    #endregion


    #region #FailedLog
    public class ServiceFailedLogResponse
    {
        public Guid? Id { get; set; }
        public string? ServiceType { get; set; }
        public string? MemberId { get; set; }
        public string? PhoneNo { get; set; }
        public string? PolicyNumber { get; set; }
        public DateTime? TranDate { get; set; }
        public string? Old { get; set; }
        public string? New { get; set; }
        public string? ErrorMessage { get; set; }
    }
    #endregion



    #region #FailedLogDetail
    public class FailedLogDetailResponse
    {
        public Guid? Id { get; set; }
        public string? PolicyNumber { get; set; }
        public string? ProductType { get; set; }
        public string? InsuredPersonClientNo { get; set; }
        public string? InsuredPersonClientName { get; set; }
        public string? PolicyHolderClientNo { get; set; }
        public string? PolicyHolderClientName { get; set; }
        public string? PolicyStatus { get; set; }
        public DateTime? PolicyIssuedDate { get; set; }
        public DateTime? MemberSinceDate { get; set; }
        public DateTime? OriginalCommenceDate { get; set; }
        public string? Components { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ServiceType { get; set; }
        public string? Olddata { get; set; }
        public string? Newdata { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? Date { get; set; }
    }
    #endregion
}
