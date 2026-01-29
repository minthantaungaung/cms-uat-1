using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace aia_core.Entities;

public partial class Client
{
    public string MasterClientNo { get; set; } = null!;

    public string ClientNo { get; set; } = null!;

    public string? Name { get; set; }

    public string? Nrc { get; set; }

    public string? PassportNo { get; set; }

    public string? Other { get; set; }

    public string? Gender { get; set; }

    public DateTime Dob { get; set; }

    public string? PhoneNo { get; set; }

    public string? Email { get; set; }

    public string? MaritalStatus { get; set; }

    public string? FatherName { get; set; }

    public string? Occupation { get; set; }

    public string? Address1 { get; set; }

    public string? Address2 { get; set; }

    public string? Address3 { get; set; }

    public string? Address4 { get; set; }

    public string? Address5 { get; set; }

   
    public string? Address6 { get; set; }

    public string? VipFlag { get; set; }

    public DateTime? VipEffectiveDate { get; set; }

    public DateTime? VipExpiryDate { get; set; }

    public string? AgentFlag { get; set; }

    public string? AgentCode { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
    public string? client_certificate { get; set; }

    public virtual Occupation? OccupationNavigation { get; set; }

    public virtual ICollection<Policy> PolicyInsured { get; set; } = new List<Policy>();

    public virtual ICollection<Policy> PolicyHolder { get; set; } = new List<Policy>();

    public virtual MemberClient MemberClient { get; set; } = new MemberClient();
    public virtual ICollection<ClaimTran> ClaimTran { get; set; } = new List<ClaimTran>();
}
