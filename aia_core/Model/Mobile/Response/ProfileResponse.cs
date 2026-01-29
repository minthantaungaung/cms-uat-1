namespace aia_core.Model.Mobile.Response
{
    public class ProfileResponse
    {
        public EnumMemberType? MemberType { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool? IsPhoneVerified { get; set; }
        public bool? IsEmailVerified { get; set; }
        public DateTime? Dob { get; set; }
        public EnumGender? Gender { get; set; }
        public string? ProfileImage { get; set; }
        public EnumIdenType? IdentificationType { get; set; }
        public string? IdentificationValue { get; set; }
        public ProfileResponse() { }
        public ProfileResponse(Entities.Member entity, Func<EnumFileType, string, string> blobUrl)
        {
            MemberType = EnumMemberType.individual;
            IsEmailVerified = entity.IsVerified ?? false;
            IsPhoneVerified = entity.IsVerified ?? false;
            FullName = entity.Name;
            ProfileImage = !string.IsNullOrEmpty(entity.ProfileImage) ? blobUrl(EnumFileType.Profile, entity.ProfileImage) : null;
            Email = entity.Email;
            Phone = entity.Mobile;
            Dob = entity.Dob;
            Gender = !string.IsNullOrEmpty(entity.Gender) ? Enum.Parse<EnumGender>(entity.Gender) : null;
            if (!string.IsNullOrEmpty(entity.Nrc)) 
            {
                IdentificationType = EnumIdenType.Nrc;
                IdentificationValue = entity.Nrc;
            }
            else if (!string.IsNullOrEmpty(entity.Passport))
            {
                IdentificationType = EnumIdenType.Passport;
                IdentificationValue = entity.Passport;
            }
            else
            {
                IdentificationType = EnumIdenType.Others;
                IdentificationValue = entity.Others;
            }
        }
    }
}
