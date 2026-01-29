using aia_core.Entities;
using aia_core.Model.Mobile.Request;
using aia_core.Services;
using aia_core.UnitOfWork;
using FastMember;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Reflection;
using System.Security.Cryptography;
using System.Transactions;
using Newtonsoft.Json;
using aia_core.Model.Cms.Response;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;

namespace aia_core.Repository.Mobile
{
    public interface IAuthRepository
    {
        ResponseModel<string> RegisterDevice(DeviceRequest model);
        Task<ResponseModel<object>> CheckIdentification(CheckIdentificationRequest model);
        Task<ResponseModel<object>> Register(RegisterRequest model);
        Task<ResponseModel<object>> OtpRequest(string refNumber);

        Task<ResponseModel<object>> ProfileOtpRequest(string refNumber, EnumOtpType type);

        Task<ResponseModel<object>> OtpVerify(string otpCode, string refNumber);
        Task<ResponseModel<object>> GetOktaUserName(string refNumber);
        Task<ResponseModel<object>> ForgotPassword(string refNumber);
        Task<ResponseModel<object>> ResetPassword(ResetPasswordRequest model);
        Task<ResponseModel<object>> RefreshToken(RefreshTokenRequest model);

        ResponseModel<object> Logout();
    }
    public class AuthRepository : BaseRepository, IAuthRepository
    {
        private readonly IConfiguration config;
        private readonly IOktaService oktaService;
        private readonly ICommonRepository commonRepository;
        private readonly INotificationService notificationService;
        private readonly ITemplateLoader templateLoader;
        
        public AuthRepository(IConfiguration config, IOktaService oktaService, IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork, ICommonRepository commonRepository
            , INotificationService notificationService, ITemplateLoader templateLoader)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.config = config;
            this.oktaService = oktaService;
            this.commonRepository = commonRepository;
            this.notificationService = notificationService;
            this.templateLoader = templateLoader;
        }

        public void RegisterDevice(DeviceRequest device)
        {
            var memberDevice = unitOfWork.GetRepository<Entities.MemberDevice>()
                                    .Query(x => x.MemberId == device.MemberId && x.DeviceType == device.DeviceType.ToString() && x. PushToken == device.PushToken)
                                    .FirstOrDefault();

            if (memberDevice == null)
            {
                memberDevice = new MemberDevice()
                {
                    Id = Guid.NewGuid(),
                    MemberId = device.MemberId,
                    DeviceType = device.DeviceType.ToString(),
                    PushToken = device.PushToken,
                    CreatedDate = Utils.GetDefaultDate(),
                };

                unitOfWork.GetRepository<Entities.MemberDevice>().Add(memberDevice);
                unitOfWork.SaveChanges();
            }
        }



