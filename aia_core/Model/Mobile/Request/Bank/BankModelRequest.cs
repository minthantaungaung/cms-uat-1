using aia_core.Model.Cms.Request;
using aia_core.UnitOfWork;

namespace aia_core.Model.Mobile.Request.Blog
{
    public class BankModelRequest
    {
        public Guid BankId { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
    }

    public class UpdateBankModelRequest
    {
        public Guid ID { get; set; }
        public Guid BankId { get; set; }
        public string AccountHolderName { get; set; }
        public string AccountNumber { get; set; }
    }
}
