using Hangfire;
using Microsoft.EntityFrameworkCore;
using aia_core.Services;
using aia_core.UnitOfWork;
using Newtonsoft.Json;
using aia_core.Repository.Mobile;
using Microsoft.Extensions.DependencyInjection;
using static System.Net.Mime.MediaTypeNames;
using FirebaseAdmin.Messaging;
using aia_core.Repository.Cms;
using CsvHelper;
using FastMember;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using aia_core.Entities;
using aia_core.Model.Mobile.Response;
using System.Security.Claims;
using Microsoft.Data.SqlClient.DataClassification;
using System.Linq;
using System.Reflection;
using aia_core.Repository;
using Microsoft.AspNetCore.Http;
using DocumentFormat.OpenXml.Spreadsheet;
using Irony;
using System.Net.Http;
using aia_core.Model.Mobile.Response.AiaILApiResponse;
using Hangfire.States;
using aia_core.Model.Mobile.Request;
using DocumentFormat.OpenXml.Vml.Office;

using aia_core.Model.Mobile.Servicing.Data.Response;


namespace aia_core.RecurringJobs
{

    public interface IRecurringJobRunner
    {
        Task UpdateClaimStatus(bool isRunFromSchedule, string otp = "");

        Task SendUpcomingPremiumsNotification();
        Task SendClaimNotification();
        Task SendServiceNotification();

        Task UpdateMemberDataPullFromAiaCoreTables();

        Task UpdateMemberInfoInClaimTran();
        Task MigrateUserAccount();

        Task CheckBeneficiaryStatusAndSendNoti();

        Task UploadDefaultCmsImages();

        Task SendNotiFromCms(Guid? notiId);

        Task DeleteNotiFromCms(Guid? notiId);

        Task SendClaimSms();

        Task SendServicingSms();
    }
    public class RecurringJobRunner : IRecurringJobRunner
    {
        private readonly IErrorCodeProvider errorCodeProvider;
        private readonly IUnitOfWork<Entities.Context> unitOfWork;
        private readonly INotificationService notificationService;
        private readonly IServiceProvider serviceProvider;
        private readonly ITemplateLoader templateLoader;
        private readonly IConfiguration config;
        private readonly IOktaService oktaService;
        private readonly BaseRepository baseRepository;

        private readonly IServiceScopeFactory serviceFactory;
        protected readonly IAzureStorageService azureStorage;


        public RecurringJobRunner(IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork,
            INotificationService notificationService, IServiceProvider serviceProvider, ITemplateLoader templateLoader, IConfiguration config,IOktaService oktaService
            , IAzureStorageService azureStorage
            , BaseRepository baseRepository, IServiceScopeFactory serviceFactory)
        {
            this.errorCodeProvider = errorCodeProvider;
            this.unitOfWork = unitOfWork;
            this.notificationService = notificationService;
            this.serviceProvider = serviceProvider;
            this.templateLoader = templateLoader;
            this.config = config;
            this.oktaService = oktaService;
            this.baseRepository = baseRepository;
            this.serviceFactory = serviceFactory;
            this.azureStorage = azureStorage;

        }

