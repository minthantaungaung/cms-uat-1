using aia_core.Entities;
using aia_core.Model.Cms;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Response;
using aia_core.RecurringJobs;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Repository.Cms
{
    public interface IProfileRepository
    {
        Task<ResponseModel<CmsAccessUser>> Get();
        Task<ResponseModel<StaffResponse>> Update(CmsAccessUserUpdate model);
    }


    public class ProfileRepository : BaseRepository, IProfileRepository
    {
        private readonly IRoleRepository roleRepository;
        private readonly IStaffRepository staffRepository;
        public ProfileRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config, IErrorCodeProvider errorCodeProvider, IUnitOfWork<Context> unitOfWork
            , IRoleRepository roleRepository, IStaffRepository staffRepository)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.roleRepository = roleRepository;   
            this.staffRepository = staffRepository;
        }

        public async Task<ResponseModel<CmsAccessUser>> Get()
        {
            var loginUser = GetCmsUser();

            if (loginUser == null)
            {
                return errorCodeProvider.GetResponseModel<CmsAccessUser>(ErrorCode.E403);
            }

            var staff = unitOfWork.GetRepository<Entities.Staff>()
                .Query(x => x.Id == Guid.Parse(loginUser.ID) && x.IsActive == true)
                .Include(x => x.Role)
                .FirstOrDefault();

            if (staff == null)
            {
                return errorCodeProvider.GetResponseModel<CmsAccessUser>(ErrorCode.E403);
            }

            return errorCodeProvider.GetResponseModel<CmsAccessUser>(ErrorCode.E0, 
                new CmsAccessUser 
                { ID = staff.Id.ToString()
                , Name = staff?.Name
                , Email = staff?.Email  
                , RoleID = staff?.RoleId.ToString()
                , RoleName = staff.Role?.Title
                }
                
                );
        }

        public async Task<ResponseModel<StaffResponse>> Update(CmsAccessUserUpdate model)
        {
            try
            {
                var oldUserData = GetCmsUser();

                if (oldUserData == null)
                {
                    return errorCodeProvider.GetResponseModel<StaffResponse>(ErrorCode.E403);
                }

                var isExistEmail = unitOfWork.GetRepository<Entities.Staff>()
                    .Query(x => x.Id != Guid.Parse(oldUserData.ID) && x.Email == model.Email && x.IsActive == true).Any();

                if(isExistEmail) return new ResponseModel<StaffResponse> { Code = 400, Message = "There is other staff using your new email" };  

                var cmsUser = new UpdateStaffRequest
                { 
                    Id = new Guid(oldUserData.ID),
                    Name = model.Name,
                    Email = model.Email,
                    RoleId = new Guid(oldUserData?.RoleID),
                };

                return staffRepository.Update(cmsUser).Result;
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<StaffResponse>(ErrorCode.E500);
            }
        }
    }
}
