using aia_core.Entities;
using aia_core.Model.Cms.Response;
using aia_core.Model.Mobile.Request.Blog;
using aia_core.Model.Mobile.Request.Notification;
using aia_core.Model.Mobile.Response;
using aia_core.Model.Mobile.Response.Notification;
using aia_core.Services;
using aia_core.UnitOfWork;
using Azure.Core.GeoJson;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Newtonsoft.Json;
using aia_core.Model.Cms.Request;
using aia_core.Repository.Cms;
using System.Data;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Vml.Office;
using Irony.Parsing;

namespace aia_core.Repository.Mobile
{
    public interface INotificationRepository
    {
        ResponseModel<ExtendedPagedList<NotificationResponse>> GetList(NotificationRequest model);
        ResponseModel<NotificationResponse> GetDetail(Guid id);
        ResponseModel<NotificationResponse> Delete(Guid id);
        ResponseModel<NotificationResponse> UndoDelete(Guid id);

        ResponseModel<string> Read(Guid id);

        ResponseModel<NotiUnreadCount> GetUnreadCount();

        ResponseModel<NotiCommonDetailResponse> GetCommonDetail(Guid id);
    }

    public class NotificationRepository : BaseRepository, INotificationRepository
    {
        #region "const"

        private readonly ICommonRepository commonRepository;