        ResponseModel<string> IAuthRepository.RegisterDevice(DeviceRequest model)
        {
            try
            {
                var memberId = commonRepository.GetMemberIDFromToken()?.ToString();
                if (!string.IsNullOrEmpty(memberId))
                {

                    RegisterDevice(new DeviceRequest()
                    {
                        MemberId = memberId,
                        PushToken = model.PushToken,
                        DeviceType = model.DeviceType,
                    });


                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0, "Register device successfully.");
                }

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E401);

            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
        }

        #region #check-identifcation
        public async Task<ResponseModel<object>> CheckIdentification(CheckIdentificationRequest model)
        {
            try
            {
                var _query = unitOfWork.GetRepository<Entities.Member>().Query(expression: r => r.IsVerified == true);
                if (model.IdentificationType == EnumIdenType.Nrc)
                {
                    _query = _query.Where(r => r.Nrc == model.IdentificationValue);
                }
                else if (model.IdentificationType == EnumIdenType.Passport)
                {
                    _query = _query.Where(r => r.Passport == model.IdentificationValue);
                }
                else if (model.IdentificationType == EnumIdenType.Others)
                {
                    _query = _query.Where(r => r.Others == model.IdentificationValue);
                }
                else
                {
                    return new ResponseModel<object> { Code = 400, Message = "No identification provided" };
                }

                var entity = await _query.FirstOrDefaultAsync();
                if (entity != null) return new ResponseModel<object> { Code = 400, Message = "The NRC/Passport/Others is already in use." };

                var query = unitOfWork.GetRepository<Entities.Client>().Query();
                if (model.IdentificationType == EnumIdenType.Nrc)
                {
                    query = query.Where(r => r.Nrc == model.IdentificationValue);
                }
                else if (model.IdentificationType == EnumIdenType.Passport)
                {
                    query = query.Where(r => r.PassportNo == model.IdentificationValue);
                }
                else if (model.IdentificationType == EnumIdenType.Others)
                {
                    query = query.Where(r => r.Other == model.IdentificationValue);
                }
                else
                {
                    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);
                }

                var member = await query.FirstOrDefaultAsync();
                if (member == null) return new ResponseModel<object> { Code = 400, Message = "Sorry. You are not AIA member yet!\r\nIf you have already purchased a policy from AIA Myanmar and are having trouble registering \r\nthe app, kindly reach out to SHER." };

                
                if(CheckAuthorization(null, model.IdentificationValue)?.Registration == false)
                    return new ResponseModel<object> { Code = 403, Message = "Kindly verify your policy status to ensure the registration process." };
                

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0);
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion

        #region #register-account
        public async Task<ResponseModel<object>> Register(RegisterRequest model)
        {
            try
            {
                var appOs = httpContext?.HttpContext?.Request?.Headers["OS"].ToString();
                if (string.IsNullOrEmpty(appOs))
                {
                    appOs = httpContext?.HttpContext?.Request?.Headers["Os"].ToString();
                    if (string.IsNullOrEmpty(appOs))
                    {
                        appOs = httpContext?.HttpContext?.Request?.Headers["os"].ToString();
                    }
                }

                var client = await unitOfWork.GetRepository<Entities.Client>().Query(
                    expression: r => r.Nrc == model.IdentificationValue || r.PassportNo == model.IdentificationValue || r.Other == model.IdentificationValue
                    ).FirstOrDefaultAsync();
					
                if(client == null) return new ResponseModel<object> { Code = 400, Message = "No client found" };

                if (CheckAuthorization(null, model.IdentificationValue)?.Registration == false)
                    return new ResponseModel<object> { Code = 403, Message = "Kindly verify your policy status to ensure the registration process." };

                #region #Record Policy For Log
                try
                {
                    var clientNoList = unitOfWork.GetRepository<Entities.Client>()
                       .Query(x => x.Nrc == model.IdentificationValue || x.PassportNo == model.IdentificationValue || x.Other == model.IdentificationValue)
                       .Select(x => x.ClientNo).ToList();

                    var policyList = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => clientNoList.Contains(x.PolicyHolderClientNo) || clientNoList.Contains(x.InsuredPersonClientNo))
                        .Select(x => new { x.PolicyNo, x.PolicyStatus, x.PremiumStatus, x.ProductType })
                        .ToList();

                    var logMessage = "";
                    policyList?.ForEach(policy =>
                    {
                        logMessage += $"<{policy.PolicyNo} {policy.ProductType} {policy.PolicyStatus} {policy.PremiumStatus}> ";
                    }
                    );

                    logMessage = $"Register Record Policy For Log => {model.IdentificationValue} {Utils.GetDefaultDate()} {logMessage}";

                    Console.WriteLine(logMessage);
                }
                catch { }
                

                #endregion

                var referenceNumber = Utils.ReferenceNumber(model.Phone); //var phoneNumber = Utils.ConcatPlusPhoneNumber(model.Phone);
                var data = await unitOfWork.GetRepository<Entities.Member>().Query(
                    expression: r => r.Nrc == model.IdentificationValue || r.Passport == model.IdentificationValue || r.Others == model.IdentificationValue
                    ).FirstOrDefaultAsync();

                if (data != null
                    && data?.IsActive == true && data?.IsVerified == true) return new ResponseModel<object> { Code = 400, Message = "Already registered user" };
                else if (data != null
                    && data?.IsActive == false
                    && data?.IsVerified != true)
                {
                    var hasEmail = await unitOfWork.GetRepository<Entities.Member>().Query(expression: r => r.MemberId != data.MemberId && r.Email == model.Email).AnyAsync();
                    if (hasEmail) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E4002);
                    var hasPhone = await unitOfWork.GetRepository<Entities.Member>().Query(expression: r => r.MemberId != data.MemberId && r.Mobile == referenceNumber).AnyAsync();
                    if (hasPhone) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E4001);

                    if (!string.IsNullOrEmpty(data.Auth0Userid))
                    {
                        var okta = await oktaService.DeleteUser(data.Auth0Userid);
                    }
                    data.OtpCode = Utils.GenerateOTP();
                    data.OtpType = $"{EnumOtpType.register}";
                    data.OtpExpiry = Utils.GetDefaultDate().AddMinutes(10);
                    data.OktaUserName = Utils.GenerateOktaUserName();
                    await unitOfWork.SaveChangesAsync();

                    var oktaRegister = await oktaService.RegisterUser(data.OktaUserName, model);
                    if (oktaRegister.Code == (long)ErrorCode.E0)
                    {
                        data.Name = model.FullName;
                        data.Email = model.Email;
                        data.Mobile = referenceNumber;
                        data.Dob = model.Dob;
                        data.Gender = $"{model.Gender}";
                        data.RegisterDate = Utils.GetDefaultDate();
                        data.Auth0Userid = oktaRegister.Data.id;
                        data.IsMobileVerified = false;
                        data.IsEmailVerified = false;
                        data.IsVerified = false;
                        data.AppOS = appOs;
                        await unitOfWork.SaveChangesAsync();

                        try { MobileErrorLog(null, "data.OtpCode " + data.OtpCode, "", httpContext?.HttpContext.Request.Path); } catch { }
                        await SendOTPCode(referenceNumber, string.Format($"{config["SmsPoh:OtpMessage"]}", data.OtpCode), config["SmsPoh:Key"], model.FullName, data.OtpCode);
                        return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, new { refNumber = model.Phone });
                    }
                    else if (oktaRegister.Code == (long)ErrorCode.E400 && !string.IsNullOrEmpty(oktaRegister.Message))
                    {
                        if(oktaRegister.Message.Contains("Password requirements were not met"))
                        {
                            return new ResponseModel<object> { Code = 400, Message = "Password requirements were not met." };
                        }
                        return new ResponseModel<object> { Code = 400, Message = oktaRegister.Message };
                    }

                    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E4004);
                }

                var email = await unitOfWork.GetRepository<Entities.Member>().Query(expression: r => r.Email == model.Email).AnyAsync();
                if (email) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E4002);
                var phone = await unitOfWork.GetRepository<Entities.Member>().Query(expression: r => r.Mobile == referenceNumber).AnyAsync();
                if (phone) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E4001);

                var query = unitOfWork.GetRepository<Entities.Client>().Query();
                if (model.IdentificationType == EnumIdenType.Nrc)
                {
                    query = query.Where(r => r.Nrc == model.IdentificationValue);
                }
                else if (model.IdentificationType == EnumIdenType.Passport)
                {
                    query = query.Where(r => r.PassportNo == model.IdentificationValue);
                }
                else if (model.IdentificationType == EnumIdenType.Others)
                {
                    query = query.Where(r => r.Other == model.IdentificationValue);
                }
                else
                {
                    return new ResponseModel<object> { Code = 400, Message = "No identification provided" };
                }

                var member = await query.FirstOrDefaultAsync();
                if (member == null) return new ResponseModel<object> { Code = 400, Message = "No client found" };

                (string? membertype, string? memberID, string? groupMemberId) clientInfo = GetClientInfoByIdValue(model.IdentificationValue);

                
                var entity = new Entities.Member
                {
                    MemberId = Guid.NewGuid(),
                    Name = model.FullName,
                    Gender = $"{model.Gender}",
                    Dob = model.Dob,
                    Mobile = referenceNumber,
                    Email = model.Email,
                    Nrc = member.Nrc,
                    Passport = member.PassportNo,
                    Others = member.Other,
                    IsActive = false,
                    OktaUserName = Utils.GenerateOktaUserName(),
                    RegisterDate = Utils.GetDefaultDate(),
                    IsEmailVerified = false,
                    IsMobileVerified = false,
                    MemberType = clientInfo.membertype,
                    GroupMemberID = clientInfo.groupMemberId,
                    IndividualMemberID = clientInfo.memberID,
                    IsVerified = false,
                    AppOS = appOs,
            };

                entity.MemberClients.Add(new MemberClient { Id = Guid.NewGuid(), ClientNo = member.ClientNo, MemberId = entity.MemberId });


                

                var oktaRegisterUser = await oktaService.RegisterUser(entity.OktaUserName, model);
                if (oktaRegisterUser.Code == (long)ErrorCode.E0)
                {
                    using (var scope = new TransactionScope(
                        scopeOption: TransactionScopeOption.Suppress,
                        scopeTimeout: TimeSpan.FromMinutes(3),
                        asyncFlowOption: TransactionScopeAsyncFlowOption.Enabled
                        ))
                    {
                        entity.OtpCode = Utils.GenerateOTP();
                        entity.OtpType = $"{EnumOtpType.register}";
                        entity.OtpExpiry = Utils.GetDefaultDate().AddMinutes(10);
                        entity.Auth0Userid = oktaRegisterUser.Data.id;


                        #region #ConcurrencyCheck
                        ////if (model.IdentificationValue == "12/UKAMA(N)112299")
                        ////{
                        ////    Console.WriteLine("12/UKAMA(N)112299 I am paused! 10 seconds");
                        ////    Thread.Sleep(10000);
                        ////}

                        var concurrencyData = await unitOfWork.GetRepository<Entities.Member>()
                            .Query(expression: r => r.Nrc == model.IdentificationValue || r.Passport == model.IdentificationValue || r.Others == model.IdentificationValue)
                            .FirstOrDefaultAsync();

                        if (concurrencyData != null) return new ResponseModel<object> { Code = 400, Message = "Concurrency user registration error!" };

                        #endregion

                        await unitOfWork.GetRepository<Entities.Member>().AddAsync(entity);
                        await unitOfWork.SaveChangesAsync();
                        scope.Complete();

                        try { MobileErrorLog(null, "entity.OtpCode " + entity.OtpCode, "", httpContext?.HttpContext.Request.Path); } catch { }
                        await SendOTPCode(referenceNumber, string.Format($"{config["SmsPoh:OtpMessage"]}", entity.OtpCode), $"{config["SmsPoh:Key"]}"
                            , entity.Name, entity.OtpCode);

                        return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, new { refNumber = model.Phone });
                    }
                }
                else if (oktaRegisterUser.Code == (long)ErrorCode.E400 && !string.IsNullOrEmpty(oktaRegisterUser.Message))
                {
                    if (oktaRegisterUser.Message.Contains("Password requirements were not met"))
                    {
                        return new ResponseModel<object> { Code = 400, Message = "Password requirements were not met." };
                    }
                    return new ResponseModel<object> { Code = 400, Message = oktaRegisterUser.Message };
                }
                else
                {
                    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E4003);
                }
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion

        #region #otp-request
        public async Task<ResponseModel<object>> ProfileOtpRequest(string refNumber, EnumOtpType type)
        {
            try
            {
                var referenceNumber = Utils.ReferenceNumber(refNumber);


                #region #OtpRateLimit
                if (IsOtpRateLimitExceeded(new OtpRateLimitModel
                {
                    UserIdentifier = referenceNumber,
                    RateLimitOtpType = RateLimitOtpType.ProfileOtpRequest
                }) == true)
                {
                    return new ResponseModel<object>
                    {
                        Code = 429,
                        Message = "Too many attempts. Please try again in 5 minutes."
                    };
                }
                #endregion


                var memberId = commonRepository.GetMemberIDFromToken();

                var member = await unitOfWork.GetRepository<Entities.Member>().Query(x => x.MemberId == memberId).FirstOrDefaultAsync();
                if (member == null) return new ResponseModel<object> { Code = 400, Message = "No profile found." };


                var otherMemerQuery = unitOfWork.GetRepository<Entities.Member>()
                        .Query(x => x.MemberId != memberId && x.IsActive == true && x.IsVerified == true);

                if (type == EnumOtpType.changephone)
                {
                    if (member.Mobile == referenceNumber)
                        return new ResponseModel<object> { Code = 400, Message = "Phone number is same phone number." };
                    if (otherMemerQuery != null && otherMemerQuery.Where(x => x.Mobile == referenceNumber).Any()) 
                        return new ResponseModel<object> { Code = 400, Message = "Phone number is used by another registered user." };
                }

                if (type == EnumOtpType.changeemail)
                {
                    if (member.Email == referenceNumber)
                        return new ResponseModel<object> { Code = 400, Message = "Email is same." };
                    if (otherMemerQuery != null && otherMemerQuery.Where(x => x.Email == referenceNumber).Any())
                        return new ResponseModel<object> { Code = 400, Message = "Email is used by another registered user." };
                }

                member.OtpToken = null;
                member.OtpType = $"{type}";
                member.OtpTo = referenceNumber;
                member.OtpCode = Utils.GenerateOTP();
                member.OtpExpiry = Utils.GetDefaultDate().AddMinutes(10);
                await unitOfWork.SaveChangesAsync();
                
                await SendOTPCode(referenceNumber, string.Format($"{config["SmsPoh:OtpMessage"]}", member.OtpCode), $"{config["SmsPoh:Key"]}"
                    , member.Name, member.OtpCode);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, new { refNumber = refNumber });
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion

        #region #otp-verify
        public async Task<ResponseModel<object>> OtpVerify(string otpCode, string refNumber)
        {
            try
            {
                

                var referenceNumber = Utils.ReferenceNumber(refNumber);

                #region #BruteForce
                var otpBruteForceAttemptsRateLimitModel  = new OtpBruteForceAttemptsRateLimitModel
                {
                    UserIdentifier = referenceNumber,
                    OtpCode = otpCode,
                    RateLimitOtpType = RateLimitOtpType.OtpVerify
                };

                if (IsOtpBruteForceAttemptDetacted(otpBruteForceAttemptsRateLimitModel) == true)
                {
                    return new ResponseModel<object>
                    {
                        Code = 429,
                        Message = "Too many failed Otp attempts!"
                    };
                }
                #endregion

                var member = await unitOfWork.GetRepository<Entities.Member>().Query(
                        expression: r => r.Mobile == referenceNumber || r.Email == referenceNumber).FirstOrDefaultAsync();
                if (member == null) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);

                if (member.OtpCode != otpCode || member.OtpExpiry < Utils.GetDefaultDate())
                {
                    #region #BruteForce
                    otpBruteForceAttemptsRateLimitModel.IsSuccess = false;
                    AddOtpBruteForceAttempt(otpBruteForceAttemptsRateLimitModel);
                    #endregion

                    return new ResponseModel<object> { Code = 400, Message = "Invalid opt code or expired." };
                }

                #region #BruteForce
                otpBruteForceAttemptsRateLimitModel.IsSuccess = true;
                AddOtpBruteForceAttempt(otpBruteForceAttemptsRateLimitModel);
                #endregion

                Console.WriteLine($"OtpVerify > refNumber > {refNumber} OtpType> {member?.OtpType} Datetime > {Utils.GetDefaultDate()}");

                //MobileErrorLog($"OtpVerify => {refNumber} {otpCode}", JsonConvert.SerializeObject(member), null, httpContext?.HttpContext.Request.Path);

                if ((!member.IsVerified.HasValue || member.IsVerified == false)
                    && member?.OtpType == $"{EnumOtpType.register}")
                {
                    member.IsVerified = true;
                    member.IsActive = true;
                    member.OtpExpiry = null;
                    member.OtpToken = null;
                    member.OtpType = null;
                    member.OtpCode = null;
                    member.IsEmailVerified = true;
                    member.IsMobileVerified = !Utils.IsEmailAddress(refNumber);
                    await unitOfWork.SaveChangesAsync();
                    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0);
                }
                else if(member?.OtpType != $"{EnumOtpType.register}")
                {
                    member.OtpCode = null;
                    member.OtpExpiry = Utils.GetDefaultDate().AddMinutes(10);
                    member.OtpToken = Utils.GenerateOtpToken(member.MemberId, member.Auth0Userid, referenceNumber,
                        otpType: (member.OtpType ?? $"{EnumOtpType.register}").ToEnum<EnumOtpType>(),
                        issuer: $"{config["JWT:OTP:Issuer"]}",
                        audience: $"{config["JWT:OTP:Audience"]}",
                        secretKey: $"{config["JWT:OTP:SecretKey"]}");

                    await unitOfWork.SaveChangesAsync();
                    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, new { refNumber = refNumber, otpToken = member.OtpToken });
                }

                return new ResponseModel<object> { Code = 400, Message = "Invalid opt code or expired." };
            }
            catch (Exception ex)
            {
                MobileErrorLog($"OtpVerify Ex => {refNumber} {otpCode}", ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion

        #region #get-okta-username
        public async Task<ResponseModel<object>> GetOktaUserName(string refNumber)
        {
            try
            {
                var phoneNumber = Utils.ReferenceNumber(refNumber);

                #region #OtpRateLimit
                if (IsOtpRateLimitExceeded(new OtpRateLimitModel
                {
                    UserIdentifier = phoneNumber,
                    RateLimitOtpType = RateLimitOtpType.GetOktaUserName,
                }) == true)
                {
                    return new ResponseModel<object>
                    {
                        Code = 429,
                        Message = "Too many attempts. Please try again in 5 minutes."
                    };
                }
                #endregion

                var validRefNumber = unitOfWork.GetRepository<Entities.Member>()
                    .Query(r => (r.Mobile == phoneNumber || r.Email == phoneNumber)).Any();
                if (validRefNumber == false) return new ResponseModel<object> { Code = 400, Message = "Email or mobile is invalid." };

                var isVerified = unitOfWork.GetRepository<Entities.Member>()
                    .Query(r => (r.Mobile == phoneNumber || r.Email == phoneNumber) && r.IsVerified == true).Any();
                if (isVerified == false) return new ResponseModel<object> { Code = 400, Message = "Your account is not verified." };

                var isActive = unitOfWork.GetRepository<Entities.Member>()
                    .Query(r => (r.Mobile == phoneNumber || r.Email == phoneNumber) && r.IsVerified == true && r.IsActive == true).Any();
                if (isActive == false) return new ResponseModel<object> { Code = 400, Message = "Your account is inactive." };

                var member = await unitOfWork.GetRepository<Entities.Member>().Query(
                        expression: r => (r.Mobile == phoneNumber || r.Email == phoneNumber) && r.IsActive == true && r.IsVerified == true).FirstOrDefaultAsync();

                var idValue = string.IsNullOrEmpty(member?.Nrc) ? 
                    (string.IsNullOrEmpty(member?.Passport) ? member?.Others : member?.Passport) : member?.Nrc;

                if (CheckAuthorization(null, idValue)?.Login == false)
                    return new ResponseModel<object> { Code = 403, Message = "Please verify policy status or contact your policyholder to submit service/claim requests." };


                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, member.OktaUserName);

            }
            catch(Exception ex) 
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);
            }
        }
        #endregion

        #region #forgot-password-otp
        public async Task<ResponseModel<object>> ForgotPassword(string refNumber)
        {
            try
            {

                Console.WriteLine($"ForgotPassword: {refNumber} Datetime: {Utils.GetDefaultDate()}");

                #region #OtpRateLimit
                if (IsOtpRateLimitExceeded(new OtpRateLimitModel 
                { 
                    UserIdentifier = refNumber, 
                    RateLimitOtpType = RateLimitOtpType.ForgotPassword 
                }) == true)
                {
                    return new ResponseModel<object>
                    {
                        Code = 429,
                        Message = "Too many attempts. Please try again in 5 minutes."
                    };
                }
                #endregion

                var referenceNumber = Utils.ReferenceNumber(refNumber);
                var member = await unitOfWork.GetRepository<Entities.Member>().Query(
                        expression: r => (r.Mobile == referenceNumber || r.Email == referenceNumber) && r.IsActive == true && r.IsVerified == true).FirstOrDefaultAsync();

                if (member == null
                    && string.IsNullOrEmpty(member?.Auth0Userid)) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);

                Console.WriteLine($"ForgotPassword => i am here");

                member.OtpToken = null;
                member.OtpType = $"{EnumOtpType.resetpassword}";
                member.OtpCode = Utils.GenerateOTP();
                member.OtpExpiry = Utils.GetDefaultDate().AddMinutes(10);
                await unitOfWork.SaveChangesAsync();

                await SendOTPCode(referenceNumber, string.Format($"{config["SmsPoh:OtpMessage"]}", member.OtpCode), $"{config["SmsPoh:Key"]}"
                    , member.Name, member.OtpCode);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, new { refNumber = refNumber });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ForgotPassword => Ex => {JsonConvert.SerializeObject(ex)}");
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);
            }
        }
        #endregion

        #region #reset-password
        public async Task<ResponseModel<object>> ResetPassword(ResetPasswordRequest model)
        {
            try
            {
                var memberId = Guid.Parse(GetValueFromJWTClaims(EnumOtpClaims.mid));
                var otpType = GetValueFromJWTClaims(EnumOtpClaims.type);
                var member = await unitOfWork.GetRepository<Entities.Member>().Query(
                        expression: r => r.MemberId == memberId && r.IsActive == true && r.IsVerified == true).FirstOrDefaultAsync();

                if (member == null) return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);
                else if (member?.OtpToken != AuthorizationOtpToken
                    || member?.OtpType != otpType) return new ResponseModel<object> { Code = 400, Message = "Invalid token." };

                

                var resetPassword = await oktaService.ResetPassword(member.Auth0Userid, model.ConfirmPassword);


                string json = JsonConvert.SerializeObject(resetPassword, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });

                MobileErrorLog("ResetPassword", $"oktaService.ResetPassword => {member.Auth0Userid} {model.ConfirmPassword}"
                    , $"resetPassword response => {json}", httpContext?.HttpContext.Request.Path);

                if (resetPassword?.Code == (int)ErrorCode.E0)
                {
                    member.OtpType = null;
                    member.OtpCode = null;
                    member.OtpExpiry = null;
                    member.OtpToken = null;
                    await unitOfWork.SaveChangesAsync();
                    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, member.OktaUserName);
                }
                else
                {
                    return new ResponseModel<object> { Code = 400, Message = resetPassword?.Message };
                }
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);
            }
        }
        #endregion

        #region #refresh-token
        public async Task<ResponseModel<object>> RefreshToken(RefreshTokenRequest model)
        {
            try 
            {
                MobileErrorLog($"RefreshToken Request Log Repo : {JsonConvert.SerializeObject(model)}",null,null, httpContext?.HttpContext.Request.Path);
                var response = await oktaService.RefreshToken(model);
                return response;
            }
            catch(Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion


        #region #otp-request
        public async Task<ResponseModel<object>> OtpRequest(string refNumber)
        {
            try
            {
                var referenceNumber = Utils.ReferenceNumber(refNumber);

                #region #OtpRateLimit
                if (IsOtpRateLimitExceeded(new OtpRateLimitModel
                {
                    UserIdentifier = referenceNumber,
                    RateLimitOtpType = RateLimitOtpType.OtpRequest,
                }) == true)
                {
                    return new ResponseModel<object>
                    {
                        Code = 429,
                        Message = "Too many attempts. Please try again in 5 minutes."
                    };
                }
                #endregion

                var member = await unitOfWork.GetRepository<Entities.Member>().Query(
                        expression: r => (r.Email == referenceNumber || r.Mobile == referenceNumber)).FirstOrDefaultAsync();

                if (member != null)
                {
                    member.OtpToken = null;
                    member.OtpType = EnumOtpType.register.ToString();
                    member.OtpTo = referenceNumber;
                    member.OtpCode = Utils.GenerateOTP();
                    member.OtpExpiry = Utils.GetDefaultDate().AddMinutes(10);
                    await unitOfWork.SaveChangesAsync();

                    await SendOTPCode(referenceNumber, string.Format($"{config["SmsPoh:OtpMessage"]}", member.OtpCode), $"{config["SmsPoh:Key"]}"
                        , member.Name, member.OtpCode);

                    return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, new { refNumber = refNumber });
                }

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E400);

            }
            catch (Exception ex)
            {
                MobileErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }

        ResponseModel<object> IAuthRepository.Logout()
        {
            try
            {
                var memberId = GetMemberIDFromToken()?.ToString();
                var deviceList = unitOfWork.GetRepository<Entities.MemberDevice>().Query(x => x.MemberId == memberId).ToList();

                deviceList?.ForEach(device => 
                {
                    unitOfWork.GetRepository<Entities.MemberDevice>().Delete(device);
                });

                unitOfWork.SaveChanges();
                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0);

            }
            catch (Exception ex)
            {
                MobileErrorLog("Logout", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E500);
            }
        }
        #endregion
    }
}
