using aia_core.Extension;
using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class BankRequest
    {
        [Required]
        public string? BankName { get; set; }

        [Required]
        public string? BankName_MM { get; set; }
        
        [Required]
        public string? BankCode { get; set; }

        [Required]
        public EnumBankDigitType DigitType { get; set; }

        public int? DigitStartRange { get; set; }

        public int? DigitEndRange { get; set; }

        public List<int>? DigitCustom { get; set; }


        [AllowedFileExtensions(".jpg", ".jpeg", ".png")]
        public IFormFile? BankLogo { get; set; }

        [Required]
        public EnumBankAccountType AccountType { get; set; }
        public string? ILBankCode { get; set; }
    }

    public class CreateBankRequest: BankRequest
    {
        
    }
    public class UpdateBankRequest : BankRequest
    {
        public Guid? Id { get; set; }
        
    }

    public class UpdateBankStatusRequest
    {
        public Guid? Id { get; set; }
        public bool IsActive { get; set; }
    }
}
