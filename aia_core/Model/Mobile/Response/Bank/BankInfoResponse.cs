namespace aia_core.Model.Mobile.Response.Bank
{
    public class BankInfoResponse
    {
        public Guid ID {get;set;}
        public Guid BankID {get;set;}
        public string BankName {get;set;}
        public string? BankName_MM { get; set; }
        public string BankCode { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
        public string BankLogo {get;set;}

    }
}