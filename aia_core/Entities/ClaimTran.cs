using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ClaimTran
{
    public Guid ClaimId { get; set; }

    public string? ClaimCode { get; set; }
    public string? MainClaimCode { get; set; }
    public string? ProductType { get; set; }
    public string? ProductNameEn { get; set; }
    public string? PolicyNo { get; set; }

    public string? ClaimFormType { get; set; }

    public string? ClaimType { get; set; }
    public string? ClaimTypeMm { get; set; }

    public Guid? CausedById { get; set; }

    public string? CausedByNameEn { get; set; }

    public string? CausedByNameMm { get; set; }
    public string? CausedByCode { get; set; }

    public DateTime? CausedByDate { get; set; }

    public string? BankNameEn { get; set; }
    public string? BankNameMm { get; set; }
    public string? BankCode { get; set; }

    public string? BankAccountNumber { get; set; }

    public string? BankAccountName { get; set; }

    public string? ClaimForPolicyNo { get; set; }

    public Guid? DiagnosisId { get; set; }

    public string? DiagnosisNameEn { get; set; }

    public string? DiagnosisNameMm { get; set; }
    public string? DiagnosisCode { get; set; }

    public Guid? HospitalId { get; set; }

    public string? HospitalNameEn { get; set; }

    public string? HospitalNameMm { get; set; }
    public string? HospitalCode { get; set; }

    public Guid? LocationId { get; set; }

    public string? LocationNameEn { get; set; }

    public string? LocationNameMm { get; set; }
    public string? LocationCode { get; set; }

    public string? DoctorName { get; set; }
    public string? DoctorId { get; set; }
    public int? TreatmentCount { get; set; }

    public string? TreatmentDates { get; set; }

    public DateTime? TreatmentFromDate { get; set; }

    public DateTime? TreatmentToDate { get; set; }

    public decimal? IncurredAmount { get; set; }

    public string? IncidentSummary { get; set; }

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

    public string? HolderClientNo { get; set; }

    public string? InsuredClientNo { get; set; }

    public string? ClaimStatus { get; set; }

    public string? ClaimStatusCode { get; set; }

    public string? RemainingTime { get; set; }

    public string? Ilstatus { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedOn { get; set; }

    public string? CausedByType { get; set; }

    public Guid? MainId { get; set; }

    public string? IlerrorMessage { get; set; }

    public DateTime? FerequestOn { get; set; }

    public DateTime? FeresponseOn { get; set; }

    public string? Ilrequest { get; set; }

    public string? Ilresponse { get; set; }

    public DateTime? IlrequestOn { get; set; }

    public DateTime? IlresponseOn { get; set; }

    public string? Ferequest { get; set; }

    public string? Feresponse { get; set; }


    public string? BankAccHolderIdValue { get; set; }

    public DateTime? BankAccHolderDob { get; set; }

    public string? CrmRequest { get; set; }

    public DateTime? CrmRequestOn { get; set; }
    public string? CrmResponse { get; set; }

    public string? IndividualClaimType { get; set; }

    public DateTime? CrmResponseOn { get; set; }

    public DateTime? CreatedDate  { get; set; }

    public DateTime? TransactionDate { get; set; }

    public string? SignatureImage { get; set; }

    public string? EligibleComponents { get; set; }


    public decimal? EligibleAmount { get; set; }

    public int? ProgressAsPercent { get; set; }
    public string? ProgressAsHours { get; set; }

    public DateTime? EstimatedCompletedDate { get; set; }

    public Guid? AppMemberId { get; set; }

    public string? MemberType { get; set; }

    public string? GroupMemberID { get; set; }

    public string? IndividualMemberID { get; set; }

    public string? MemberName { get; set; }

    public string? MemberPhone { get; set; }

    public bool? SentSms { get; set; }
    public DateTime? SentSmsAt { get; set; }

    public virtual ICollection<ClaimBenefit> ClaimBenefits { get; set; } = new List<ClaimBenefit>();

    public virtual ICollection<ClaimDocument> ClaimDocuments { get; set; } = new List<ClaimDocument>();

    public virtual Client? Client { get; set; }

    //public virtual Claim? Claim { get; set; }
}
