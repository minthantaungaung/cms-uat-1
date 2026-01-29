using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.UnitOfWork;
using FastMember;

namespace aia_core.Model.Cms.Response
{
    public class MemberResponse
    {
        public string? AppRegMemberId { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; } 

        public string? MemberPhone { get; set; }

        public string? MemberEmail { get; set; }

        public string? MemberIdNrc { get; set; }

        public string? MemberIdPassport { get; set; }

        public string? MemberIdOther { get; set; }

        public bool? MemberIsActive { get; set; }

        public string? MemberType { get; set; }

        public string? MemberGender { get; set; }   

        public DateTime? MemberDob { get; set; }

        public string? MemberIndCop { get; set; }

        public string? IndividualMemberID { get; set; }

        public string? GroupMemberID { get; set; }

        public DateTime? RegisterDate { get; set; }

        public DateTime? LastActiveDate { get; set; }

        public string IsVerified { get; set; }

        public MemberResponse() { }
        public MemberResponse(Entities.Member entity)
        {
            AppRegMemberId = entity.MemberId.ToString();
            MemberName = entity.Name;
            MemberEmail = entity.Email;
            MemberPhone = entity.Mobile;
            MemberIdNrc = entity.Nrc;
            MemberIdPassport = entity.Passport;
            MemberIdOther = entity.Others;
            MemberIsActive = entity.IsActive;
            MemberDob = entity.Dob;
            MemberGender = entity.Gender;
            RegisterDate = entity.RegisterDate;

            LastActiveDate = entity.IsVerified == true ? entity.LastActiveDate : null;
            IsVerified = entity.IsVerified == true ? "Verified" : "Not Verified";


            var IsVipMember = entity.MemberClients.Any(x => x.Client?.VipFlag == "Y");
            MemberType = IsVipMember ? EnumIndividualMemberType.Ruby.ToString() : EnumIndividualMemberType.Member.ToString();

            var corporateMember = entity.MemberClients
                .Where(x => x.Client.PolicyHolder.Any(x => x.PolicyNo.Length > DefaultConstants.IndividualPolicyNoLength)
                || x.Client.PolicyInsured.Any(x => x.PolicyNo.Length > DefaultConstants.IndividualPolicyNoLength))
                .Select(x => x).FirstOrDefault();
            

            GroupMemberID = corporateMember?.ClientNo;
            IndividualMemberID = entity.MemberClients.Where(x => x.ClientNo != corporateMember?.ClientNo)
                .FirstOrDefault()?.ClientNo;

            MemberId = IndividualMemberID;

            MemberIndCop = corporateMember != null ?
                EnumMemberType.corporate.ToString() : EnumMemberType.individual.ToString();
        }
    }


    public class MemberListResponse
    {
        public Guid? AppRegMemberId { get; set; }
        public string? MemberId { get; set; }
        public string? MemberName { get; set; }

        public string? MemberPhone { get; set; }

        public string? MemberEmail { get; set; }

        public string? MemberIdNrc { get; set; }

        public string? MemberIdPassport { get; set; }

        public string? MemberIdOther { get; set; }

        public bool? MemberIsActive { get; set; }

        public string? MemberType { get; set; }

        public string? MemberGender { get; set; }

        public DateTime? MemberDob { get; set; }

        public string? MemberIndCop { get; set; }

        public string? IndividualMemberID { get; set; }

        public string? GroupMemberID { get; set; }

        public DateTime? RegisterDate { get; set; }

        public DateTime? LastActiveDate { get; set; }

        public string? IsVerified { get; set; }

        public bool? IsVerified_Flag { get; set; }

        public MemberListResponse() { }
        
    }
}
