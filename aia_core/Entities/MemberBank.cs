using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class MemberBank
{
    public Guid ID { get; set; }
    public Guid MemberID { get; set; }
    public Guid BankID {get;set;}
    public string AccountHolderName { get; set; }
    public string AccountNumber { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public virtual Bank? Bank { get; set; }

}