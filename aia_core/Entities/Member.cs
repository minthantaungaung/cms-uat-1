using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Member
{
    public Guid MemberId { get; set; }

    public string? Name { get; set; }
    public string? ProfileImage { get; set; }

    public DateTime? Dob { get; set; }

    public string? Gender { get; set; }

    public string? Nrc { get; set; }

    public string? Passport { get; set; }

    public string? Others { get; set; }

    public string? Mobile { get; set; }

    public string? Email { get; set; }

    public DateTime? RegisterDate { get; set; }
    public DateTime? UpdatedDate { get; set; }

    public DateTime? LastActiveDate { get; set; }

    public bool? IsActive { get; set; }

    public string? Auth0Userid { get; set; }
    public string? OktaUserName { get; set; }
    public bool? IsVerified { get; set; }
    public string? OtpToken { get; set; }
    public DateTime? OtpExpiry { get; set; }
    public string? OtpCode { get; set; }
    public string? OtpType { get; set; }
    public string? OtpTo { get; set; }
    public bool? IsEmailVerified { get; set; }
    public bool? IsMobileVerified { get; set; }

    public string? MemberType { get; set; }

    public string? GroupMemberID { get; set; }

    public string? IndividualMemberID { get; set; }

    public string? Country { get; set; }
    public string? Province { get; set; }
    public string? District { get; set; }
    public string? Township { get; set; }
    public string? AllClientNoListString { get; set; }

    public string? ProductCodeList { get; set; }

    public string? PolicyStatusList { get; set; }

    public string? AppOS { get; set; }

    public virtual ICollection<MemberClient> MemberClients { get; set; } = new List<MemberClient>();
}
