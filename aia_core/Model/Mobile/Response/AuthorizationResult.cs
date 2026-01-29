using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response
{
    public class AuthorizationResult
    {
        public bool Registration { get; set; }
        public bool Login { get; set; }
        public bool ViewMyPolicies { get; set; }
        public bool Servicing { get; set; }
        public bool Proposition { get; set; }
        public bool Claim { get; set; }
        public bool PolicyHolderDetails { get; set; }
        public bool InsuredDetails { get; set; }
        public bool BeneficiaryInfo { get; set; }
        public bool PaymentFrequency { get; set; }
        public bool ACP { get; set; }
        public bool LoanRepayment { get; set; }
        public bool AdhocTopup { get; set; }
        public bool HealthRenewal { get; set; }
        public bool LapseReinstatement { get; set; }
        public bool PartialWithdrawal { get; set; }
        public bool PolicyLoan { get; set; }
        public bool PolicyPaidup { get; set; }
        public bool PolicySurrender { get; set; }
        public bool RefundofPayment { get; set; }
        public bool SumAssuredChange { get; set; }
        public bool RequestStatus { get; set; }

        public bool PolicyLoanRepayment { get; set; }


        public string Test123 { get; set; }
        public AuthorizationResult() { }
        public AuthorizationResult(Entities.AuthorizationPerson entity)
        {
            Registration = entity.Registration;
            Login= entity.Login;
            ViewMyPolicies = entity.ViewMyPolicies;
            Proposition= entity.Proposition;
            PolicyHolderDetails= entity.PolicyHolderDetails;
            InsuredDetails= entity.InsuredDetails;
            BeneficiaryInfo= entity.BeneficiaryInfo;
            PaymentFrequency= entity.PaymentFrequency;
            ACP = entity.Acp;
            
            AdhocTopup= entity.AdhocTopup;
            HealthRenewal= entity.HealthRenewal;
            LapseReinstatement= entity.LapseReinstatement;
            PartialWithdrawal= entity.PartialWithdrawal;
            PolicyLoan= entity.PolicyLoan;
            PolicyPaidup= entity.PolicyPaidup;
            PolicySurrender= entity.PolicySurrender;
            RefundofPayment= entity.RefundofPayment;
            SumAssuredChange= entity.SumAssuredChange;
            Claim = entity.Claim;
            PolicyLoanRepayment= entity.PolicyLoanRepayment;
        }

        public AuthorizationResult(Entities.AuthorizationProduct entity)
        {
            Registration = entity.Registration;
            Login = entity.Login;
            ViewMyPolicies = entity.ViewMyPolicies;
            Proposition = entity.Proposition;
            PolicyHolderDetails = entity.PolicyHolderDetails;
            InsuredDetails = entity.InsuredDetails;
            BeneficiaryInfo = entity.BeneficiaryInfo;
            PaymentFrequency = entity.PaymentFrequency;
            ACP = entity.Acp;
            
            AdhocTopup = entity.AdhocTopup;
            HealthRenewal = entity.HealthRenewal;
            LapseReinstatement = entity.LapseReinstatement;
            PartialWithdrawal = entity.PartialWithdrawal;
            PolicyLoan = entity.PolicyLoan;
            PolicyPaidup = entity.PolicyPaidup;
            PolicySurrender = entity.PolicySurrender;
            RefundofPayment = entity.RefundofPayment;
            SumAssuredChange = entity.SumAssuredChange;
            Claim = entity.Claim;
            PolicyLoanRepayment = entity.PolicyLoanRepayment;
        }

        public AuthorizationResult(Entities.AuthorizationStatus entity)
        {
            Registration = entity.Registration;
            Login = entity.Login;
            ViewMyPolicies = entity.ViewMyPolicies;
            Proposition = entity.Proposition;
            PolicyHolderDetails = entity.PolicyHolderDetails;
            InsuredDetails = entity.InsuredDetails;
            BeneficiaryInfo = entity.BeneficiaryInfo;
            PaymentFrequency = entity.PaymentFrequency;
            ACP = entity.Acp;
           
            AdhocTopup = entity.AdhocTopup;
            HealthRenewal = entity.HealthRenewal;
            LapseReinstatement = entity.LapseReinstatement;
            PartialWithdrawal = entity.PartialWithdrawal;
            PolicyLoan = entity.PolicyLoan;
            PolicyPaidup = entity.PolicyPaidup;
            PolicySurrender = entity.PolicySurrender;
            RefundofPayment = entity.RefundofPayment;
            SumAssuredChange = entity.SumAssuredChange;
            Claim = Claim;

            PolicyLoanRepayment = entity.PolicyLoanRepayment;
        }
    }
}
