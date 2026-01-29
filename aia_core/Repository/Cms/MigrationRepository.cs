using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.Model.Mobile.Response;
using aia_core.RecurringJobs;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Transactions;
using Newtonsoft.Json;
using aia_core.Model.Cms.Response.Hospital;
using aia_core.Model.Cms.Request.Hospital;
using aia_core.Model.Mobile.Request;
using Hangfire;
using System.Linq;

namespace aia_core.Repository.Cms
{
    public interface IMigrationRepository
    {
        Task<ResponseModel<string>> Migrate(bool migrateAll, bool? retry = false, string? condition = "");
    }

    public class MigrationRepository : BaseRepository, IMigrationRepository
    {
        #region "const"
        private readonly IConfiguration config;
        private readonly IOktaService oktaService;
        private readonly INotificationService notificationService;
        private readonly ITemplateLoader templateLoader;
        
        public MigrationRepository(IConfiguration config, IOktaService oktaService, IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork
            , INotificationService notificationService, ITemplateLoader templateLoader)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.config = config;
            this.oktaService = oktaService;
            this.notificationService = notificationService;
            this.templateLoader = templateLoader;
        }
        #endregion

        public async Task<ResponseModel<string>> Migrate(bool migrateAll, bool? retry = false, string? condition = "")
        {
            //oktaService.DeleteUser("r9u4zprc4yd7mWHTF8Yp");
            BackgroundJob.Enqueue(() => MigrateUserAccount(migrateAll, retry, condition));
            //MigrateUserAccount();
            return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0);
        }

        #region #migrate-account
        public async Task MigrateUserAccount(bool migrateAll, bool? retry = false, string? condition = "")
        {
            try
            {
                Console.WriteLine("Start MigrateUserAccount");
                string groupID = config["Okta:GroupID"];


                List<UsersTemp> usersTemps = new List<UsersTemp>();

                if (migrateAll)
                {
                    usersTemps = unitOfWork.GetRepository<UsersTemp>().Query(x => x.is_done != true).ToList();
                }
                else if (migrateAll == false)
                {
                    usersTemps = unitOfWork.GetRepository<UsersTemp>().Query(x => x.is_done != true)
                        .OrderByDescending(x => x.registration_date)
                        .Skip(0).Take(10)
                        .ToList();
                }
                
                
                if (retry == true && !string.IsNullOrEmpty(condition))
                {
                    usersTemps = unitOfWork.GetRepository<UsersTemp>()
                        .Query(x => x.is_done == true && x.migrate_status == "fail" && x.migrate_log.Contains(condition))
                        .ToList();

                    Console.WriteLine($"MigrateUserAccount => Retry {usersTemps?.Count}");
                }
                

                var count = 1;
                foreach (var user in usersTemps)
                {
                    Console.WriteLine($"MigrateUserAccount => UserId: {user.id} Datetime: {Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)} " +
                        $"Name: {user.name} Nrc: {user.nrc} Passport: {user.passport} Others: {user.others} " +
                        $"Email: {user.email} Phone: {user.phone_no}");

                    count++;

                    var checkClient = unitOfWork.GetRepository<Entities.Client>().Query(
                    expression: r =>
                     (!(string.IsNullOrEmpty(user.nrc)) && r.Nrc == user.nrc) ||
                            (!(string.IsNullOrEmpty(user.passport)) && r.PassportNo == user.passport) ||
                            ((string.IsNullOrEmpty(user.others)) && r.Other == user.others)
                    ).FirstOrDefault();
					
                    if(checkClient == null) 
                    {
                        user.is_done = true;
                        user.migrate_date = Utils.GetDefaultDate();
                        user.migrate_status = "fail";
                        user.migrate_log = "No client found";
                        unitOfWork.SaveChanges();
                        continue;
                    }

                    var checkMember = unitOfWork.GetRepository<Entities.Member>().Query(
                        expression: r =>
                            (user.nrc != null && r.Nrc == user.nrc) ||
                            (user.passport != null && r.Passport == user.passport) ||
                            (user.others != null && r.Others == user.others)
                    ).Any();

                    if(checkMember) 
                    {
                        user.is_done = true;
                        user.migrate_date = Utils.GetDefaultDate();
                        user.migrate_status = "fail";
                        user.migrate_log = "Already registered user";
                        unitOfWork.SaveChanges();
                        continue;
                    }

                    var hasEmail = unitOfWork.GetRepository<Entities.Member>().Query(expression: r => r.Email == user.email).Any();
                    if (hasEmail) 
                    {
                        user.is_done = true;
                        user.migrate_date = Utils.GetDefaultDate();
                        user.migrate_status = "fail";
                        user.migrate_log = "This email has already been registered.";
                        unitOfWork.SaveChanges();
                        continue;
                    }
                    var hasPhone = unitOfWork.GetRepository<Entities.Member>().Query(expression: r =>  r.Mobile == user.phone_no).Any();
                    if (hasPhone) 
                    {
                        user.is_done = true;
                        user.migrate_date = Utils.GetDefaultDate();
                        user.migrate_status = "fail";
                        user.migrate_log = "This mobile number has already been registered.";
                        unitOfWork.SaveChanges();
                        continue;
                    }

                    Member member = new Member();
                    member.OktaUserName = Utils.GenerateOktaUserName();

                    RegisterRequest registerRequest = new RegisterRequest();
                    registerRequest.FullName = user.name;
                    registerRequest.Dob = user.date_of_birth;
                    registerRequest.Gender = user.gender=="M"?EnumGender.Male:EnumGender.Female;
                    registerRequest.Email = user.email;
                    registerRequest.Phone = user.phone_no;
                    registerRequest.Password = user.password;
                    registerRequest.ConfirmPassword = user.password;

                    string algorithmValue = config["Okta:Algorithm"];
                    // string workFactorValue = config["Okta:WorkFactor"];
                    // string saltValue = config["Okta:Salt"];

                    string[] parts = registerRequest.ConfirmPassword.Split('$');

                    int workFactorValue = int.Parse(parts[2]);
                    string saltValue = parts[3].Substring(0, 22);
                    string value = parts[3].Substring(22);

                    var payload = new
                    {
                        profile = new
                        {
                            firstName = registerRequest.FullName,
                            lastName = registerRequest.FullName,
                            email = registerRequest.Email,
                            mobilePhone = registerRequest.Phone,
                            login = member.OktaUserName,
                            locale = "my_MM",
                        },
                        credentials = new
                        {
                            //password = new { value = registerRequest.ConfirmPassword },
                            password = new
                            {
                                hash = new
                                {
                                    algorithm = algorithmValue,
                                    workFactor = workFactorValue,
                                    salt = saltValue,
                                    value = value
                                }
                            },
                            provider = new { type = "OKTA", name = "OKTA" }
                        },
                        groupIds = new string[] { groupID }
                    };
                    string okta_register_request = System.Text.Json.JsonSerializer.Serialize(payload);

                    var oktaRegister = await oktaService.RegisterUserMigration(member.OktaUserName, registerRequest);
                    if (oktaRegister.Code == (long)ErrorCode.E0)
                    {
                        member.MemberId = Guid.NewGuid();
                        member.Name = user.name;
                        member.Email = user.email;
                        member.Mobile = user.phone_no;
                        member.Dob = user.date_of_birth;
                        member.Gender = user.gender=="M"?"Male":"Female";

                        member.RegisterDate = user.registration_date;
                        member.LastActiveDate = null;

                        member.Auth0Userid = oktaRegister.Data.id;

                        member.Nrc = user.nrc;
                        member.Passport = user.passport;
                        member.Others = user.others;

                        member.IsVerified = true;
                        member.IsMobileVerified = true;
                        member.IsEmailVerified = true;
                        member.IsActive = true;

                        user.is_done = true;
                        user.migrate_date = Utils.GetDefaultDate();
                        user.migrate_status = "success";
                        user.migrate_log = $"Success | OktaUserName : {member.OktaUserName}";
                        user.okta_register_request = okta_register_request;
                        
                        unitOfWork.GetRepository<aia_core.Entities.Member>().Add(member);
                        unitOfWork.SaveChanges();
                        continue;
                    }
                    else
                    {
                        user.is_done = true;
                        user.migrate_date = Utils.GetDefaultDate();
                        user.migrate_status = "fail";
                        user.migrate_log = $"Okta API fail | Okta response : {JsonConvert.SerializeObject(oktaRegister)}";
                        user.okta_register_request = okta_register_request;
                        unitOfWork.SaveChanges();
                        continue;
                    }
                }
                Console.WriteLine("End MigrateUserAccount");

            }
            catch (Exception ex)
            {
                Console.WriteLine("MigrateUserAccount Error : " + ex);
            }
        }
        #endregion
    }
}