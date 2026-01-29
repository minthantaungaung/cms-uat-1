using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.Provider;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace aia_core.Repository.Cms
{
    public interface IAuthRepository
    {
        ResponseModel<string> ADLogin(string email);
        ResponseModel<LoginResponse> Login(LoginRequest model);
        ResponseModel<Permission> GetPermissions();

        ResponseModel Logout();
    }
    public class AuthRepository : BaseRepository, IAuthRepository
    {
        private readonly ICmsTokenGenerator cmsTokenGenerator;
        public AuthRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork, ICmsTokenGenerator cmsTokenGenerator)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.cmsTokenGenerator = cmsTokenGenerator;
        }

        public ResponseModel<string> ADLogin(string email)
        {
            try
            {
                var staff = unitOfWork.GetRepository<Staff>().Query(x => x.Email.ToLower() == email.ToLower()).FirstOrDefault();

                if (staff == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E501);


                if (staff.IsActive != true) return new ResponseModel<string> { Code = 40111, Message = "Inactive user" };

                string token = GetSuccessLoginToken(staff);
                CmsAuditLogLogin(
                        objectGroup: EnumObjectGroup.Auth,
                        objectAction: EnumObjectAction.ADLogin,
                        email: email,
                        objectId: staff.Id);
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0, token);
            }
            catch (System.Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }

        }

        public ResponseModel<LoginResponse> Login(LoginRequest model)
        {
            try
            {
                var staff = unitOfWork.GetRepository<Staff>()
                    .Query(x => x.Email.ToLower() == model.email.ToLower())
                    .FirstOrDefault();

                Console.WriteLine($"Login DB Check => {staff?.Name} {staff?.Email}");


                if (staff == null) return errorCodeProvider.GetResponseModel<LoginResponse>(ErrorCode.E402);

                if (staff.IsActive != true) return new ResponseModel<LoginResponse> { Code = 40111, Message = "Inactive user"};

                bool validatePassword = PasswordManager.ValidatePassword(model.password, staff.PasswordHash, staff.PasswordSalt);
                if (validatePassword)
                {
                    string token = GetSuccessLoginToken(staff);

                    var role = unitOfWork.GetRepository<Entities.Role>().Query(x => x.Id == staff.RoleId).FirstOrDefault();
                    if(role == null || string.IsNullOrEmpty(role.Permissions)) return errorCodeProvider.GetResponseModel<LoginResponse>(ErrorCode.E403);

                    var module = new List<string>();
                    var permissions = role.Permissions.Split(",");

                    foreach (var perm in permissions)
                    {
                        var permiss = perm.Replace("[", "").Replace("]", "");
                        var enumRoleModule = (EnumRoleModule)Convert.ToInt32(permiss);

                        module.Add(enumRoleModule.ToString());
                    }

                    var permission = new Permission();

                    if (module != null && module.Count > 0)
                    {
                        permission = new Permission
                        {
                            StaffEmail = model.email,
                            StaffId = staff.Id,
                            RoleId = role.Id,
                            RoleName = role.Title,
                            Permissions = module.ToArray(),
                        };
                    }
                    

                    CmsAuditLogLogin(
                        objectGroup: EnumObjectGroup.Auth,
                        objectAction: EnumObjectAction.PasswordLogin,
                        email: model.email,
                        objectId: staff.Id);
                    return errorCodeProvider.GetResponseModel<LoginResponse>(ErrorCode.E0, new LoginResponse() { accessToken = token, Permission =  permission});
                }
                else
                {
                    return errorCodeProvider.GetResponseModel<LoginResponse>(ErrorCode.E402);
                }
            }
            catch (System.Exception ex)
            {

                Console.WriteLine($"Login Ex => {ErrorCode.E500} {ex.Message} {JsonConvert.SerializeObject(ex)}");


                return errorCodeProvider.GetResponseModel<LoginResponse>(ErrorCode.E500);
            }


        }

        ResponseModel<Permission> IAuthRepository.GetPermissions()
        {
            try
            {
                var cmsUser = GetCmsUser();
                if(cmsUser == null) return errorCodeProvider.GetResponseModel<Permission>(ErrorCode.E401);

                var roleId = new Guid(cmsUser.RoleID);
                var role = unitOfWork.GetRepository<Entities.Role>().Query(x => x.Id == roleId).FirstOrDefault();

                if (role == null || string.IsNullOrEmpty(role.Permissions)) return errorCodeProvider.GetResponseModel<Permission>(ErrorCode.E403);

                var module = new List<string>();
                var permissions = role.Permissions.Split(",");

                foreach (var perm in permissions)
                {
                    var permiss = perm.Replace("[", "").Replace("]", "");
                    var enumRoleModule = (EnumRoleModule)Convert.ToInt32(permiss);

                    module.Add(enumRoleModule.ToString());
                }

                if (module != null && module.Count > 0)
                {
                    var permission = new Permission
                    {
                        StaffEmail = cmsUser.Email,
                        StaffId = Guid.Parse(cmsUser.ID),
                        RoleId = role.Id,
                        RoleName = role.Title,
                        Permissions = module.ToArray(),
                    };

                    return errorCodeProvider.GetResponseModel<Permission>(ErrorCode.E0, permission);
                }

                return errorCodeProvider.GetResponseModel<Permission>(ErrorCode.E403);
            }
            catch(Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<Permission>(ErrorCode.E500);
            }
        }

        private string GetSuccessLoginToken(Staff staff)
        {
            
            List<CmsUserSession> sessions = unitOfWork.GetRepository<CmsUserSession>().Query(x => x.UserId == staff.Id).ToList();

            if (sessions?.Any() == true)
            {
                unitOfWork.GetRepository<CmsUserSession>().Delete(sessions);
                unitOfWork.SaveChanges();
            }

            Guid sessionId = Guid.NewGuid();
            var accessToken = cmsTokenGenerator.GetAccessToken(staff, sessionId.ToString());

            unitOfWork.GetRepository<CmsUserSession>().Add(new CmsUserSession
            {
                UserId = staff.Id,
                SessionId = sessionId,
                GeneratedOn = Utils.GetDefaultDate(),
                Token = accessToken,
            });

            unitOfWork.SaveChanges();

            return accessToken;
        }

        ResponseModel IAuthRepository.Logout()
        {
            var cmsUser = GetCmsUser();
            if(!string.IsNullOrEmpty(cmsUser?.ID))
            {
                List<CmsUserSession> sessions = unitOfWork.GetRepository<CmsUserSession>()
                    .Query(x => x.UserId == Guid.Parse(cmsUser.ID))
                    .ToList();

                if (sessions?.Any() == true)
                {
                    unitOfWork.GetRepository<CmsUserSession>().Delete(sessions);
                    unitOfWork.SaveChanges();
                }
            }

            return errorCodeProvider.GetResponseModel(ErrorCode.E0);
        }
    }
}
