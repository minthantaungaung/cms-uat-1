namespace aia_core.Model.AiaCrm
{
    public class CaseRequest
    {
        public CustomerInfo CustomerInfo { get; set; }
        public PolicyInfo PolicyInfo { get; set; }
        public Request RequestInfo { get; set; }
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class CustomerInfo
    {
        public string ClientNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    public class PolicyInfo
    {
        public string PolicyNumber { get; set; }
        public string MasterPolicyNumber { get; set; }
    }

    public class Request
    {
        public string RequestId { get; set; }
        public string ClaimId { get; set; }
        public string Channel { get; set; }
        public string CaseType { get; set; }
        public string CaseCategory { get; set; }
        public string BankAccountName { get; set; }
        public string BankAccountNo { get; set; }
        public string BankName { get; set; }
    }



}