        public async Task SendClaimNotification()
        {
            Console.WriteLine($"SendClaimNotification Job Started At {Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)}");

            try
            {
                SaveLog(new SaveLogModel { LogMessage = "SendClaimNotification" });

                var claimStatusCodeList = new string[]
                        {
                        EnumClaimStatus.AL.ToString(), /*EnumClaimStatus.AL.ToString(),*/ //Approved
                        EnumClaimStatus.FU.ToString(), //Followed-up
                        EnumClaimStatus.PD.ToString(), //Paid
                        EnumClaimStatus.CS.ToString(), //Closed
                        EnumClaimStatus.WD.ToString(), //Withdrawn
                        EnumClaimStatus.RJ.ToString(), //Rejected 
                        };

                var statusChangeList = unitOfWork.GetRepository<Entities.ClaimsStatusUpdate>()
                    .Query(x => x.IsDone == false && claimStatusCodeList.Contains(x.NewStatus))
                    .OrderBy(x => x.CreatedDate)
                    .ToList();

                foreach (var statusChange in statusChangeList)
                {
                    var claimTran = unitOfWork.GetRepository<Entities.ClaimTran>()
                        .Query(x => x.ClaimId == new Guid(statusChange.ClaimId))
                        .FirstOrDefault();

                    if (claimTran != null && claimTran.AppMemberId != null)
                    {
                        notificationService.SendClaimNoti(claimTran.AppMemberId.Value, new Guid(statusChange.ClaimId)
                                        , ((EnumClaimStatus)(Enum.Parse(typeof(EnumClaimStatus), statusChange.NewStatus))), claimTran?.ClaimType);

                        
                        
                    }

                    statusChange.IsDone = true;
                    unitOfWork.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                SaveLog(new SaveLogModel { LogMessage = "SendClaimNotification Ex", Exception = $"{JsonConvert.SerializeObject(ex)}" });
            }

            Console.WriteLine($"SendClaimNotification Job Finished At {Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)}");
        }

        public async Task SendServiceNotification()
        {
            Console.WriteLine($"SendServiceNotification Job Started At {Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)}");

            try
            {

                SaveLog(new SaveLogModel { LogMessage = "SendServiceNotification" });

                using (var scope = this.serviceFactory.CreateScope())
                {
                    var scopeUnitOfWork = scope.ServiceProvider.GetService<IUnitOfWork<aia_core.Entities.Context>>();

                    // NOT INCLUDED BENEFICIARY REQUEST
                    // there is another schedule CheckBeneficiaryStatusAndSendNoti
                    //
                    var statusChangeList = scopeUnitOfWork.GetRepository<Entities.ServiceStatusUpdate>()
                    .Query(x => x.IsDone == false
                    && x.ServiceType != EnumServiceType.BeneficiaryInformation.ToString()
                    && x.NewStatus != "Pending" 
                    && x.NewStatus != EnumServicingStatus.Received.ToString()
                    && x.ServiceID != null 
                    && x.ServiceMainID != null)
                    .OrderBy(x => x.CreatedDate)
                    .ToList();


                    foreach (var statusChange in statusChangeList)
                    {
                        try
                        {
                            EnumServicingStatus enumStatusValue = (EnumServicingStatus)Enum.Parse(typeof(EnumServicingStatus), statusChange.NewStatus);
                            EnumServiceType enumTypeValue = (EnumServiceType)Enum.Parse(typeof(EnumServiceType), statusChange.ServiceType);

                            if (statusChange.MemberID != null)
                            {
                                notificationService.SendServicingNoti(statusChange.MemberID.Value, statusChange.ServiceID.Value, enumStatusValue
                                                    , enumTypeValue, statusChange.PolicyNumber);
                            }


                            statusChange.IsDone = true;
                            scopeUnitOfWork.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            //SaveLog(new SaveLogModel { 
                            //    LogMessage = $"SendServiceNotification Ex > {statusChange.ServiceID}", 
                            //    ExceptionMessage = $"{JsonConvert.SerializeObject(statusChange)}",
                            //    Exception = $"{JsonConvert.SerializeObject(ex)}" 
                            //});
                        }
                        
                    }
                }

                    

            }
            catch (Exception ex)
            {
                SaveLog(new SaveLogModel { LogMessage = "SendServiceNotification Ex", Exception = $"{JsonConvert.SerializeObject(ex)}" });
            }

            Console.WriteLine($"SendServiceNotification Job Finished At {Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)}");
        }

        public async Task SendUpcomingPremiumsNotification()
        {
            Console.WriteLine($"SendUpcomingPremiumsNotification Job Started At {Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)}");

            try
            {

                SaveLog(new SaveLogModel { LogMessage = "SendUpcomingPremiumsNotification" });

                var memberIdList = unitOfWork.GetRepository<Entities.Member>()
                    .Query(x => x.IsVerified == true && x.IsActive == true)
                    .Select(x => x.MemberId)
                    .ToList();

                memberIdList?.ForEach(memberId =>
                {
                    var clientNoList = baseRepository.GetClientNoListByIdValue(memberId);

                    if (clientNoList != null)
                    {
                        //var upcoming = Utils.GetDefaultDate().Date.AddDays(DefaultConstants.LimitDaysForUpcomingAndOverdueForULI);
                        //var overdued = Utils.GetDefaultDate().Date.AddDays((-1) * (DefaultConstants.LimitDaysForUpcomingAndOverdueForULI));

                        var upcoming = Utils.GetDefaultDate().Date.AddDays(DefaultConstants.LimitDaysForUpcomingAndOverdue);
                        var overdued = Utils.GetDefaultDate().Date.AddDays((-1) * (DefaultConstants.LimitDaysForUpcomingAndOverdue));
                        Console.WriteLine($"{DateTime.Now.ToString(DefaultConstants.DateTimeFormat)} " +
                            $"SendUpcomingPremiumsNotification => upcoming {upcoming} overdued {overdued}");

                        var query = unitOfWork.GetRepository<Entities.Policy>()
                            .Query(x => x.ProductType != "PAI" /*Mya Kyay Hmone Request 12/03/2024*/
                            ////&& x.AcpModeFlag != "1" /*MKM & TMA Request 02/07/2024 ms team meeting */ MKM asked to use to move this checking on 30/05/2025 Friday!
                            && (clientNoList.Contains(x.PolicyHolderClientNo) || clientNoList.Contains(x.InsuredPersonClientNo))
                            && (x.PaidToDate >= overdued && x.PaidToDate <= upcoming)
                            && Utils.GetActivePolicyStatus().Contains(x.PolicyStatus)
                            && x.PaidToDate != null);

                        var excludedProductCodeList = unitOfWork
                                 .GetRepository<PolicyExcludedList>()
                                 .Query()
                                 .Select(x => x.ProductCode)
                                 .ToList();

                        if (excludedProductCodeList != null && excludedProductCodeList.Any())
                        {
                            query = query.Where(x => excludedProductCodeList.Contains(x.ProductType) == false);
                        }

                        var policies = query
                            .ToList(); 

                        policies?.ForEach(policy =>
                        {
                            var isDued = false;
                            var isUpcoming = false;
                            var dueInDays = 0;

                            if (policy.PaidToDate != null)
                            {
                                var actualDueInDays = Utils.GetNumberOfDaysForPolicyDue(policy.PaidToDate.Value);

                                //if(policy.ProductType == "ULI")
                                //{
                                //    if (actualDueInDays < 0
                                //    && actualDueInDays >= ((-1) * DefaultConstants.LimitDaysForUpcomingAndOverdueForULI))
                                //    {
                                //        isDued = true;
                                //    }


                                //    if (actualDueInDays >= 0
                                //        && actualDueInDays <= DefaultConstants.LimitDaysForUpcomingAndOverdueForULI)
                                //    {
                                //        isUpcoming = true;
                                //    }
                                //}
                                //else
                                //{
                                    if (actualDueInDays < 0
                                    && actualDueInDays >= ((-1) * DefaultConstants.LimitDaysForUpcomingAndOverdue))
                                    {
                                        isDued = true;
                                    }


                                    if (actualDueInDays >= 0
                                        && actualDueInDays <= DefaultConstants.LimitDaysForUpcomingAndOverdue)
                                    {
                                        isUpcoming = true;
                                    }
                                //}

                                


                                dueInDays = (actualDueInDays < 0)
                                    ? ((-1) * actualDueInDays) : actualDueInDays;

                                Console.WriteLine($"{DateTime.Now.ToString(DefaultConstants.DateTimeFormat)} SendUpcomingPremiumsNotification => " +
                                    $"PolicyNo {policy.PolicyNo} PaidToDate {policy.PaidToDate} isDued {isDued} isUpcoming {isUpcoming} dueInDays {dueInDays}");

                                //if ((isDued || isUpcoming) && 
                                //(
                                //(policy.ProductType != "ULI" && (dueInDays == ((-1) * DefaultConstants.LimitDaysForUpcomingAndOverdue) || dueInDays == 0 || dueInDays == DefaultConstants.LimitDaysForUpcomingAndOverdue))
                                //|| 
                                //(policy.ProductType == "ULI" && (dueInDays == ((-1) * DefaultConstants.LimitDaysForUpcomingAndOverdueForULI) || dueInDays == 0 || dueInDays == ((-1) * DefaultConstants.LimitDaysForUpcomingAndOverdueForULI)))
                                //)
                                //)
                                if ((isDued || isUpcoming) && (dueInDays == -7 || dueInDays == 0 || dueInDays == 7))
                                {

                                    var messageCode = EnumPushMessageCode.UpcomingPremiums.ToString();
                                    var notiMsgListJson = templateLoader.GetNotiMsgListJson();
                                    var msg = notiMsgListJson[messageCode];
                                    var formatMsg = "";

                                    if (isUpcoming)
                                    {
                                        formatMsg = string.Format(msg.Message, Convert.ToDouble(policy.PremiumDue), policy.PolicyNo, "Due in " + (dueInDays == 0 ? "today" : dueInDays + " days") + ".");
                                    }
                                    if (isDued)
                                    {
                                        formatMsg = string.Format(msg.Message, Convert.ToDouble(policy.PremiumDue), policy.PolicyNo, "overdued " + dueInDays + " days.");
                                    }


                                    if(!string.IsNullOrEmpty(formatMsg))
                                    {
                                        var notiId = Guid.NewGuid();

                                        var notification = new Entities.MemberNotification()
                                        {
                                            IsDeleted = false,
                                            IsRead = false,
                                            Id = notiId,
                                            Type = EnumNotificationType.Others.ToString(),
                                            IsSytemNoti = true,
                                            SystemNotiType = EnumSystemNotiType.UpcomingPremiums.ToString(),
                                            MemberId = memberId,
                                            CreatedDate = Utils.GetDefaultDate(),
                                            Message = formatMsg,
                                            PremiumPolicyNo = policy.PolicyNo,
                                        };

                                        unitOfWork.GetRepository<Entities.MemberNotification>().Add(notification);
                                        unitOfWork.SaveChanges();

                                        notificationService.SendNotification(new NotificationMessage
                                        {
                                            MemberId = memberId,
                                            NotificationType = EnumNotificationType.Others,
                                            IsSytemNoti = true,
                                            SystemNotiType = EnumSystemNotiType.UpcomingPremiums,
                                            Message = formatMsg,
                                            Title = msg?.Title,
                                            ImageUrl = msg?.ImageUrl,
                                            PolicyNumber = policy.PolicyNo,
                                        });
                                    }
                                    
                                }
                            }




                        });
                    }
                });
            }
            catch (Exception ex)
            {
                
                SaveLog(new SaveLogModel { LogMessage = "SendUpcomingPremiumsNotification Ex", Exception = $"{JsonConvert.SerializeObject(ex)}" });
            }

            Console.WriteLine($"SendUpcomingPremiumsNotification Job Finished At {Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)}");
        }


       

        public static DataTable ListToDataTable<T>(List<T> list, string _tableName)
        {
            DataTable dt = new DataTable(_tableName);

            foreach (PropertyInfo info in typeof(T).GetProperties())
            {
                dt.Columns.Add(new DataColumn(info.Name, Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType));
            }
            foreach (T t in list)
            {
                DataRow row = dt.NewRow();
                foreach (PropertyInfo info in typeof(T).GetProperties())
                {
                    row[info.Name] = info.GetValue(t, null) ?? DBNull.Value;
                }
                dt.Rows.Add(row);
            }
            return dt;
        }

        public void SaveLog(SaveLogModel saveLogModel)
        {
            try
            {
                unitOfWork.GetRepository<Entities.ErrorLogMobile>().Add(new Entities.ErrorLogMobile
                {
                    ID = Guid.NewGuid(),
                    LogMessage = saveLogModel.LogMessage,
                    ExceptionMessage = saveLogModel.ExceptionMessage,
                    Exception = saveLogModel.Exception,
                    EndPoint = saveLogModel.EndPoint,
                    LogDate = Utils.GetDefaultDate(),
                    UserID = ""
                });
                unitOfWork.SaveChanges();
            }
            catch { }
            
        }

        public async Task UpdateMemberDataPullFromAiaCoreTables()
        {

            Console.WriteLine($"UpdateMemberDataPullFromAiaCoreTables START => {Utils.GetDefaultDate()}");


            
            try
            {
                var memberList = unitOfWork.GetRepository<Entities.Member>()
                    .Query(x => x.IsActive == true && x.IsVerified == true)                    
                    .ToList();

                memberList?.ForEach(member =>
                {
                    (string? membertype, string? memberID, string? groupMemberId) clientInfo = GetClientInfo(member.MemberId);

                    member.MemberType = clientInfo.membertype;
                    member.GroupMemberID = clientInfo.groupMemberId;
                    member.IndividualMemberID = clientInfo.memberID;
                    unitOfWork.SaveChanges();

                    try
                    {
                        #region #Address & All Client No List
                        var IdValue = (!string.IsNullOrEmpty(member.Nrc) ? member.Nrc :
                            !string.IsNullOrEmpty(member.Passport) ? member.Passport : member.Others).ToLower();

                        var clientList = unitOfWork.GetRepository<Entities.Client>()
                        .Query(x => x.Nrc == IdValue || x.PassportNo == IdValue || x.Other == IdValue)
                        .ToList();

                        if (clientList?.Any() == true)
                        {
                            
                                member.Country = clientList.FirstOrDefault()?.Address6;

                                member.Country = unitOfWork.GetRepository<Entities.Country>()
                                .Query(x => x.code == member.Country)
                                .Select(x => x.description)
                                .FirstOrDefault();

                                member.Province = clientList.FirstOrDefault()?.Address5;
                                member.District = clientList.FirstOrDefault()?.Address4;
                                member.Township = clientList.FirstOrDefault()?.Address3;

                                var clientNoList = clientList.Select(x => x.ClientNo).ToList();
                                member.AllClientNoListString = string.Join(",", clientNoList);
                                unitOfWork.SaveChanges();


                                var policyInfoList = unitOfWork.GetRepository<Entities.Policy>()
                            .Query(x => clientNoList.Contains(x.PolicyHolderClientNo) || clientNoList.Contains(x.InsuredPersonClientNo))
                            .Select(x => new { x.ProductType, x.PolicyStatus })
                            .ToList();

                                if (policyInfoList?.Any() == true)
                                {
                                    var productCodeListAsString = string.Join(",", policyInfoList.Select(x => x.ProductType).ToList().Distinct());
                                    var policyStatusListAsString = string.Join(",", policyInfoList.Select(x => x.PolicyStatus).ToList().Distinct());
                                    member.ProductCodeList = productCodeListAsString;
                                    member.PolicyStatusList = policyStatusListAsString;

                                    unitOfWork.SaveChanges();
                                }
                                
                            

                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"UpdateMemberDataPullFromAiaCoreTables EX1 => {member.MemberId} {Utils.GetDefaultDate()} {JsonConvert.SerializeObject(ex)}");

                    }




                });


            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateMemberDataPullFromAiaCoreTables EX => {Utils.GetDefaultDate()} {JsonConvert.SerializeObject(ex)}");

            }

			Console.WriteLine($"UpdateMemberDataPullFromAiaCoreTables END => {Utils.GetDefaultDate()}");

        }

        public (string? membertype, string? memberID, string? groupMemberId) GetClientInfo(Guid? appMemberId)
        {

            var idValue = unitOfWork.GetRepository<Entities.Member>()
                .Query(x => x.MemberId == appMemberId)
                .Select(x => new { x.Nrc, x.Passport, x.Others })
                .FirstOrDefault();

            if (idValue != null)
            {

                var clientNoList = unitOfWork.GetRepository<Entities.Client>()
                       .Query(x => (!string.IsNullOrEmpty(idValue.Nrc) && x.Nrc == idValue.Nrc)
                       || (!string.IsNullOrEmpty(idValue.Passport) && x.PassportNo == idValue.Passport)
                       || (!string.IsNullOrEmpty(idValue.Others) && x.Other == idValue.Others))
                       .Select(x => x.ClientNo).ToList();

                var client = unitOfWork.GetRepository<Entities.Client>()
                    .Query(x => clientNoList.Contains(x.ClientNo))
                    .ToList();

                var isruby = client.Any(x => x.VipFlag == "Y");
                var membertype = isruby == true ? EnumIndividualMemberType.Ruby.ToString() : EnumIndividualMemberType.Member.ToString();


                var groupClientNo = unitOfWork.GetRepository<Entities.Policy>()
                   .Query(x => (clientNoList.Contains(x.PolicyHolderClientNo) || clientNoList.Contains(x.InsuredPersonClientNo))
                   && x.PolicyNo.Length > DefaultConstants.IndividualPolicyNoLength)
                   .Select(x => x.PolicyHolderClientNo)
                   .FirstOrDefault();

                return (membertype, clientNoList?.FirstOrDefault(), groupClientNo);
            }


            return (null, null, null);

        }

        public async Task UpdateMemberInfoInClaimTran()
        {
            try
            {
                var claimList = unitOfWork.GetRepository<Entities.ClaimTran>()
                    .Query(x => x.AppMemberId != null && (x.MemberType == null || x.IndividualMemberID == null))                   
                    .ToList();

                claimList?.ForEach(claimTran =>
                {
                    (string? membertype, string? memberID, string? groupMemberId) clientInfo = GetClientInfo(claimTran.AppMemberId);

                    var memberInfo = unitOfWork.GetRepository<Entities.Member>()
                    .Query(x => x.MemberId == claimTran.AppMemberId)
                    .Select(x => new { x.Name, x.Mobile })
                    .FirstOrDefault();

                    claimTran.MemberType = clientInfo.membertype;
                    claimTran.GroupMemberID = clientInfo.groupMemberId;
                    claimTran.IndividualMemberID = clientInfo.memberID;
                    claimTran.MemberName = memberInfo?.Name;
                    claimTran.MemberPhone = memberInfo?.Mobile;

                    unitOfWork.SaveChanges();
                });


            }
            catch (Exception ex)
            {
                SaveLog(new SaveLogModel { LogMessage = "UpdateMemberInfoInClaimTran Ex", Exception = $"{JsonConvert.SerializeObject(ex)}" });
            }
        }

        #region #migrate-account
        public async Task MigrateUserAccount()
        {
            try
            {
                // string groupID = config["Okta:GroupID"];
                // List<UsersTemp> usersTemps = unitOfWork.GetRepository<UsersTemp>().Query(x=> x.is_done != true).ToList();
                // foreach (var user in usersTemps)
                // {
                //     var checkClient = unitOfWork.GetRepository<Entities.Client>().Query(
                //     expression: r => r.Nrc == user.nrc || r.PassportNo == user.passport || r.Other == user.others
                //     ).FirstOrDefault();
					
                //     if(checkClient == null) 
                //     {
                //         user.is_done = true;
                //         user.migrate_date = Utils.GetDefaultDate();
                //         user.migrate_status = "fail";
                //         user.migrate_log = "No client found";
                //         unitOfWork.SaveChanges();
                //         continue;
                //     }

                //     var checkMember = unitOfWork.GetRepository<Entities.Member>().Query(
                //     expression: r => r.Nrc == user.nrc || r.Passport == user.passport || r.Others == user.others
                //     ).FirstOrDefault();

                //     if(checkMember != null) 
                //     {
                //         user.is_done = true;
                //         user.migrate_date = Utils.GetDefaultDate();
                //         user.migrate_status = "fail";
                //         user.migrate_log = "Already registered user";
                //         unitOfWork.SaveChanges();
                //         continue;
                //     }

                //     var hasEmail = unitOfWork.GetRepository<Entities.Member>().Query(expression: r => r.Email == user.email).Any();
                //     if (hasEmail) 
                //     {
                //         user.is_done = true;
                //         user.migrate_date = Utils.GetDefaultDate();
                //         user.migrate_status = "fail";
                //         user.migrate_log = "This email has already been registered.";
                //         unitOfWork.SaveChanges();
                //         continue;
                //     }
                //     var hasPhone = unitOfWork.GetRepository<Entities.Member>().Query(expression: r =>  r.Mobile == user.phone_no).Any();
                //     if (hasPhone) 
                //     {
                //         user.is_done = true;
                //         user.migrate_date = Utils.GetDefaultDate();
                //         user.migrate_status = "fail";
                //         user.migrate_log = "This mobile number has already been registered.";
                //         unitOfWork.SaveChanges();
                //         continue;
                //     }

                //     aia_core.Entities.Member member = new aia_core.Entities.Member();
                //     member.OktaUserName = Utils.GenerateOktaUserName();

                //     RegisterRequest registerRequest = new RegisterRequest();
                //     registerRequest.FullName = user.name;
                //     registerRequest.Dob = user.date_of_birth;
                //     registerRequest.Gender = user.gender=="M"?EnumGender.Male:EnumGender.Female;
                //     registerRequest.Email = user.email;
                //     registerRequest.Phone = user.phone_no;
                //     registerRequest.Password = user.password;
                //     registerRequest.ConfirmPassword = user.password;

                //     var payload = new
                //     {
                //         profile = new
                //         {
                //             firstName = registerRequest.FullName,
                //             lastName = registerRequest.FullName,
                //             email = registerRequest.Email,
                //             mobilePhone = registerRequest.Phone,
                //             login = member.OktaUserName,
                //             locale = "my_MM",
                //         },
                //         credentials = new
                //         {
                //             password = new { value = registerRequest.ConfirmPassword },
                //             provider = new { type = "OKTA", name = "OKTA" }
                //         },
                //         groupIds = new string[] { groupID }
                //     };
                //     string okta_register_request = System.Text.Json.JsonSerializer.Serialize(payload);

                //     var oktaRegister = await oktaService.RegisterUser(member.OktaUserName, registerRequest);
                //     if (oktaRegister.Code == (long)ErrorCode.E0)
                //     {
                //         member.MemberId = Guid.NewGuid();
                //         member.Name = user.name;
                //         member.Email = user.email;
                //         member.Mobile = user.phone_no;
                //         member.Dob = user.date_of_birth;
                //         member.Gender = user.gender=="M"?"Male":"Female";
                //         member.RegisterDate = Utils.GetDefaultDate();
                //         member.Auth0Userid = oktaRegister.Data.id;

                //         member.Nrc = user.nrc;
                //         member.Passport = user.passport;
                //         member.Others = user.others;

                //         member.IsVerified = true;
                //         member.IsMobileVerified = true;
                //         member.IsEmailVerified = true;

                //         user.is_done = true;
                //         user.migrate_date = Utils.GetDefaultDate();
                //         user.migrate_status = "success";
                //         user.migrate_log = $"Success | OktaUserName : {member.OktaUserName}";
                //         user.okta_register_request = okta_register_request;
                //         unitOfWork.GetRepository<aia_core.Entities.Member>().Add(member);
                //         unitOfWork.SaveChanges();
                //         continue;
                //     }
                //     else
                //     {
                //         user.is_done = true;
                //         user.migrate_date = Utils.GetDefaultDate();
                //         user.migrate_status = "fail";
                //         user.migrate_log = $"Okta API fail | Okta response : {JsonConvert.SerializeObject(oktaRegister)}";
                //         user.okta_register_request = okta_register_request;
                //         unitOfWork.SaveChanges();
                //         continue;
                //     }
                // }

            }
            catch (Exception ex)
            {
            }
        }

        public async Task CheckBeneficiaryStatusAndSendNoti()
        {
            Console.WriteLine($"CheckBeneficiaryStatusAndSendNoti Job Started At {Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)}");

            try
            {
                var scheduleId = Guid.NewGuid();

                var requestList = unitOfWork.GetRepository<Entities.ServiceMain>()
                    .Query(x => x.ServiceStatus == EnumServicingStatus.Received.ToString()
                    && x.ServiceType == EnumServiceType.BeneficiaryInformation.ToString())
                    .OrderBy(x => x.CreatedDate)
                    .ToList();

                requestList?.ForEach(request => 
                { 

                    var itemList = unitOfWork.GetRepository<Entities.ServiceBeneficiaryShareInfo>().
                    Query(x => x.ServiceBeneficiaryID == request.ServiceID)
                    .ToList();

                    var checkList = new List<BeneficiaryCheckList>();
                    itemList?.ForEach(item => 
                    {
                        //Remove

                        if (item.Type == "Remove")
                        {
                           var IsExisted = unitOfWork.GetRepository<Entities.Beneficiary>()
                            .Query(x => x.BeneficiaryClientNo == item.ClientNo && x.PolicyNo == request.PolicyNumber)
                            .Any();

                            checkList.Add(new Entities.BeneficiaryCheckList
                            {
                                ScheduleId = scheduleId,
                                Id = Guid.NewGuid(),
                                CreatedOn = Utils.GetDefaultDate(),
                                ServiceId = item.ServiceBeneficiaryID,
                                ShareItemId = item.ID,
                                Type = "Remove",
                                IsCompleted = !IsExisted,
                                ClientNo = item.ClientNo,                                
                            });
                        }

                        // Update
                        if (item.Type == "Update")
                        {
                            var beneficiary = unitOfWork.GetRepository<Entities.Beneficiary>()
                            .Query(x => x.BeneficiaryClientNo == item.ClientNo && x.PolicyNo == request.PolicyNumber)
                            .FirstOrDefault();

                            if (beneficiary != null)
                            {
                                var _IsUpdatePercentage = false;
                                var _IsUpdatePercentageDone = false;
                                var _IsUpdateRelationship = false;
                                var _IsUpdateRelationshipDone = false;

                                var updateItemList = new List<Entities.BeneficiaryCheckList>();

                                if (item.NewPercentage != null && item.NewPercentage > 0)
                                {
                                    _IsUpdatePercentage = true;
                                    _IsUpdatePercentageDone = (item.NewPercentage == beneficiary.Percentage);


                                    updateItemList.Add(new BeneficiaryCheckList 
                                    { 
                                        IsCompleted = _IsUpdatePercentageDone,
                                        Id = Guid.NewGuid(),
                                    });
                                }

                                if (!string.IsNullOrEmpty(item.NewRelationShipCode))
                                {
                                    _IsUpdateRelationship = true;
                                    _IsUpdateRelationshipDone = (item.NewRelationShipCode.ToLower() == beneficiary.Relationship?.ToLower());

                                    updateItemList.Add(new BeneficiaryCheckList
                                    {
                                        IsCompleted = _IsUpdateRelationshipDone,
                                        Id = Guid.NewGuid(),
                                    });
                                }

                                var IsCompleted = updateItemList.Where(x => x.IsCompleted == true).Count() == updateItemList.Count;

                                checkList.Add(new Entities.BeneficiaryCheckList
                                {
                                    ScheduleId = scheduleId,
                                    ServiceId = item.ServiceBeneficiaryID,
                                    ShareItemId = item.ID,
                                    Type = "Update",
                                    IsCompleted = IsCompleted,
                                    ClientNo = item.ClientNo,
                                    UpdateValue = $"NewPercentage => {item.NewPercentage} NewRelationship => {item.NewRelationShipCode}",
                                    UpdateValueType = $"PercentageUpdated? => {_IsUpdatePercentage} RelationshipUpdated? => {_IsUpdateRelationship}",
                                    Id = Guid.NewGuid(),
                                    CreatedOn = Utils.GetDefaultDate(),
                                });
                            }
                            else
                            {
                                #region #iOs Screen Flaw Temporary Fix
                                if (string.IsNullOrEmpty(item.NewRelationShipCode) && string.IsNullOrEmpty(item.OldRelationShipCode)
                                && (item.NewPercentage == 0 || item.NewPercentage == null) && item.OldPercentage == 100) // iOs Screen Flow Temporary Fix
                                {
                                    checkList.Add(new Entities.BeneficiaryCheckList
                                    {
                                        ScheduleId = scheduleId,
                                        ServiceId = item.ServiceBeneficiaryID,
                                        ShareItemId = item.ID,
                                        Type = "Update",
                                        IsCompleted = true,
                                        ClientNo = item.ClientNo,
                                        UpdateValue = $"NewPercentage => {item.NewPercentage} NewRelationship => {item.NewRelationShipCode}",
                                        UpdateValueType = $"iOs Screen Flaw Temporary Fix!!",
                                        Id = Guid.NewGuid(),
                                        CreatedOn = Utils.GetDefaultDate(),
                                    });
                                }

                                #endregion
                            }
                        }

                        // New
                        if (item.Type == "New")
                        {
                            if (!string.IsNullOrEmpty(item.ClientNo)) // New But Existing
                            {
                                var _IsDoneAdded = unitOfWork.GetRepository<Entities.Beneficiary>()
                                .Query(x => x.BeneficiaryClientNo == item.ClientNo && x.PolicyNo == request.PolicyNumber)
                                .Any();

                                checkList.Add(new Entities.BeneficiaryCheckList
                                {
                                    ScheduleId = scheduleId,
                                    ServiceId = item.ServiceBeneficiaryID,
                                    ShareItemId = item.ID,
                                    Type = "New",
                                    IsCompleted = _IsDoneAdded,
                                    ClientNo = item.ClientNo,
                                    UpdateValueType = "New But Existing",
                                    Id = Guid.NewGuid(),
                                    CreatedOn = Utils.GetDefaultDate(),
                                });
                            }
                            else // Brand New
                            {
                                var idValue = unitOfWork.GetRepository<Entities.ServiceBeneficiaryPersonalInfo>()
                                .Query(x => x.ServiceBeneficiaryID == item.ServiceBeneficiaryID && x.ServiceBeneficiaryShareID == item.ID
                                && x.IsNewBeneficiary == true && string.IsNullOrEmpty(x.ClientNo))
                                .Select(x => x.IdValue)
                                .FirstOrDefault();

                                if (!string.IsNullOrEmpty(idValue))
                                {
                                    var newAddedClientNoList = unitOfWork.GetRepository<Entities.Client>()
                                    .Query(x => x.Nrc == idValue || x.PassportNo == idValue || x.Other == idValue)
                                    .Select(x => x.ClientNo)
                                    .ToList();
                                    
                                    var IsDoneAddedBeneficiary = unitOfWork.GetRepository<Entities.Beneficiary>()
                                        .Query(x => newAddedClientNoList.Contains(x.BeneficiaryClientNo) && x.PolicyNo == request.PolicyNumber)
                                        .FirstOrDefault();

                                    checkList.Add(new Entities.BeneficiaryCheckList
                                    {
                                        ScheduleId = scheduleId,
                                        ServiceId = item.ServiceBeneficiaryID,
                                        ShareItemId = item.ID,
                                        Type = "New",
                                        IsCompleted = (IsDoneAddedBeneficiary != null),
                                        ClientNo = IsDoneAddedBeneficiary?.BeneficiaryClientNo,
                                        UpdateValueType = "Brand New",
                                        Id = Guid.NewGuid(),
                                        CreatedOn = Utils.GetDefaultDate(),
                                    });
                                }
                            }
                        }


                    });


                    if (itemList != null && checkList != null)
                    {
                        try
                        {
                            unitOfWork.GetRepository<Entities.BeneficiaryCheckList>().Add(checkList);
                            unitOfWork.SaveChanges();
                        }
                        catch { }
                        

                        var completedCount = checkList.Where(x => x.IsCompleted == true).Count();

                        if (itemList.Count == completedCount)
                        {
                            request.ServiceStatus = EnumServicingStatus.Approved.ToString();
                            request.UpdatedOn = Utils.GetDefaultDate();
                            request.UpdateChannel = "Job";
                            unitOfWork.SaveChanges();

                            try
                            {
                                notificationService.SendServicingNoti(request.LoginMemberID.Value, request.ServiceID.Value, EnumServicingStatus.Approved
                                                        , EnumServiceType.BeneficiaryInformation, request.PolicyNumber);
                            }
                            catch (Exception ex)
                            {
                                SaveLog(new SaveLogModel
                                {
                                    LogMessage = $"CheckBeneficiaryStatusAndSendNoti Send Not Ex",
                                    ExceptionMessage = $"LoginMemberID => {request.LoginMemberID.Value} ServiceID => {request.ServiceID.Value}",
                                    Exception = $"{JsonConvert.SerializeObject(ex)}"
                                });
                            }
                        }
                    }
                });
            }
            catch 
            {
            
            }

            Console.WriteLine($"CheckBeneficiaryStatusAndSendNoti Job Finished At {Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)}");
        }

        public async Task UploadDefaultCmsImages()
        {
            try
            {
                var defaultCmsImage = unitOfWork.GetRepository<Entities.DefaultCmsImage>().Query().FirstOrDefault();
                if (defaultCmsImage == null)
                {
                    // Read the file into a byte array
                    byte[] fileBytes = System.IO.File.ReadAllBytes("default-banklogo.png");

                    // Create an IFormFile instance
                    IFormFile defaultBankLogo = new FormFile(new MemoryStream(fileBytes), 0, fileBytes.Length, "default-banklogo.png", "default-banklogo.png");



                    var defaultBankLogoName = $"{Utils.GetDefaultDate().Ticks}-{defaultBankLogo.FileName}";
                    var result = await azureStorage.UploadAsync(defaultBankLogoName, defaultBankLogo);

                    

                    if (result.Code == 200)
                    {
                        unitOfWork.GetRepository<Entities.DefaultCmsImage>().Add(new DefaultCmsImage
                        {
                            id = Guid.NewGuid(),
                            image_for = "bank",
                            image_url = defaultBankLogoName,
                            created_at = Utils.GetDefaultDate(),
                        });

                        unitOfWork.SaveChanges();
                    }

                    
                }
                
            }
            catch(Exception ex)
            {
                Console.WriteLine($"UploadDefaultCmsImages Ex => {ex.Message} {ex.StackTrace}");
            }
        }
        #endregion


        public string GetFileFullUrl(EnumFileType fileType, string fileName)
        {
            var rawUrl = this.azureStorage.GetUrlFromPrivate(fileName).Result;
            var url = rawUrl;

            if (rawUrl.Contains(" "))
            {
                url = rawUrl.Replace(" ", "%20");
            }

            return url;
        }

        public async Task SendNotiFromCms(Guid? notiId)
        {
            Console.WriteLine($"Hello I am SendNotiFromCms {notiId}");

            try
            {
                notificationService.SendNotiFromCms(notiId);
            }
            catch(Exception ex)
            {

            }
        }

        public async Task DeleteNotiFromCms(Guid? notiId)
        {
            try
            {
                var memberNotiList = unitOfWork.GetRepository<Entities.MemberNotification>()
                    .Query(x => x.CmsNotificationId == notiId)
                    .ToList();

                memberNotiList?.ForEach(noti =>
                {
                    noti.IsDeleted = true;
                }
                );

                unitOfWork.SaveChanges();
            }

            catch (Exception ex)
            {
                Console.WriteLine($"DeleteNotiFromCms Ex Message {ex.Message} Ex Stacktrace {JsonConvert.SerializeObject(ex)}");
            }
        }


        public async Task UpdateClaimStatus(bool isRunFromSchedule, string otp = "")
        {
            Console.WriteLine($"UpdateClaimStatus isRunFromSchedule {isRunFromSchedule} run at {Utils.GetDefaultDate()} ");

            var isValidOtp = false;

            try
            {
                if(isRunFromSchedule == false && !string.IsNullOrEmpty(otp))
                {
                    #region CustomOtp
                    var onetimeToken = unitOfWork.GetRepository<Entities.OnetimeToken>()
                        .Query(x => x.Otp == otp)
                        .FirstOrDefault();

                    if( onetimeToken != null )
                    {
                        isValidOtp = true;

                        unitOfWork.GetRepository<Entities.OnetimeToken>().Delete(onetimeToken);
                        unitOfWork.SaveChanges();
                    }
                    

                    
                    #endregion
                }

                if(isRunFromSchedule == true || (isRunFromSchedule == false && isValidOtp == true))
                {
                    var receivedClaimIdList = unitOfWork.GetRepository<Entities.ClaimTran>()
                    .Query(x => x.ClaimStatus == EnumClaimStatusDesc.Received.ToString() && x.Ilstatus == "success")
                    .Select(x => $"{x.ClaimId}")
                    .ToList();

                    Console.WriteLine($"UpdateClaimStatus > receivedClaimIdList > {receivedClaimIdList?.Count}");

                    if (receivedClaimIdList != null)
                    {
                        var updatedClaimList = unitOfWork.GetRepository<Entities.Claim>() // claims
                            .Query(x => receivedClaimIdList.Contains(x.ClaimId) && x.Status != "PN" && x.Status != "AL")
                            .ToList();

                        Console.WriteLine($"UpdateClaimStatus > updatedClaimList > {updatedClaimList?.Count}");

                        updatedClaimList?.ForEach(updatedClaim =>
                        {

                            #region #AlignStatus
                            if (updatedClaim.Status == "BT" || updatedClaim.Status == "EX")
                            {
                                updatedClaim.Status = "AL";
                            }
                            else if (updatedClaim.Status == "DC")
                            {
                                updatedClaim.Status = "RJ";
                            }
                            #endregion

                            var claimTran = unitOfWork.GetRepository<Entities.ClaimTran>()
                            .Query(x => x.ClaimId == new Guid(updatedClaim.ClaimId))
                            .FirstOrDefault();

                            if (updatedClaim.Status != claimTran?.ClaimStatusCode)
                            {
                                var claimStatus = unitOfWork.GetRepository<Entities.ClaimStatus>() //claim_status
                                .Query(x => x.ShortDesc == updatedClaim.Status)
                                .FirstOrDefault();


                                #region #Insert ClaimsStatusUpdate 
                                var claimStatusUpdate = new Entities.ClaimsStatusUpdate()
                                {
                                    Id = Guid.NewGuid(),
                                    ClaimId = updatedClaim.ClaimId,
                                    OldStatus = "",

                                    NewStatus = updatedClaim.Status,
                                    CreatedDate = Utils.GetDefaultDate(),
                                    IsDone = false,
                                    ChangedByAiaPlus = false,
                                    NewStatusDesc = claimStatus?.LongDesc,
                                    NewStatusDescMm = claimStatus?.LongDesc,
                                    Reason = updatedClaim.FollowupReason,

                                };

                                unitOfWork.GetRepository<Entities.ClaimsStatusUpdate>().Add(claimStatusUpdate);
                                #endregion


                                #region Update ClaimTran

                                claimTran.ClaimStatus = claimStatus?.LongDesc;
                                claimTran.ClaimStatusCode = updatedClaim.Status;
                                claimTran.UpdatedBy = "IL";
                                claimTran.UpdatedOn = Utils.GetDefaultDate();

                                #endregion

                                unitOfWork.SaveChanges();
                            }




                        });

                    }
                }

                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateClaimStatus Ex Message {ex.Message} Ex Stacktrace {JsonConvert.SerializeObject(ex)}");
            }

            Console.WriteLine($"UpdateClaimStatus run finished at {Utils.GetDefaultDate()}");
        }

        public async Task SendClaimSms()
        {
            Console.WriteLine($"SendClaimSms START => {Utils.GetDefaultDate()}");

            try
            {
                


                var smsPohApiKey = AppSettingsHelper.GetSetting("SmsPoh:Key");
                var currentDate = Utils.GetDefaultDate();

                var yesterdayDate = Utils.GetDefaultDate().Date.AddHours(-6).AddMinutes(-5);

                var claimList = unitOfWork.GetRepository<Entities.ClaimTran>()
                                                .Query(x => (x.TransactionDate >= yesterdayDate && x.TransactionDate <= currentDate)
                            && (x.SentSms == null || x.SentSms == false))
                            .Select(x => new { x.ClaimId, x.PolicyNo, x.MemberPhone, x.AppMemberId })
                            .ToList();

                var locale = templateLoader.GetLocalizationJson();

                if(locale != null && locale["ClaimSms"] != null && !string.IsNullOrEmpty(locale["ClaimSms"]?.En) && !string.IsNullOrEmpty(locale["ClaimSms"]?.Mm))
                {
                    claimList?.ForEach(claim =>
                    {

                        var policy = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => x.PolicyNo == claim.PolicyNo)
                        .Select(x => new { x.PolicyHolderClientNo, x.AgentCode })
                        .FirstOrDefault();

                        if (policy != null)
                        {
                            var member = unitOfWork.GetRepository<Entities.Member>()
                            .Query(x => x.MemberId == claim.AppMemberId)
                            .FirstOrDefault();

                            if(member != null)
                            {
                                var smsEn = string.Format(locale["ClaimSms"].En, claim.PolicyNo, member.Name);
                                var smsMm = string.Format(locale["ClaimSms"].Mm, claim.PolicyNo, member.Name);

                                #region #PolicyHolderSms
                                if (!string.IsNullOrEmpty(member.Mobile))
                                {
                                    var memberMobile = Utils.NormalizeMyanmarPhoneNumber(member.Mobile);

                                    ////if (!string.IsNullOrEmpty(AppSettingsHelper.GetSetting("Env")) && AppSettingsHelper.GetSetting("Env") != "Production"
                                    ////&& !string.IsNullOrEmpty(AppSettingsHelper.GetSetting("SmsPoh:PolicyOwnerTempPhoneNo")))
                                    ////{
                                    ////    policyHolderPhoneNo = Utils.NormalizeMyanmarPhoneNumber(AppSettingsHelper.GetSetting("SmsPoh:PolicyOwnerTempPhoneNo"));

                                    ////}



                                    if (!string.IsNullOrEmpty(member.Nrc))
                                    {
                                        Utils.SendSms(memberMobile, smsMm, smsPohApiKey);

                                    }
                                    else
                                    {
                                        Utils.SendSms(memberMobile, smsEn, smsPohApiKey);

                                    }

                                    Console.WriteLine($"SendClaimSms PolicyHolderSms for ClaimId => {claim.ClaimId} Sent At {Utils.GetDefaultDate()}");

                                }

                                #endregion

                                #region #PolicyAgentSms

                                var policyAgent = unitOfWork.GetRepository<Entities.Client>()
                                    .Query(x => x.AgentCode == policy.AgentCode)
                                    .Select(x => new { x.Name, x.Nrc, x.PassportNo, x.Other, x.PhoneNo })
                                    .FirstOrDefault();

                                if (policyAgent != null && !string.IsNullOrEmpty(policyAgent.PhoneNo))
                                {
                                    var agentPhone = Utils.NormalizeMyanmarPhoneNumber(policyAgent.PhoneNo);

                                    //if (!string.IsNullOrEmpty(AppSettingsHelper.GetSetting("Env")) && AppSettingsHelper.GetSetting("Env") != "Production"
                                    //&& !string.IsNullOrEmpty(AppSettingsHelper.GetSetting("SmsPoh:AgentTempPhoneNo")))
                                    //{
                                    //    agentPhone = Utils.NormalizeMyanmarPhoneNumber(AppSettingsHelper.GetSetting("SmsPoh:AgentTempPhoneNo"));

                                    //}

                                    if (!string.IsNullOrEmpty(policyAgent.Nrc))
                                    {
                                        Utils.SendSms(agentPhone, smsMm, smsPohApiKey);

                                    }
                                    else
                                    {
                                        Utils.SendSms(agentPhone, smsEn, smsPohApiKey);

                                    }

                                    Console.WriteLine($"SendClaimSms PolicyAgentSms for ClaimId => {claim.ClaimId} Sent At {Utils.GetDefaultDate()}");

                                }

                                #endregion
                            }








                            var _claim = unitOfWork.GetRepository<Entities.ClaimTran>()
                            .Query(x => x.ClaimId == claim.ClaimId)
                            .FirstOrDefault();

                            if (_claim != null)
                            {
                                _claim.SentSms = true;
                                _claim.SentSmsAt = Utils.GetDefaultDate();

                                unitOfWork.SaveChanges();
                            }
                        }




                    });
                }

                
                
            }
            catch (Exception ex)
            {

                Console.WriteLine($"SendClaimSms EX => {Utils.GetDefaultDate()} {JsonConvert.SerializeObject(ex)}");

            }

            Console.WriteLine($"SendClaimSms END => {Utils.GetDefaultDate()}");
        }

