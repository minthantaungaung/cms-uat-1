using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Bank
{
    public Guid ID { get; set; }
    public string BankName { get; set; }
    public string? BankName_MM { get; set; }
    public string BankCode {get;set;}
    public string DigitType { get; set; }
    public int? DigitStartRange { get; set; }
    public int? DigitEndRange { get; set; }
    public string? DigitCustom { get; set; }
    public string? BankLogo { get; set; }
    public string AccountType { get; set; }
    public DateTime? CreatedDate { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDelete { get; set; }
    public int? Sort { get; set; }

    public string? IlBankCode { get; set; }

}