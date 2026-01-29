using aia_core.Model.Mobile.Request;
using aia_core.Model.Mobile.Response;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Newtonsoft.Json;

namespace aia_core.Repository.Mobile
{
    public interface IProfileRepository
    {
        Task<ResponseModel<ProfileResponse>> GetProfile();
        Task<ResponseModel<ProfileResponse>> Update(UpdateProfileRequest model);
        Task<ResponseModel<object>> ChangeEmail(ChangeEmailRequest model);
        Task<ResponseModel<object>> ChangePassword(ChangePasswordRequest model);
        Task<ResponseModel<object>> ChangePhoneNumber(ChangePhoneRequest model);
    }
    public class ProfileRepository: BaseRepository, IProfileRepository
    {
        private readonly ICommonRepository commonRepository;
        private readonly INotificationService notificationService;
        private readonly ITemplateLoader templateLoader;
        private readonly IOktaService oktaService;

        public ProfileRepository(IOktaService oktaService, IHttpContextAccessor httpContext, ICommonRepository commonRepository,
            IAzureStorageService azureStorage,
            IConfiguration config,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork, INotificationService notificationService, ITemplateLoader templateLoader)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.oktaService = oktaService;
            this.commonRepository = commonRepository;
            this.notificationService = notificationService;
            this.templateLoader = templateLoader;
        }

        #region #get-profile
        public async Task<ResponseModel<ProfileResponse>> GetProfile()
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();
                
                var profile = await unitOfWork.GetRepository<Entities.Member>().Query(
                    expression: r => r.MemberId == memberId && r.IsActive == true)
                    .Include(x => x.MemberClients).ThenInclude(x => x.Client)
                    .FirstOrDefaultAsync();

                if (profile == null) return errorCodeProvider.GetResponseModel<ProfileResponse>(ErrorCode.E400);

                var profileResponse = new ProfileResponse(profile, GetFileFullUrl);


                var clientNoList = GetClientNoListByIdValue(memberId);