        public async Task SendServicingSms()
        {
            Console.WriteLine($"SendServicingSms START => {Utils.GetDefaultDate()}");

            try
            {



                var smsPohApiKey = AppSettingsHelper.GetSetting("SmsPoh:Key");
                var currentDate = Utils.GetDefaultDate(); //6 pm
                var status = EnumClaimStatusDesc.Received.ToString();


                var yesterdayDate = Utils.GetDefaultDate().Date.AddHours(-6).AddMinutes(-5); // yesterday 5:55 pm

                

                var serviceRequestList = unitOfWork.GetRepository<Entities.ServiceMain>()
                            .Query(x => (x.CreatedDate >= yesterdayDate && x.CreatedDate <= currentDate)
                            && (x.SentSms == null || x.SentSms == false))
                            .Select(x => new { x.ServiceID, x.PolicyNumber, x.LoginMemberID })
                            .ToList();

                var locale = templateLoader.GetLocalizationJson();

                if (locale != null && locale["ServicingSms"] != null && !string.IsNullOrEmpty(locale["ServicingSms"]?.En) && !string.IsNullOrEmpty(locale["ServicingSms"]?.Mm))
                {
                    serviceRequestList?.ForEach(serviceRequest =>
                    {

                        var policy = unitOfWork.GetRepository<Entities.Policy>()
                        .Query(x => x.PolicyNo == serviceRequest.PolicyNumber)
                        .Select(x => new { x.PolicyHolderClientNo, x.AgentCode })
                        .FirstOrDefault();

                        if (policy != null)
                        {
                            var member = unitOfWork.GetRepository<Entities.Member>()
                            .Query(x => x.MemberId == serviceRequest.LoginMemberID)
                            .FirstOrDefault();

                            if (member != null)
                            {
                                var appConfig = unitOfWork.GetRepository<Entities.AppConfig>().Query().FirstOrDefault();

                                var smsEn = string.Format(locale["ServicingSms"].En, serviceRequest.PolicyNumber, member.Name, appConfig?.SherContactNumber, appConfig?.AiaCustomerCareEmail);
                                var smsMm = string.Format(locale["ServicingSms"].Mm, serviceRequest.PolicyNumber, member.Name, appConfig?.SherContactNumber, appConfig?.AiaCustomerCareEmail);

                                #region #PolicyHolderSms
                                if (!string.IsNullOrEmpty(member.Mobile))
                                {
                                    var memberMobile = Utils.NormalizeMyanmarPhoneNumber(member.Mobile);

                                    //if (!string.IsNullOrEmpty(AppSettingsHelper.GetSetting("Env")) && AppSettingsHelper.GetSetting("Env") != "Production"
                                    //&& !string.IsNullOrEmpty(AppSettingsHelper.GetSetting("SmsPoh:PolicyOwnerTempPhoneNo")))
                                    //{
                                    //    memberMobile = Utils.NormalizeMyanmarPhoneNumber(AppSettingsHelper.GetSetting("SmsPoh:PolicyOwnerTempPhoneNo"));

                                    //}



                                    if (!string.IsNullOrEmpty(member.Nrc))
                                    {
                                        Utils.SendSms(memberMobile, smsMm, smsPohApiKey);

                                    }
                                    else
                                    {
                                        Utils.SendSms(memberMobile, smsEn, smsPohApiKey);

                                    }

                                    Console.WriteLine($"SendServicingSms PolicyHolderSms for ServiceID => {serviceRequest.ServiceID} Sent At {Utils.GetDefaultDate()}");

                                }

                                #endregion

                                #region #PolicyAgentSms

                                var policyAgent = unitOfWork.GetRepository<Entities.Client>()
                                    .Query(x => x.AgentCode == policy.AgentCode)
                                    .Select(x => new { x.Name, x.Nrc, x.PassportNo, x.Other, x.PhoneNo })
                                    .FirstOrDefault();

                                if (policyAgent != null && !string.IsNullOrEmpty(policyAgent.PhoneNo))
                                {
                                    var agentPhone = Utils.NormalizeMyanmarPhoneNumber(policyAgent.PhoneNo);

                                    //if (!string.IsNullOrEmpty(AppSettingsHelper.GetSetting("Env")) && AppSettingsHelper.GetSetting("Env") != "Production"
                                    //&& !string.IsNullOrEmpty(AppSettingsHelper.GetSetting("SmsPoh:AgentTempPhoneNo")))
                                    //{
                                    //    agentPhone = Utils.NormalizeMyanmarPhoneNumber(AppSettingsHelper.GetSetting("SmsPoh:AgentTempPhoneNo"));

                                    //}

                                    if (!string.IsNullOrEmpty(policyAgent.Nrc))
                                    {
                                        Utils.SendSms(agentPhone, smsMm, smsPohApiKey);

                                    }
                                    else
                                    {
                                        Utils.SendSms(agentPhone, smsEn, smsPohApiKey);

                                    }

                                    Console.WriteLine($"SendServicingSms PolicyAgentSms for ServiceID => {serviceRequest.ServiceID} Sent At {Utils.GetDefaultDate()}");

                                }

                                #endregion
                            }


                            var serviceMain = unitOfWork.GetRepository<Entities.ServiceMain>()
                            .Query(x => x.ServiceID == serviceRequest.ServiceID)
                            .FirstOrDefault();

                            if (serviceMain != null)
                            {
                                serviceMain.SentSms = true;
                                serviceMain.SentSmsAt = Utils.GetDefaultDate();

                                unitOfWork.SaveChanges();
                            }
                        }




                    });
                }



            }
            catch (Exception ex)
            {

                Console.WriteLine($"SendServicingSms EX => {Utils.GetDefaultDate()} {JsonConvert.SerializeObject(ex)}");

            }

            Console.WriteLine($"SendServicingSms END => {Utils.GetDefaultDate()}");
        }
    }

    public class SaveLogModel
    {
        public string? LogMessage { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? Exception { get; set; }
        public string? EndPoint { get; set; }
    }
    public class CustomClient
    {
        public string? ClientNo { get; set; }
        public string? IdenValue { get; set; }
        public EnumIdenType? IdenType { get; set; }
    }


}
