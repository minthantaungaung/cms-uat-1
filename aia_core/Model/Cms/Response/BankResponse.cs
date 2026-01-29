using System.ComponentModel.DataAnnotations;
using aia_core.Services;

namespace aia_core.Model.Cms.Response
{
    public class BankResponse
    {
        public Guid ID {get;set;}
        [Required]
        public string? BankName { get; set; }

        [Required]
        public string? BankName_MM { get; set; }
        
        [Required]
        public string? BankCode { get; set; }

        [Required]
        public string DigitType { get; set; }

        public int? DigitStartRange { get; set; }

        public int? DigitEndRange { get; set; }

        public List<int> DigitCustom { get; set; }

        [Required]
        public string? BankLogo { get; set; }

        [Required]
        public string AccountType { get; set; }

        public bool? IsActive { get; set; }

        public string? ILBankCode { get; set; } 

        public BankResponse() { }
        public BankResponse(Entities.Bank entity, Func<EnumFileType, string, string> blobUrl) 
        {
            ID = entity.ID;
            BankName = entity.BankName;
            BankName_MM = entity.BankName_MM;
            BankCode = entity.BankCode;
            DigitType = entity.DigitType;
            AccountType = entity.AccountType;
            DigitStartRange = entity.DigitStartRange;
            DigitEndRange = entity.DigitEndRange;
            IsActive = entity.IsActive;
            if(!string.IsNullOrEmpty(entity.DigitCustom)) DigitCustom = entity.DigitCustom.Split(',').Select(int.Parse).ToList();
            if (!string.IsNullOrEmpty(entity.BankLogo)) BankLogo = $"{blobUrl(EnumFileType.Bank, entity.BankLogo)}";

            ILBankCode = entity.IlBankCode;
            
        }
    }
}

