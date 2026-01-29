using aia_core.Entities;
using aia_core.RecurringJobs;
using aia_core.Services;
using aia_core.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using aia_core.Model.Cms.Request.Notification;
using aia_core.Model.Cms.Response.Notification;
using DocumentFormat.OpenXml.Vml.Office;
using System.Net;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Bibliography;
using FirebaseAdmin.Messaging;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http.HttpResults;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Server.HttpSys;
using DocumentFormat.OpenXml.Spreadsheet;
using Irony;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Repository.Cms
{
    public interface INotificationRepository
    {
        ResponseModel<PagedList<NotificationResponse>> GetList(NotificationRequest model);
        ResponseModel<NotificationDetailResponse> Get(Guid id);
        ResponseModel<ThirdPartyNotificationResponse> CreateNotificationOnDemand(ThirdPartyNotificationRequest model);
        ResponseModel<NotificationResponse> Create(CreateNotificationRequest model);
        ResponseModel<NotificationResponse> Update(UpdateNotificationRequest model);
        ResponseModel<NotificationResponse> Delete(Guid id);
        Task<ResponseModel> ToggleActive([Required] Guid id);
    }
    
    public class NotificationRepository : BaseRepository, INotificationRepository
    {
        private readonly IRecurringJobRunner recurringJobRunner;
        private readonly IBackgroundJobClient backgroundJob;
        public NotificationRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Context> unitOfWork, IRecurringJobRunner recurringJobRunner, IBackgroundJobClient backgroundJob)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.recurringJobRunner = recurringJobRunner;
            this.backgroundJob = backgroundJob;
        }

        public async Task<ResponseModel> ToggleActive(Guid id)
        {
            try
            {
                var cmsNoti = unitOfWork.GetRepository<Entities.CmsNotification>().Query(x => x.Id == id)
                    .FirstOrDefault();

                if (cmsNoti == null) return errorCodeProvider.GetResponseModel(ErrorCode.E400);

                cmsNoti.IsActive = !cmsNoti.IsActive;
                cmsNoti.UpdatedBy = GetCmsUser().Name;
                cmsNoti.UpdatedOn = Utils.GetDefaultDate();
                unitOfWork.SaveChanges();

                var log = CmsAuditLog(
                        objectGroup: EnumObjectGroup.Faq,
                        objectAction: EnumObjectAction.ToggleStatus,
                        objectId: cmsNoti.Id,
                        objectName: cmsNoti.TitleEn,
                        newData: JsonConvert.SerializeObject(cmsNoti, Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                            }
                            ));

                return errorCodeProvider.GetResponseModel(ErrorCode.E0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{httpContext?.HttpContext?.Request.Method} " +
                    $"=> {httpContext?.HttpContext?.Request.Path} " +
                    $"=> Exception " +
                    $"=> {JsonConvert.SerializeObject(ex)}");

                return errorCodeProvider.GetResponseModel(ErrorCode.E500);
            }
        }

        ResponseModel<NotificationResponse> INotificationRepository.Create(CreateNotificationRequest model)
        {
            try
            {
                Console.WriteLine($"Cms => Noti => Create model {JsonConvert.SerializeObject(model)}");

                if (model.SendDateAndTime < Utils.GetDefaultDate())
                {
                    return new ResponseModel<NotificationResponse> { Code = 400, Message = "SendDateAndTime cannot be less than current datetime." };
                }

                //if (model.SendDateAndTime < Utils.GetDefaultDate().AddMinutes(5))
                //{
                //    return new ResponseModel<NotificationResponse> { Code = 400,
                //        Message = "SendDateAndTime must be greater than 5 minutes of current datetime due to schedule job creating process."
                //    };
                //}

                #region #TranslateCountryRelatedCodeToDesc
                var provinceDescListString = "";
                var districtDescListString = "";
                var townshipDescListString = "";

                if (!string.IsNullOrEmpty(model.Province))
                {
                    #region #Province
                    var provinceCodeList = model.Province.Split(",")?.ToList();
                    var provinceDescList = unitOfWork.GetRepository<Entities.Province>()
                            .Query(x => provinceCodeList.Contains(x.province_code))
                            .Select(x => x.province_eng_name)
                            .ToList();

                    provinceDescListString = string.Join(",", provinceDescList);

                    #endregion


                    #region #District
                    if (!string.IsNullOrEmpty(model.Province) && !string.IsNullOrEmpty(model.District))
                    {
                        var distinctCodeList = model.District.Split(",")?.ToList();
                        var districtDescList = unitOfWork.GetRepository<Entities.District>()
                            .Query(x => provinceCodeList.Contains(x.province_code) && distinctCodeList.Contains(x.district_code))
                            .Select(x => x.district_eng_name)
                            .ToList();

                        districtDescListString = string.Join(",", districtDescList);

                        if (!string.IsNullOrEmpty(model.Township))
                        {
                            var townshipCodeList = model.Township.Split(",")?.ToList();
                            var townshipDescList = unitOfWork.GetRepository<Entities.Township>()
                                .Query(x => distinctCodeList.Contains(x.district_code) && townshipCodeList.Contains(x.township_code))
                                .Select(x => x.township_eng_name)
                                .ToList();

                            townshipDescListString = string.Join(",", townshipDescList);
                        }
                    }
                    #endregion

                    
                }
                #endregion

                model.Province = provinceDescListString;
                model.District = districtDescListString;
                model.Township = townshipDescListString;
                Console.WriteLine($"Cms => Noti => Create model.Province {model.Province} model.District {model.District} model.Township {model.Township}");

                var notification = new Entities.CmsNotification
                {
                    Id = Guid.NewGuid(),
                    TitleEn = model.TitleEn,
                    TitleMm = model.TitleMm,
                    DescEn = model.DescEn,
                    DescMm  = model.DescMm,
                    SendDateAndTime = model.SendDateAndTime,
                    Image = "",
                    Audience = model.Audience.ToString(),
                    Country = model.Country,
                    Province = model.Province,
                    District = model.District,
                    Township = model.Township,
                    MemberType = model.MemberType.ToString(),
                    IsActive = true,
                    IsDeleted = false,
                    SendingStatus = EnumNotiStatus.Pending.ToString(),
                    CreatedBy = GetCmsUser().Name,
                    CreatedOn = Utils.GetDefaultDate(),
                    ProductType = model.ProductType,
                    PolicyStatus = model.PolicyStatus,
            };


                if(model.Audience == EnumNotiAudience.All) 
                {
                    notification.AudienceCount = "All members";
                }
                else
                {
                    notification.AudienceCount = "{0} members";


                    #region #UpdateAudienceCount

                    List<string>? memberIdList = new List<string>();

                    var memberQuery = unitOfWork.GetRepository<Entities.Member>()
                            .Query(x => x.IsVerified == true && x.IsActive == true);

                    if (model.MemberType != null && model.MemberType != EnumIndividualMemberType.All)
                    {
                        memberQuery = memberQuery.Where(x => x.MemberType == model.MemberType.ToString());
                    }

                    if (!string.IsNullOrEmpty(model.Country))
                    {
                        var countryList = model.Country.Split(",");
                        memberQuery = memberQuery.Where(x => countryList.Contains(x.Country));
                    }

                    if (!string.IsNullOrEmpty(model.Province))
                    {
                        var provinceList = model.Province.Split(",");
                        memberQuery = memberQuery.Where(x => provinceList.Contains(x.Province));
                    }

                    if (!string.IsNullOrEmpty(model.District))
                    {

                        var districtList = model.District.Split(",");
                        memberQuery = memberQuery.Where(x => districtList.Contains(x.District));
                    }

                    if (!string.IsNullOrEmpty(model.Township))
                    {
                        var townshipList = model.Township.Split(",");
                        memberQuery = memberQuery.Where(x => townshipList.Contains(x.Township));
                    }



                    if (model.ProductType?.Any() == true)
                    {
                        var values = model.ProductType.Trim().Split(",");
                        Expression<Func<Entities.Member, bool>> searchExpression = entity => false;

                        foreach (var value in values)
                        {
                            searchExpression = searchExpression.OrElse(entity => EF.Functions.Like(entity.ProductCodeList, $"%{value}%"));
                        }

                        memberQuery = memberQuery.Where(searchExpression);
                    }

                    if (model.PolicyStatus?.Any() == true)
                    {
                        var values = model.PolicyStatus.Trim().Split(",");
                        Expression<Func<Entities.Member, bool>> searchExpression = entity => false;

                        foreach (var value in values)
                        {
                            searchExpression = searchExpression.OrElse(entity => EF.Functions.Like(entity.PolicyStatusList, $"%{value}%"));
                        }

                        memberQuery = memberQuery.Where(searchExpression);
                    }


                    memberIdList = memberQuery.Select(x => $"{x.MemberId}").ToList();

                    notification.AudienceCount = $"{memberIdList?.Count} members";
                    unitOfWork.SaveChanges();


                    #endregion
                }



                #region #upload-image
                if (model.Image != null)
                {
                    string imageName = $"{Utils.GetDefaultDate().Ticks}-{model.Image.FileName}";
                    var uploadResult = azureStorage.UploadAsync(imageName, model.Image).Result;

                    Console.WriteLine($"UploadResult => {JsonConvert.SerializeObject(uploadResult)}");

                    if (uploadResult.Code == (int)HttpStatusCode.OK)
                    {
                        notification.Image = imageName;
                        notification.FullImageUrl = GetFileFullUrl(imageName);
                    }
                    
                }
                #endregion

                Console.WriteLine($"Cms Noti Created!");

                #region #ScheduleSetup

                if(model.SendNow == true)
                {
                    var jobId = this.backgroundJob.Enqueue<aia_core.RecurringJobs.IRecurringJobRunner>((x) => x.SendNotiFromCms(notification.Id));
                    notification.JobId = int.Parse(jobId);
                }
                else
                {
                    var scheduledTimespan =  model.SendDateAndTime - Utils.GetDefaultDate();
                    Console.WriteLine($"notification.SendDateAndTime => Local => {model.SendDateAndTime} scheduledTimespan => {scheduledTimespan}");
                   

                    var jobId = this.backgroundJob.Schedule<aia_core.RecurringJobs.IRecurringJobRunner>((x) => x.SendNotiFromCms(notification.Id), scheduledTimespan.Value);
                    notification.JobId = int.Parse(jobId);

                    Console.WriteLine($"Cms Noti Schedule Created! JobId {notification.JobId}");
                    
                }


                

                #endregion

                unitOfWork.GetRepository<Entities.CmsNotification>().Add(notification);
                unitOfWork.SaveChanges();

                var log = CmsAuditLog(
                        objectGroup: EnumObjectGroup.Notification,
                        objectAction: EnumObjectAction.Create,
                        objectId: notification.Id,
                        objectName: notification.TitleEn,
                        newData: System.Text.Json.JsonSerializer.Serialize(notification));

                Console.WriteLine($"Cms Noti Log Created!");

                return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E0, new NotificationResponse
                { 
                Id = notification.Id,
                SendingStatus = EnumNotiStatus.Pending.ToString(),
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{httpContext?.HttpContext?.Request.Method} " +
                    $"=> {httpContext?.HttpContext?.Request.Path} " +
                    $"=> Exception " +
                    $"=> {JsonConvert.SerializeObject(ex)}");

                return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E500);
            }
        }

        // Lightweight on-demand create method: does not translate location codes,
        // does not compute audience counts, does not upload images, and does not schedule jobs.
        public ResponseModel<ThirdPartyNotificationResponse> CreateNotificationOnDemand(ThirdPartyNotificationRequest model)
        {
            try
            {
                Console.WriteLine($"Cms => Noti => CreateNotificationOnDemand model {JsonConvert.SerializeObject(model)}");

                
                var notification = new Entities.CmsNotification
                {
                    Id = Guid.NewGuid(),
                    TitleEn = model.Subject,
                    TitleMm = model.Subject,
                    DescEn = model.Message,
                    DescMm = model.Message,
                    SendDateAndTime = Utils.GetDefaultDate(),
                    Image = string.Empty,
                    Audience = string.Empty,
                    Country = string.Empty,
                    Province = string.Empty,
                    District = string.Empty,
                    Township = string.Empty,
                    MemberType = string.Empty,
                    IsActive = true,
                    IsDeleted = false,
                    SendingStatus = EnumNotiStatus.Pending.ToString(),
                    CreatedBy = GetCmsUser().Name,
                    CreatedOn = Utils.GetDefaultDate(),
                    ProductType = string.Empty,
                    PolicyStatus = string.Empty,
                    AudienceCount = null
                };

                unitOfWork.GetRepository<Entities.CmsNotification>().Add(notification);
                unitOfWork.SaveChanges();

                var log = CmsAuditLog(
                        objectGroup: EnumObjectGroup.Notification,
                        objectAction: EnumObjectAction.Create,
                        objectId: notification.Id,
                        objectName: notification.TitleEn,
                        newData: System.Text.Json.JsonSerializer.Serialize(notification));

                return errorCodeProvider.GetResponseModel(ErrorCode.E0, new ThirdPartyNotificationResponse
                {
                    ClientId = model.ClientId,
                    isSuccess = false,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{httpContext?.HttpContext?.Request.Method} " +
                    $"=> {httpContext?.HttpContext?.Request.Path} " +
                    $"=> Exception " +
                    $"=> {JsonConvert.SerializeObject(ex)}");

                return errorCodeProvider.GetResponseModel<ThirdPartyNotificationResponse>(ErrorCode.E500);
            }
        }

        ResponseModel<NotificationResponse> INotificationRepository.Delete(Guid id)
        {
            try
            {
                var notification = unitOfWork.GetRepository<Entities.CmsNotification>()
                    .Query(x => x.Id == id)
                    .FirstOrDefault();

                if (notification == null) return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E400);

                notification.IsDeleted = true;
                notification.UpdatedBy = GetCmsUser().Name;
                notification.UpdatedOn = Utils.GetDefaultDate();
                unitOfWork.SaveChanges();

                this.backgroundJob.Enqueue<aia_core.RecurringJobs.IRecurringJobRunner>((x) => x.DeleteNotiFromCms(notification.Id));

                var log = CmsAuditLog(
                        objectGroup: EnumObjectGroup.Notification,
                        objectAction: EnumObjectAction.Delete,
                        objectId: notification.Id,
                        objectName: notification.TitleEn);

                return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{httpContext?.HttpContext?.Request.Method} " +
                    $"=> {httpContext?.HttpContext?.Request.Path} " +
                    $"=> Exception " +
                    $"=> {JsonConvert.SerializeObject(ex)}");

                return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E500);
            }
        }

        ResponseModel<NotificationDetailResponse> INotificationRepository.Get(Guid id)
        {
            try
            {
                var notification = unitOfWork.GetRepository<Entities.CmsNotification>()
                    .Query(notification => notification.Id == id)
                    .FirstOrDefault();

                if (notification == null) return errorCodeProvider.GetResponseModel<NotificationDetailResponse>(ErrorCode.E400);

                var response = new NotificationDetailResponse
                {
                    Id = notification.Id,
                    TitleEn = notification.TitleEn,
                    TitleMm = notification.TitleMm,
                    DescEn = notification.DescEn,
                    DescMm = notification.DescMm,
                    SendDateAndTime = notification.SendDateAndTime,
                    Image = GetFileFullUrl(notification.Image),
                    Audience = notification.Audience,
                    Country = notification.Country, 
                    Province = notification.Province, 
                    District = notification.District, 
                    Township = notification.Township,
                    MemberType = notification.MemberType,
                    IsActive = notification.IsActive,
                    SendingStatus = notification.SendingStatus,
                    ProductType = notification.ProductType,
                    PolicyStatus = notification.PolicyStatus,
                };
               


                var log = CmsAuditLog(
                        objectGroup: EnumObjectGroup.Notification,
                        objectAction: EnumObjectAction.View,
                        objectId: notification.Id,
                        objectName: notification.TitleEn);

                return errorCodeProvider.GetResponseModel(ErrorCode.E0, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{httpContext?.HttpContext?.Request.Method} " +
                    $"=> {httpContext?.HttpContext?.Request.Path} " +
                    $"=> Exception " +
                    $"=> {JsonConvert.SerializeObject(ex)}");

                return errorCodeProvider.GetResponseModel<NotificationDetailResponse>(ErrorCode.E500);
            }
        }

        ResponseModel<PagedList<NotificationResponse>> INotificationRepository.GetList(NotificationRequest model)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.CmsNotification>()
                    .Query(x => x.IsDeleted == false);

                if(!string.IsNullOrEmpty(model.Title))
                {
                    query = query.Where(x => x.TitleEn.Contains(model.Title) || x.TitleMm.Contains(model.Title));
                }

                if (model.SendingStatus != null)
                {
                    query = query.Where(x => x.SendingStatus == model.SendingStatus.ToString());
                }

                if (model.IsActive != null)
                {
                    query = query.Where(x => x.IsActive == model.IsActive);
                }

                if (model.Audience != null)
                {
                    query = query.Where(x => x.Audience == model.Audience.ToString());
                }

                if (model.MemberType != null && model.MemberType != EnumIndividualMemberType.All)
                {
                    query = query.Where(x => x.MemberType == model.MemberType.ToString());
                }

                if (model.ProductType != null)
                {
                    query = query.Where(x => x.ProductType.Contains(model.ProductType));
                }

                if (model.Country != null)
                {
                    query = query.Where(x => x.Country.Contains(model.Country));
                }

                if (model.Province != null)
                {
                    query = query.Where(x => x.Province.Contains(model.Province));
                }

                if (model.District != null)
                {
                    query = query.Where(x => x.District.Contains(model.District));
                }

                if (model.Township != null)
                {
                    query = query.Where(x => x.Township.Contains(model.Township));
                }

                var count = query.Count();

                var result = query
                    .OrderByDescending(x => x.CreatedOn)
                    .Skip((model.Page - 1) * model.Size)
                    .Take(model.Size)
                    
                    .Select(x => new NotificationResponse
                    {
                        Id = x.Id,  
                        Title = x.TitleEn,
                        Image = x.Image,
                        SendingStatus = x.SendingStatus,
                        IsActive    = x.IsActive,
                        TargetedTo  = x.AudienceCount,
                        StartDateTime = x.SendDateAndTime,
                        UpdatedBy = x.UpdatedBy,
                        UpdatedOn   = x.UpdatedOn,
                        CreatedOn = x.CreatedOn,
                        CreatedBy = x.CreatedBy,
                        

                    })                    
                    .ToList();

                result?.ForEach(item =>
                {
                    item.Image = GetFileFullUrl(item.Image);
                });

                var data = new PagedList<NotificationResponse>(
                    source: result,
                    totalCount: count,
                    pageNumber: model.Page,
                    pageSize: model.Size);

                var log = CmsAuditLog(
                        objectGroup: EnumObjectGroup.Notification,
                        objectAction: EnumObjectAction.View);

                return errorCodeProvider.GetResponseModel(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{httpContext?.HttpContext?.Request.Method} " +
                    $"=> {httpContext?.HttpContext?.Request.Path} " +
                    $"=> Exception " +
                    $"=> {JsonConvert.SerializeObject(ex)}");

                return errorCodeProvider.GetResponseModel<PagedList<NotificationResponse>>(ErrorCode.E500);
            }
        }

        ResponseModel<NotificationResponse> INotificationRepository.Update(UpdateNotificationRequest model)
        {
            try
            {
                Console.WriteLine($"Cms => Noti => Update model {JsonConvert.SerializeObject(model)}");
                var notification = unitOfWork.GetRepository<Entities.CmsNotification>()
                    .Query(x => x.Id == model.Id)
                    .FirstOrDefault();

                if (notification == null) return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E400);

                #region #CheckStillCanUpdateNotiOrNot
                var isJobDone = unitOfWork.GetRepository<Entities.CmsNotificationJobLocker>()
                    .Query(x => x.NotiId == model.Id).Any();

                if(notification.SendingStatus == EnumNotiStatus.Sending.ToString()
                    || notification.SendingStatus == EnumNotiStatus.Sent.ToString()
                    || isJobDone == true)
                {

                    notification.TitleEn = model.TitleEn;
                    notification.TitleMm = model.TitleMm;
                    notification.DescMm = model.DescMm;
                    notification.DescEn = model.DescEn;

                    #region #upload-image
                    var imageName = "";
                    if (model.Image != null)
                    {
                        imageName = $"{Utils.GetDefaultDate().Ticks}-{model.Image.FileName}";
                        var uploadResult = azureStorage.UploadAsync(imageName, model.Image).Result;

                        if (uploadResult.Code == (int)HttpStatusCode.OK)
                        {
                            notification.Image = imageName;
                            notification.FullImageUrl = GetFileFullUrl(imageName);
                        }
                    }
                    #endregion

                    notification.UpdatedBy = GetCmsUser().Name;
                    notification.UpdatedOn = Utils.GetDefaultDate();


                    var memberNotiList = unitOfWork.GetRepository<Entities.MemberNotification>()
                    .Query(x => x.CmsNotificationId == model.Id)
                    .ToList();

                    memberNotiList?.ForEach(noti =>
                    {
                        noti.TitleEn = model.TitleEn;
                        noti.TitleMm = model.TitleMm;
                        noti.Message = model.DescEn;
                        noti.MessageMm = model.DescMm;
                        noti.ImageUrl = imageName;
                    }
                    );


                    unitOfWork.SaveChanges();

                    return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E0, new NotificationResponse
                    {
                        Id = notification.Id,
                        SendingStatus = notification.SendingStatus,
                    });


                    //return new ResponseModel<NotificationResponse> { Code = 400, Message = "Cannot update!. Notification is Sending or already Sent!" };
                }

                if (model.SendDateAndTime < Utils.GetDefaultDate())
                {
                    return new ResponseModel<NotificationResponse> { Code = 400, Message = "SendDateAndTime cannot be less than current datetime." };
                }

                //if (model.SendDateAndTime < Utils.GetDefaultDate().AddMinutes(5))
                //{
                //    return new ResponseModel<NotificationResponse>
                //    {
                //        Code = 400,
                //        Message = "SendDateAndTime must be greater than 5 minutes of current datetime due to schedule job creating process."
                //    };
                //}

                #endregion

                var oldData = JsonConvert.SerializeObject(notification);

                #region #TranslateCountryRelatedCodeToDesc
                var provinceDescListString = "";
                var districtDescListString = "";
                var townshipDescListString = "";

                if (!string.IsNullOrEmpty(model.Province))
                {
                    #region #Province
                    var provinceCodeList = model.Province.Split(",")?.ToList();
                    var provinceDescList = unitOfWork.GetRepository<Entities.Province>()
                            .Query(x => provinceCodeList.Contains(x.province_code))
                            .Select(x => x.province_eng_name)
                            .ToList();

                    provinceDescListString = string.Join(",", provinceDescList);

                    #endregion


                    #region #District
                    if (!string.IsNullOrEmpty(model.Province) && !string.IsNullOrEmpty(model.District))
                    {
                        var distinctCodeList = model.District.Split(",")?.ToList();
                        var districtDescList = unitOfWork.GetRepository<Entities.District>()
                            .Query(x => provinceCodeList.Contains(x.province_code) && distinctCodeList.Contains(x.district_code))
                            .Select(x => x.district_eng_name)
                            .ToList();

                        districtDescListString = string.Join(",", districtDescList);

                        if (!string.IsNullOrEmpty(model.Township))
                        {
                            var townshipCodeList = model.Township.Split(",")?.ToList();
                            var townshipDescList = unitOfWork.GetRepository<Entities.Township>()
                                .Query(x => distinctCodeList.Contains(x.district_code) && townshipCodeList.Contains(x.township_code))
                                .Select(x => x.township_eng_name)
                                .ToList();

                            townshipDescListString = string.Join(",", townshipDescList);
                        }
                    }
                    #endregion

                    

                    
                }
                #endregion

                model.Province = provinceDescListString;
                model.District = districtDescListString;
                model.Township = townshipDescListString;
                Console.WriteLine($"Cms => Noti => Update model.Province {model.Province} model.District {model.District} model.Township {model.Township}");

                if (!string.IsNullOrEmpty(model.TitleEn))
                {
                    notification.TitleEn = model.TitleEn;
                }

                if (!string.IsNullOrEmpty(model.TitleMm))
                {
                    notification.TitleMm = model.TitleMm;
                }

                if (!string.IsNullOrEmpty(model.DescEn))
                {
                    notification.DescEn = model.DescEn;
                }

                if (!string.IsNullOrEmpty(model.DescMm))
                {
                    notification.DescMm = model.DescMm;
                }

                #region #upload-image
                if (model.Image != null)
                {
                    string imageName = $"{Utils.GetDefaultDate().Ticks}-{model.Image.FileName}";
                    var uploadResult = azureStorage.UploadAsync(imageName, model.Image).Result;

                    if (uploadResult.Code == (int)HttpStatusCode.OK)
                    {
                        notification.Image = imageName;
                        notification.FullImageUrl = GetFileFullUrl(imageName);
                    }
                }
                #endregion

                if (!string.IsNullOrEmpty(model.Country))
                {
                    notification.Country = model.Country;
                }

                if (!string.IsNullOrEmpty(model.Province))
                {
                    notification.Province = model.Province;
                }

                if (!string.IsNullOrEmpty(model.District))
                {
                    notification.District = model.District;
                }

                if (!string.IsNullOrEmpty(model.Township))
                {
                    notification.Township = model.Township;
                }

                if (model.IsActive != null)
                {
                    notification.IsActive = model.IsActive;
                }

                if (model.MemberType != null)
                {
                    notification.MemberType = model.MemberType.ToString();
                }

                if (!string.IsNullOrEmpty(model.ProductType))
                {
                    notification.ProductType = model.ProductType;
                }

                if (!string.IsNullOrEmpty(model.PolicyStatus))
                {
                    notification.PolicyStatus = model.PolicyStatus;
                }

                if (model.Audience != null)
                {
                    notification.Audience = model.Audience.ToString();

                    if (model.Audience == EnumNotiAudience.All)
                    {
                        notification.AudienceCount = "All members";
                    }
                    else
                    {
                        notification.AudienceCount = "{0} members";

                        #region #UpdateAudienceCount

                        List<string>? memberIdList = new List<string>();

                        var memberQuery = unitOfWork.GetRepository<Entities.Member>()
                                .Query(x => x.IsVerified == true && x.IsActive == true);

                        if (model.MemberType != null && model.MemberType != EnumIndividualMemberType.All)
                        {
                            memberQuery = memberQuery.Where(x => x.MemberType == model.MemberType.ToString());
                        }

                        if (!string.IsNullOrEmpty(model.Country))
                        {
                            var countryList = model.Country.Split(",");
                            memberQuery = memberQuery.Where(x => countryList.Contains(x.Country));
                        }

                        if (!string.IsNullOrEmpty(model.Province))
                        {
                            var provinceList = model.Province.Split(",");
                            memberQuery = memberQuery.Where(x => provinceList.Contains(x.Province));
                        }

                        if (!string.IsNullOrEmpty(model.District))
                        {

                            var districtList = model.District.Split(",");
                            memberQuery = memberQuery.Where(x => districtList.Contains(x.District));
                        }

                        if (!string.IsNullOrEmpty(model.Township))
                        {
                            var townshipList = model.Township.Split(",");
                            memberQuery = memberQuery.Where(x => townshipList.Contains(x.Township));
                        }



                        if (model.ProductType?.Any() == true)
                        {
                            var values = model.ProductType.Trim().Split(",");
                            Expression<Func<Entities.Member, bool>> searchExpression = entity => false;

                            foreach (var value in values)
                            {
                                searchExpression = searchExpression.OrElse(entity => EF.Functions.Like(entity.ProductCodeList, $"%{value}%"));
                            }

                            memberQuery = memberQuery.Where(searchExpression);
                        }

                        if (model.PolicyStatus?.Any() == true)
                        {
                            var values = model.PolicyStatus.Trim().Split(",");
                            Expression<Func<Entities.Member, bool>> searchExpression = entity => false;

                            foreach (var value in values)
                            {
                                searchExpression = searchExpression.OrElse(entity => EF.Functions.Like(entity.PolicyStatusList, $"%{value}%"));
                            }

                            memberQuery = memberQuery.Where(searchExpression);
                        }


                        memberIdList = memberQuery.Select(x => $"{x.MemberId}").ToList();

                        notification.AudienceCount = $"{memberIdList?.Count} members";
                        unitOfWork.SaveChanges();


                        #endregion
                    }
                }

                

                notification.UpdatedBy = GetCmsUser().Name;
                notification.UpdatedOn = Utils.GetDefaultDate();

                #region #ScheduleSetup
                if (model.SendDateAndTime != null)
                {
                    notification.SendDateAndTime = model.SendDateAndTime;

                    this.backgroundJob.Delete($"{notification.JobId}");

                    if (model.SendNow == true)
                    {
                        var jobId = this.backgroundJob.Enqueue<aia_core.RecurringJobs.IRecurringJobRunner>((x) => x.SendNotiFromCms(notification.Id));
                        notification.JobId = int.Parse(jobId);
                    }
                    else
                    {


                        var scheduledTimespan = model.SendDateAndTime - Utils.GetDefaultDate();
                        Console.WriteLine($"notification.SendDateAndTime => Local => {model.SendDateAndTime} scheduledTimespan => {scheduledTimespan}");


                        var jobId = this.backgroundJob.Schedule<aia_core.RecurringJobs.IRecurringJobRunner>((x) => x.SendNotiFromCms(notification.Id), scheduledTimespan.Value);
                        notification.JobId = int.Parse(jobId);

                        Console.WriteLine($"Cms Noti Schedule Created! JobId {notification.JobId}");
                    }

                    
                }
                #endregion

                unitOfWork.SaveChanges();

                var log = CmsAuditLog(
                        objectGroup: EnumObjectGroup.Notification,
                        objectAction: EnumObjectAction.Update,
                        objectId: notification.Id,
                        objectName: notification.TitleEn,
                        oldData: oldData,
                        newData: System.Text.Json.JsonSerializer.Serialize(notification));

                

                return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E0, new NotificationResponse
                {
                    Id = notification.Id,
                    SendingStatus = EnumNotiStatus.Pending.ToString(),
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{httpContext?.HttpContext?.Request.Method} " +
                    $"=> {httpContext?.HttpContext?.Request.Path} " +
                    $"=> Exception " +
                    $"=> {JsonConvert.SerializeObject(ex)}");

                return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E500);
            };
        }
    }
}
