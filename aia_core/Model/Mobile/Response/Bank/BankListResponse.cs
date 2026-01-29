using aia_core.UnitOfWork;

namespace aia_core.Model.Mobile.Response
{
    // public class BankListResponse
    // {
    //     public List<BankResponse> list { get; set; }

    // }


    public class BankModelResponse 
    {
        public Guid ID { get; set; }
        public string BankName { get; set; }
        public string BankName_MM { get; set; }
        public string BankCode {get;set;}
        public string BankLogo { get; set; }
    }
}
