using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Request
{
    #region #Common
    public class Claiment
    {
        public string clientNumber { get; set; }
        public string dob { get; set; }
        public string email { get; set; }
        public string gender { get; set; }
        public string idnumber { get; set; }
        public string idtype { get; set; }
        public string name { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string address4 { get; set; }
        public string address5 { get; set; }
        public string country { get; set; }
        public string phone { get; set; }
        public string townshipCode { get; set; }
    }

    public class Payee
    {
        public string bankNameCode { get; set; }

        public string bankAccountNumber { get; set; }
        public string bankAccountName { get; set; }
    }
    #endregion


    #region SeparateRequests
    #region #TPDRegister
    public class TPDRegisterRequest
    {
        public string claimId { get; set; }
        public string policyNumber { get; set; }
        public string effectiveDate { get; set; }
        public string dateOfDisability { get; set; }
        public string causeOfDisability { get; set; }
        public string gratiaIndicator { get; set; }
        public string interimCoverage { get; set; }
        public string disallowITC { get; set; }
        public string soleProprietor { get; set; }
        public string purposeOrPersonal { get; set; }
        public string sourceNotification { get; set; }
        public string relationship { get; set; }
        public string claimLocation { get; set; }
        public Claiment claiment { get; set; }
        public string issuAgeVerified { get; set; }
    }

    #endregion

    #region #DeathRegister

    public class DeathRegisterRequest
    {
        public string claimId { get; set; }
        public string policyNumber { get; set; }
        public string effectiveDate { get; set; }
        public string dateOfDisability { get; set; }
        public string causeOfDisability { get; set; }
        public string gratiaIndicator { get; set; }
        public string interimCoverage { get; set; }
        public string disallowITC { get; set; }
        public string soleProprietor { get; set; }
        public string purposeOrPersonal { get; set; }
        public string sourceNotification { get; set; }
        public string relationship { get; set; }
        public string claimLocation { get; set; }
        public Claiment claiment { get; set; }
        public string issuAgeVerified { get; set; }
    }
    #endregion

    #region #NonHealthRegister
    

    public class NonHealthRegisterRequest
    {
        public string claimId { get; set; }
        public string policyNumber { get; set; }
        public string claimType { get; set; }
        public string componentID { get; set; }
        public string receiveDate { get; set; }
        public string incurredDate { get; set; }
        public string claimReason { get; set; }
        public string sourceNotification { get; set; }
        public string gratiaIndicator { get; set; }
        public string interimCoverage { get; set; }
        public string disallowITC { get; set; }
        public string soleProprietor { get; set; }
        public string purposeOrPersonal { get; set; }
        public string claimentOwnerAccount { get; set; }
        public string claimLocation { get; set; }
        public Payee payee { get; set; }
    }
    #endregion

    #region #HealthRegister
    public class Benefit
    {
        public string benefitName { get; set; }
        public string benefitCode { get; set; }
        public string dateFrom { get; set; }
        public string dateTo { get; set; }
        public string incurredAmount { get; set; }       
        public string totalAmount { get; set; }
        public string noOfDays { get; set; }

        [JsonIgnore]
        public DateTime? dateFromDt { get; set; }

        [JsonIgnore]
        public DateTime? dateToDt { get; set; }

        [JsonIgnore]

        public decimal? Amount { get; set; }
    }

    public class HealthRegisterRequest
    {
        public string claimId { get; set; }
        public string policyNumber { get; set; }
        public string claimType { get; set; }
        public string componentID { get; set; }
        public string receiveDate { get; set; }
        public string incurredDate { get; set; }
        public string claimReason { get; set; }
        public string paymentMethod { get; set; }
        public string claimLocation { get; set; }
        public Payee payee { get; set; }
        public string currencyCode { get; set; }
        public string effectiveDate { get; set; }
        public string sourceNotification { get; set; }
        public string gratiaIndicator { get; set; }
        public string interimCoverage { get; set; }
        public string disallowITC { get; set; }
        public string soleProprietor { get; set; }
        public string purposeOrPersonal { get; set; }
        public string admitDate { get; set; }
        public string dischargeDate { get; set; }
        public string diagnosisCode { get; set; }
        public string doctorID { get; set; }
        public string providerID { get; set; }
        public List<Benefit> benefits { get; set; }
    }
    #endregion

    #region #CIRegister
    public class CIRegisterRequest
    {
        public string claimId { get; set; }
        public string policyNumber { get; set; }
        public string effectiveDate { get; set; }
        public string dateOfDisability { get; set; }
        public string causeOfDisability { get; set; }
        public string gratiaIndicator { get; set; }
        public string interimCoverage { get; set; }
        public string disallowITC { get; set; }
        public string soleProprietor { get; set; }
        public string purposeOrPersonal { get; set; }
        public string sourceNotification { get; set; }
        public string issuAgeVerified { get; set; }
        public string claimLocation { get; set; }
    }
    #endregion

    #endregion

    #region #CommonRequest
    public class CommonRegisterRequest
    {
        #region #Common
        public string claimId { get; set; }
        public string policyNumber { get; set; }
        public string effectiveDate { get; set; }
        public string dateOfDisability { get; set; }
        public string causeOfDisability { get; set; }
        public string gratiaIndicator { get; set; }
        public string interimCoverage { get; set; }
        public string disallowITC { get; set; }
        public string soleProprietor { get; set; }
        public string purposeOrPersonal { get; set; }
        public string sourceNotification { get; set; }
        public string relationship { get; set; }
        public string claimLocation { get; set; }        
        public string issuAgeVerified { get; set; }

        public string ProductCode { get; set; }
        public string BenefitClaimType { get; set; }
        #endregion

        #region #Health & NonHealth
        public Payee payee { get; set; }
        public string claimType { get; set; }
        public string componentID { get; set; }
        public string receiveDate { get; set; }
        public string incurredDate { get; set; }
        public string claimReason { get; set; }
        public string paymentMethod { get; set; }
        public string admitDate { get; set; }
        public string dischargeDate { get; set; }
        public string diagnosisCode { get; set; }
        public string doctorID { get; set; }
        public string providerID { get; set; }
        public string currencyCode { get; set; }
        #endregion

        #region #Health
        public List<Benefit> benefits { get; set; }
        #endregion

        #region #TPD & Death
        public Claiment claiment { get; set; }
        #endregion

        #region #NonHealth
        public string claimentOwnerAccount { get; set; }
        #endregion

        #region #CI
        public string ciOrNonhealth { get; set; }


        #endregion
    }
    #endregion

    
}