        public NotificationRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork,
            ICommonRepository commonRepository)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.commonRepository = commonRepository;
        }

        ResponseModel<NotificationResponse> INotificationRepository.Delete(Guid id)
        {
            try
            {
                var notification = unitOfWork.GetRepository<Entities.MemberNotification>().Query(x => x.Id == id).FirstOrDefault();                

                if (notification == null)
                    return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E400);

                notification.IsDeleted = true;
                unitOfWork.SaveChanges();

                return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E0);
            }
            catch (Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E500);
            }
        }

        ResponseModel<NotificationResponse> INotificationRepository.UndoDelete(Guid id)
        {
            try
            {
                var notification = unitOfWork.GetRepository<Entities.MemberNotification>().Query(x => x.Id == id).FirstOrDefault();

                if (notification == null)
                    return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E400);

                notification.IsDeleted = false;
                unitOfWork.SaveChanges();

                return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E0);
            }
            catch(Exception ex)
            {
                MobileErrorLog(null,ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E500);
            }
        }

        ResponseModel<NotificationResponse> INotificationRepository.GetDetail(Guid id)
        {
            try
            {
                var entity = unitOfWork.GetRepository<Entities.MemberNotification>()
                    .Query(x => x.Id == id)
                    .FirstOrDefault();

                if (entity == null)
                    return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E400);


                entity.IsRead = true;
                unitOfWork.SaveChanges();


                var data = new NotificationResponse
                {
                    Id = entity.Id,
                    Message = entity.Message,
                    Type = entity.Type,
                    CreatedDate = entity.CreatedDate,
                    IsRead = entity.IsRead,

                    IsSytemNoti = entity.IsSytemNoti,
                    SystemNotiType = entity.SystemNotiType,
                    ProductId = entity.ProductId,
                    PromotionId = entity.PromotionId,
                    PropositionId = entity.PropositionId,
                };


                if (entity.Type == EnumNotificationType.Claim.ToString())
                {

                    data.ClaimData = new ClaimData()
                    {
                        ClaimId = entity.ClaimId,
                    };                    

                    var claimTran = unitOfWork.GetRepository<Entities.ClaimTran>()
                        .Query(x => x.ClaimId == new Guid(entity.ClaimId))
                        .FirstOrDefault();

                    if (claimTran != null)
                    {
                        data.ClaimData.PolicyNumber = claimTran.PolicyNo;
                        data.ClaimData.InsuredId = claimTran.InsuredClientNo;


                        var policy = unitOfWork.GetRepository<Entities.Policy>()
                            .Query(x => x.PolicyNo == claimTran.PolicyNo).FirstOrDefault();

                        if(policy != null)
                        {
                            var product = unitOfWork.GetRepository<Entities.Product>()
                            .Query(x => x.ProductTypeShort == policy.ProductType && x.IsActive == true && x.IsDelete == false).FirstOrDefault();

                            if(product != null && !string.IsNullOrEmpty(product.LogoImage))
                            {
                                data.Icon = GetFileFullUrl(EnumFileType.Product, product.LogoImage);
                            }
                        }
                    }
                    
                }
                else if (entity.Type == EnumNotificationType.Service.ToString())
                {

                    data.ServicingData = new ServicingData
                    {
                        ServicingId = entity?.ServicingId,
                        ServiceType = (EnumServiceType)Enum.Parse(typeof(EnumServiceType), entity.ServiceType),
                        ServiceStatus = entity?.ServiceStatus,
                    };
                }

                return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E0, data);
            }
            catch
            {
                return errorCodeProvider.GetResponseModel<NotificationResponse>(ErrorCode.E500);
            }
        }

        ResponseModel<ExtendedPagedList<NotificationResponse>> INotificationRepository.GetList(NotificationRequest model)
        {
            try
            {
                var memberId = GetMemberIDFromToken();



                model.MemberId = memberId;

                
                var queryStrings = this.PrepareListQuery(model);

                var count = unitOfWork.GetRepository<NotificationCount>()
                    .FromSqlRaw(queryStrings?.CountQuery, null, CommandType.Text)
                    .FirstOrDefault();

                var list = unitOfWork.GetRepository<NotificationResponse>()
                    .FromSqlRaw(queryStrings?.ListQuery, null, CommandType.Text)
                    .ToList();

               
                foreach ( var item in list )
                {
                    if( item.Type == EnumNotificationType.Claim.ToString()) 
                    {
                        item.Icon = !string.IsNullOrEmpty(item.Icon) ? GetFileFullUrl(EnumFileType.Product, item.Icon) : "";

                        item.ClaimData = new ClaimData
                        { 
                            ClaimId = item.ClaimId,
                            InsuredId = item.InsuredId,
                            PolicyNumber = item.PolicyNumber,
                        };
                    }

                    else if (item.Type == EnumNotificationType.Service.ToString())
                    {
                        
                        item.ServicingData = new ServicingData
                        {
                            ServicingId = item?.ServicingId,
                            ServiceType = item.ServiceType != null ?
                            (EnumServiceType)Enum.Parse(typeof(EnumServiceType), item.ServiceType)
                            : null,
                            ServiceStatus = item?.ServiceStatus,
                        };

                    }
                    else if (item.Type == EnumNotificationType.Others.ToString()
                        && item.SystemNotiType == EnumSystemNotiType.Announcement.ToString())
                    {
                        item.Message = item.TitleEn;
                        item.MessageMm = item.TitleMm;
                    }
                    else if (item.Type == EnumNotificationType.Others.ToString()
                       && item.SystemNotiType == EnumSystemNotiType.Payment.ToString()
                       && !string.IsNullOrEmpty(item.PremiumPolicyNo))
                    {
                        var productTypeCode = unitOfWork.GetRepository<Policy>()
                            .Query(x => x.PolicyNo == item.PremiumPolicyNo)
                            .Select(x => x.ProductType)
                            .FirstOrDefault();

                        if(!string.IsNullOrEmpty(productTypeCode))
                        {
                            var product = unitOfWork.GetRepository<Product>()
                                .Query(x => x.ProductTypeShort == productTypeCode && x.IsActive == true && x.IsDelete == false)
                                .FirstOrDefault();
                            if (product != null && !string.IsNullOrEmpty(product.LogoImage))
                            {
                                item.Icon = GetFileFullUrl(EnumFileType.Product, product.LogoImage);
                            }
                        }

                    }
                }


                var unReadCount = 0;
                unReadCount = GetUnreadCount(memberId.Value);

                var data = new ExtendedPagedList<NotificationResponse>(
                source: list,
                totalCount: count?.SelectCount ?? 0,
                pageNumber: (int)model.PageIndex,
                pageSize: (int)model.PageSize,
                unreadCount: unReadCount);

                return errorCodeProvider.GetResponseModel<ExtendedPagedList<NotificationResponse>>(ErrorCode.E0, data);
            }
            catch(Exception ex)
            {
                MobileErrorLog("Noti => GetList", ex.Message,JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<ExtendedPagedList<NotificationResponse>>(ErrorCode.E500);
            }
        }

        ResponseModel<string> INotificationRepository.Read(Guid id)
        {
            var notification = unitOfWork.GetRepository<Entities.MemberNotification>().Query(x => x.Id == id)
                    .FirstOrDefault();

            if (notification == null)
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);


            notification.IsRead = true;
            unitOfWork.SaveChanges();
            return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0);
        }

        private QueryStrings PrepareListQuery(NotificationRequest model)
        {
            #region #CountQuery
            var countQuery = @"SELECT COUNT(MemberNotification.ID) as SelectCount ";
            var asQuery = @"";
            #endregion

            #region #DataQuery
            var dataQuery = @"SELECT 
                                ID,
                                [Message],
                                [Type],
                                MemberNotification.CreatedDate,
                                IsRead,
                                IsSytemNoti,
                                SystemNotiType,                
                                ProductID,
                                PropositionID,
                                PromotionID,
                                MemberNotification.ClaimID,
                                ClaimTran.InsuredClientNo,
                                ClaimTran.PolicyNo,
                                Product.Logo_Image As Icon,
                                CASE
                                    WHEN [Type] = 'Claim' THEN MemberNotification.ClaimStatus
                                    WHEN [Type] = 'Service' THEN MemberNotification.ServiceStatus
                                END AS Status,
                                MemberNotification.ClaimStatusCode AS StatusCode,
                                MemberNotification.ServicingId as ServicingId,
                                MemberNotification.ServiceType as ServiceType,
                                MemberNotification.ServiceStatus as ServiceStatus,
                                MemberNotification.PremiumPolicyNo as PolicyNumber,
                                MemberNotification.MessageMm as MessageMm,
                                MemberNotification.TitleEn as TitleEn, 
                                MemberNotification.TitleMm as TitleMm, 
                                MemberNotification.CommonKeyId as CommonKeyId, 
                                MemberNotification.PremiumPolicyNo as PremiumPolicyNo "; 
            #endregion

            #region #FromQuery
            var fromQuery = $@"FROM 
                                    MemberNotification
                                LEFT JOIN 
                                    ClaimTran ON ClaimTran.ClaimID = MemberNotification.ClaimID                                     
                                LEFT JOIN 
                                    Policies ON Policies.Policy_No = ClaimTran.PolicyNo
                                LEFT JOIN 
                                    Product ON Product.Product_Type_Short = Policies.Product_Type AND Product.Is_Active = 1 AND Product.Is_Delete = 0 ";
            #endregion

            #region #GroupQuery

            var groupQuery = @"";
            #endregion

            #region #OrderQuery
            var orderQuery = @"Order by MemberNotification.CreatedDate desc ";
            #endregion



            #region #FilterQuery

            var filterQuery = $@"WHERE MemberNotification.MemberID = '{model.MemberId}'
								AND MemberNotification.IsDeleted = 0 ";

            if (model.IsRead != null && model.IsRead == true)
            {
                filterQuery += @"AND MemberNotification.IsRead = 1 ";
            }
            else if (model.IsRead != null && model.IsRead == false)
            {
                filterQuery += @"AND MemberNotification.IsRead = 0 ";
            }

            if (model.NotificationType != null && model.NotificationType != EnumNotificationType.All)
            {
                filterQuery += @"AND MemberNotification.Type = '" + model.NotificationType.ToString() + "' ";
            }

            if (model.ClaimStatus != null && model.ClaimStatus != EnumClaimStatusDesc.All.ToString())
            {
                filterQuery += @"AND MemberNotification.ClaimStatus = '" + model.ClaimStatus.ToString() + "' ";
            }

            if (!string.IsNullOrEmpty(model.ServiceStatus)  && model.ServiceStatus != EnumServiceStatus.All.ToString())
            {
                

                filterQuery += @"AND MemberNotification.ServiceStatus = '" + model.ServiceStatus + "' ";
            }

            #endregion

            #region #OffsetQuery
            var offsetQuery = "";
            offsetQuery = $"OFFSET {(model.PageIndex - 1) * model.PageSize} ROWS FETCH NEXT {model.PageSize} ROWS ONLY";
            #endregion
            
            

            countQuery = $"{countQuery}{fromQuery}{filterQuery}{groupQuery}{asQuery}";
            var listQuery = $"{dataQuery}{fromQuery}{filterQuery}{groupQuery}{orderQuery}{offsetQuery}";

           
            return new QueryStrings { CountQuery = countQuery, ListQuery = listQuery };
        }
        #endregion


        public int GetUnreadCount(Guid userId)
        { 
            var unreadCount = 0;
            try
            {
                unreadCount = unitOfWork.GetRepository<Entities.MemberNotification>()
                    .Query(x => x.MemberId == userId && x.IsRead == false && x.IsDeleted == false)
                    .Count();
            }
            catch { }
            

            return unreadCount;
        }

        ResponseModel<NotiUnreadCount> INotificationRepository.GetUnreadCount()
        {
            try
            {
                var memberId = GetMemberIDFromToken();

                var unReadCount = GetUnreadCount(memberId.Value);

                return errorCodeProvider.GetResponseModel(ErrorCode.E0,
                    new NotiUnreadCount
                    {
                        UnreadCount = unReadCount,
                    });
            }
            catch (Exception ex)
            {
                MobileErrorLog("Noti => GetUnreadCount", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<NotiUnreadCount>(ErrorCode.E500);
            }
        }

        ResponseModel<NotiCommonDetailResponse> INotificationRepository.GetCommonDetail(Guid id)
        {
            try
            {
                var memberId = GetMemberIDFromToken();
                Entities.MemberNotification? memberNoti = unitOfWork.GetRepository<Entities.MemberNotification>()
                    .Query(x => x.CmsNotificationId == id && x.MemberId == memberId)
                    .FirstOrDefault();

                if (memberNoti == null)
                    memberNoti = unitOfWork.GetRepository<Entities.MemberNotification>()
                    .Query(x => x.Id == id)
                    .FirstOrDefault();

                if (memberNoti == null)
                    return errorCodeProvider.GetResponseModel<NotiCommonDetailResponse>(ErrorCode.E400);


                return errorCodeProvider.GetResponseModel(ErrorCode.E0, 
                    new NotiCommonDetailResponse { 
                        Id = memberNoti.Id,
                        TitleEn = memberNoti.TitleEn,
                        TitleMm = memberNoti.TitleMm,
                        MessageEn = memberNoti.Message,
                        MessageMm = memberNoti.MessageMm,
                        CreatedDate = memberNoti.CreatedDate,
                        ImageUrl = GetFileFullUrl(memberNoti.ImageUrl),
                    });
            }
            catch (Exception ex)
            {
                

                return errorCodeProvider.GetResponseModel<NotiCommonDetailResponse>(ErrorCode.E500);
            }
        }
    }
}