                var policies = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => clientNoList.Contains(x.PolicyHolderClientNo)
                        || clientNoList.Contains(x.InsuredPersonClientNo)
                        ).ToList();


                var hasCorporate = policies.Where(x => x.PolicyNo.Length > DefaultConstants.IndividualPolicyNoLength).Any();

                if (hasCorporate)
                {
                    profileResponse.MemberType = EnumMemberType.corporate;
                }
                else
                {
                    var isRubyMember = unitOfWork.GetRepository<Entities.Client>()
                .Query(x => clientNoList.Contains(x.ClientNo) && x.VipFlag == "Y").Any();

                    if (isRubyMember)
                    {
                        profileResponse.MemberType = EnumMemberType.ruby;
                    }
                    else
                    {
                        profileResponse.MemberType = EnumMemberType.individual;
                    }

                }

                Console.WriteLine($"GetProfile res {JsonConvert.SerializeObject(profileResponse)}");

                return errorCodeProvider.GetResponseModel<ProfileResponse>(ErrorCode.E0, profileResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetProfile ex {ex.Message}{ex.StackTrace}");

                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<ProfileResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #update-profile
        public async Task<ResponseModel<ProfileResponse>> Update(UpdateProfileRequest model)
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();
                var profile = await unitOfWork.GetRepository<Entities.Member>().Query(
                    expression: r => r.MemberId == memberId && r.IsActive == true).FirstOrDefaultAsync();
                if(profile == null) return errorCodeProvider.GetResponseModel<ProfileResponse>(ErrorCode.E400);

                var isProfileNameUpdateSuccess = true;
                var oktaUpdateUserResponse = "";
                if (!string.IsNullOrEmpty(model.FullName) && profile.Name != model.FullName)
                {
                    Console.WriteLine($"UpdateProfile => model.FullName {model.FullName}");

                    var updateProfile = await oktaService.UpdateUser(profile?.Auth0Userid, profile?.Email, profile?.Mobile, model.FullName, "");
                    if (updateProfile?.Code != (int)ErrorCode.E0)
                    {
                        isProfileNameUpdateSuccess = false;
                        oktaUpdateUserResponse = JsonConvert.SerializeObject(updateProfile);
                    }
                }

                Console.WriteLine($"UpdateProfile => isProfileNameUpdateSuccess {isProfileNameUpdateSuccess}");

                if (isProfileNameUpdateSuccess)
                {
                    #region #upload-cover-image
                    if (model.Image != null)
                    {
                        Console.WriteLine($"UpdateProfile => profile image {model.Image.Name} {model.Image.Length}");
                        var profileImage = $"{Utils.GetDefaultDate().Ticks}-{model.Image.FileName}";
                        var result = await azureStorage.UploadAsync(profileImage, model.Image);
                        profile.ProfileImage = result.Code == 200 ? profileImage : profile.ProfileImage;
                    }
                    #endregion

                    profile.Name = model.FullName;
                    profile.Gender = $"{model.Gender}";
                    profile.Dob = model.Dob;
                    profile.UpdatedDate = Utils.GetDefaultDate();
                    profile.OtpToken = null;
                    profile.OtpExpiry = null;

                    await unitOfWork.SaveChangesAsync();
                    return errorCodeProvider.GetResponseModel<ProfileResponse>(ErrorCode.E0, new ProfileResponse(profile, GetFileFullUrl));
                }

                return new ResponseModel<ProfileResponse> { Code = 500, Message = $"Okta UpdateUser error {oktaUpdateUserResponse}" };
            }
            catch(Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<ProfileResponse>(ErrorCode.E500);
            }
        }
        #endregion

        #region #change-email
        public async Task<ResponseModel<object>> ChangeEmail(ChangeEmailRequest model)
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();

                var email = await unitOfWork.GetRepository<Entities.Member>().Query(
                    expression: r => r.MemberId != memberId && r.Email == model.Email && r.IsActive == true && r.IsVerified == true).FirstOrDefaultAsync();
                if (email != null) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);

                var profile = await unitOfWork.GetRepository<Entities.Member>().Query(
                    expression: r => r.MemberId == memberId && r.IsActive == true && r.IsVerified == true).FirstOrDefaultAsync();
                if (profile == null) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);
                else if (profile?.OtpCode != model.OtpToken
                    || profile.OtpExpiry < Utils.GetDefaultDate()
                    || profile?.OtpType != $"{EnumOtpType.changeemail}") return new ResponseModel<object> { Code = 400, Message = "Invalid otp token or expired." };

                var updateProfile = await oktaService.UpdateUser(profile.Auth0Userid, model.Email, profile.Mobile);
                if (updateProfile?.Code == (int)ErrorCode.E0)
                {
                    profile.Email = model.Email ?? profile.Email;
                    profile.UpdatedDate = Utils.GetDefaultDate();
                    profile.OtpCode = null;
                    profile.OtpExpiry = null;
                    await unitOfWork.SaveChangesAsync();

                    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, new { refNumber = model.Email });
                }
                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E4003);
            }
            catch(Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion

        #region #change-password
        public async Task<ResponseModel<object>> ChangePassword(ChangePasswordRequest model)
        {
            var retry = 0;
            var retryLimit = 3;
            var isOktaError = false;

        OktaChangePassword:
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken();
                var member = await unitOfWork.GetRepository<Entities.Member>().Query(
                    expression: r => r.MemberId == memberId && r.IsActive == true && r.IsVerified == true).FirstOrDefaultAsync();
                if (member == null
                    && string.IsNullOrEmpty(member?.Auth0Userid)) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);

                if (member != null)
                {

                    isOktaError = true;

                    var changePassword = await oktaService.ChangePassword(member.Auth0Userid, model.CurrentPassword, model.ConfirmNewPassword);
                    if (changePassword?.Code != (int)ErrorCode.E0)
                    {
                        #region #retry
                        if (retry < retryLimit)
                        {
                            MobileErrorLog("Okta ChangePassword at retry " + retry, changePassword?.Message ?? "Invalid current password", null, httpContext?.HttpContext.Request.Path);
                            retry++;
                            isOktaError = false;
                            goto OktaChangePassword;
                        }

                        #endregion
                        return new ResponseModel<object> { Code = 400, Message = changePassword?.Message ?? "Invalid current password" };
                    }



                    isOktaError = false;

                    #region #Noti
                    try
                    {
                        var notiId = Guid.NewGuid();
                        var notiMsgListJson = templateLoader.GetNotiMsgListJson();

                        var msg = notiMsgListJson[EnumSystemNotiType.PasswordChange.ToString()];

                        var notification = new Entities.MemberNotification()
                        {
                            IsDeleted = false,
                            IsRead = false,
                            Id = notiId,
                            Type = EnumNotificationType.Others.ToString(),
                            IsSytemNoti = true,
                            SystemNotiType = EnumSystemNotiType.PasswordChange.ToString(),
                            MemberId = member.MemberId,
                            CreatedDate = Utils.GetDefaultDate(),
                            Message = msg?.Message,
                        };

                        unitOfWork.GetRepository<Entities.MemberNotification>().Add(notification);
                        unitOfWork.SaveChanges();

                        await notificationService.SendNotification(new NotificationMessage
                        {
                            MemberId = memberId,
                            NotificationType = EnumNotificationType.Others,
                            IsSytemNoti = true,
                            SystemNotiType = EnumSystemNotiType.PasswordChange,
                            Message = msg?.Message,
                            Title = msg?.Title,
                            ImageUrl = msg?.ImageUrl,
                            NotificationId = notiId.ToString(),
                        });
                    }
                    catch { }
                    #endregion

                    return new ResponseModel<object> { Code = 0, Message = "Your password has been changed successfully.", Data = "" };
                }

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E401);
            }
            catch (Exception ex)
            {
                #region #retry
                if (isOktaError && retry < retryLimit)
                {
                    MobileErrorLog("Okta ChangePassword exception at retry " + retry, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                    retry++;
                    isOktaError = false;
                    goto OktaChangePassword;
                }
                #endregion
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion

        #region #change-phone-number
        public async Task<ResponseModel<object>> ChangePhoneNumber(ChangePhoneRequest model)
        {
            try
            {
                var referenceNumber = Utils.ReferenceNumber(model.Phone);
                var memberId = commonRepository.GetMemberIDFromToken();

                var phone = await unitOfWork.GetRepository<Entities.Member>().Query(
                    expression: r => r.MemberId != memberId && r.Mobile == referenceNumber && r.IsActive == true && r.IsVerified == true).FirstOrDefaultAsync();
                if (phone != null) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E4001);

                var profile = await unitOfWork.GetRepository<Entities.Member>().Query(
                    expression: r => r.MemberId == memberId && r.OtpCode == model.OtpToken && r.IsActive == true && r.IsVerified == true).FirstOrDefaultAsync();
                if (profile == null
                    && string.IsNullOrEmpty(profile?.Auth0Userid)) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);
                else if (profile?.OtpCode != model.OtpToken
                    || profile.OtpExpiry < Utils.GetDefaultDate()
                    || profile?.OtpType != $"{EnumOtpType.changephone}") return new ResponseModel<object> { Code = 400, Message = "Invalid otp token or expired." };

                //get-sms-factor-id
                var oktaFactors = await oktaService.ListEnrollFactors(profile.Auth0Userid);
                if (oktaFactors?.Code == (long)ErrorCode.E0)
                {
                    var smsFactor = oktaFactors.Data.Where(r => (r.profile.phoneNumber == profile.Mobile && r.factorType == "sms")).OrderByDescending(o => o.factorType).FirstOrDefault();
                    if (!string.IsNullOrEmpty(smsFactor?.id))
                    {
                        //unroll-existing-sms-factor
                        var deleteFactor = await oktaService.UnenrollSMS(smsFactor.id, profile.Auth0Userid);
                        if (deleteFactor?.Code == (int)ErrorCode.E0)
                        {
                            //enroll-new-sms-factor
                            var enrollSms = await oktaService.EnrollNewSMS(referenceNumber, profile.Auth0Userid);
                            profile.Mobile = referenceNumber ?? profile.Mobile;
                            profile.UpdatedDate = Utils.GetDefaultDate();
                            profile.OtpCode = null;
                            profile.OtpExpiry = null;
                            await unitOfWork.SaveChangesAsync();
                            return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, new { refNumber = model.Phone });
                        }
                    }
                }
                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E4003);
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion
    }
}
