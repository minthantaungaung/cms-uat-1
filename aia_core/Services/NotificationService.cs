using aia_core.Entities;
using aia_core.Model.Cms.Request.Notification;
using aia_core.Model.Mobile.Response;
using aia_core.Model.Mobile.Response.AiaILApiResponse;
using aia_core.UnitOfWork;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using FastMember;
using FirebaseAdmin.Messaging;
using Google.Api.Gax.ResourceNames;
using Irony.Parsing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace aia_core.Services
{
    public interface INotificationService
    {
        public string SendClaimNoti(Guid appMemberId, Guid claimId, EnumClaimStatus claimStatus, string productName, string statusReason = null);

        Task<string> SendServicingNoti(Guid appMemberId, Guid servicingId, EnumServicingStatus servicingStatus, EnumServiceType serviceType
            , string? policyNo = null);
        Task SendNewSetupItemNoti(EnumSystemNotiType type, string id);
        Task SendNotification(NotificationMessage data);

        Task<BatchResponse> SendNotificationSdkMulticastMessage(NotificationMessage data, string notiId, int batch);


        Task SendNotiFromCms(Guid? notiId);
      
    }
    public class NotificationService : INotificationService
    {
        protected readonly IErrorCodeProvider errorCodeProvider;
        protected readonly IUnitOfWork<Entities.Context> unitOfWork;
        private readonly ITemplateLoader templateLoader;
        private readonly IServiceScopeFactory serviceFactory;

        public NotificationService(IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork, ITemplateLoader templateLoader,IServiceScopeFactory serviceFactory)
        {
            this.errorCodeProvider = errorCodeProvider;
            this.unitOfWork = unitOfWork;
            this.templateLoader = templateLoader;
            this.serviceFactory = serviceFactory;
        }

        public async Task SendNotification(NotificationMessage data)
        {
            Console.WriteLine($"SendNotification data: {JsonConvert.SerializeObject(data)}");

            try
            {

                using (var scope = this.serviceFactory.CreateScope())
                {
                    var _unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork<aia_core.Entities.Context>>();

                    var deviceList = _unitOfWork.GetRepository<Entities.MemberDevice>()
                    .Query(x => x.MemberId == data.MemberId.ToString())
                    .ToList();

                    deviceList?.ForEach(device =>
                    {

                        try
                        {
                            var message = new FirebaseAdmin.Messaging.Message
                            {

                                Token = device.PushToken,

                                Notification = new Notification
                                {
                                    Title = data.Title,
                                    Body = data.Message,
                                    ImageUrl = data.ImageUrl,

                                },
                                Data = new Dictionary<string, string>
                        {
                            { "Title", data.Title },
                            { "Body",  data.Message },
                            { "ImageUrl", data.ImageUrl },
                            { "Code", data.NotificationType?.ToString() },
                            { "ServicingId", data.ServicingId?.ToString() },
                            { "ServiceType", data.ServiceType?.ToString() },
                            { "ClaimId",  data.ClaimId?.ToString() },
                            { "IsSytemNoti", data.IsSytemNoti?.ToString() },
                            { "SystemNotiType", data.SystemNotiType?.ToString() },
                            { "ProductId", data.ProductId },
                            { "PromotionId", data.PromotionId },
                            { "PropositionId",  data.PropositionId },
                            { "NotificationId",  data.NotificationId },
                            { "PasswordChangedDate",  data.SystemNotiType == EnumSystemNotiType.PasswordChange ? $"{Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)}" : "" },
                            { "PolicyNumber",  data.PolicyNumber },
                            { "CommonKeyId",  data.CommonKeyId },

                        },

                            };

                            var messaging = FirebaseMessaging.DefaultInstance;
                            var result = messaging.SendAsync(message).Result;
                              
                            Console.WriteLine($"SendNotification Success: {data?.MemberId} {device?.PushToken} {JsonConvert.SerializeObject(result)}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"SendNotification Ex Detail => {data?.MemberId} {device?.PushToken}",
                                $"{JsonConvert.SerializeObject(data)} {JsonConvert.SerializeObject(ex)}");
                        }


                    });
                }

                


            }
            catch (Exception ex)
            {
                
            }
        }

        string INotificationService.SendClaimNoti(Guid appMemberId, Guid claimId, EnumClaimStatus claimStatus, string productName, string statusReason = null)
        {
            string? notiMsg = "";

            try
            {
                var messageCode = "";
                var claimStatusDesc = "";

                switch (claimStatus)
                {
                    case EnumClaimStatus.RC: messageCode = EnumPushMessageCode.ClaimPushMsg_RC.ToString(); claimStatusDesc = EnumClaimStatusDesc.Received.ToString(); break;
                    case EnumClaimStatus.AL: messageCode = EnumPushMessageCode.ClaimPushMsg_AL.ToString(); claimStatusDesc = EnumClaimStatusDesc.Approved.ToString(); break;
                    case EnumClaimStatus.FU: messageCode = EnumPushMessageCode.ClaimPushMsg_FU.ToString(); claimStatusDesc = "Followed-up"; break;
                    case EnumClaimStatus.PD: messageCode = EnumPushMessageCode.ClaimPushMsg_PD.ToString(); claimStatusDesc = EnumClaimStatusDesc.Paid.ToString(); break;
                    case EnumClaimStatus.CS: messageCode = EnumPushMessageCode.ClaimPushMsg_CS.ToString(); claimStatusDesc = EnumClaimStatusDesc.Closed.ToString(); break;
                    case EnumClaimStatus.WD: messageCode = EnumPushMessageCode.ClaimPushMsg_WD.ToString(); claimStatusDesc = EnumClaimStatusDesc.Withdrawn.ToString(); break;
                    case EnumClaimStatus.RJ: messageCode = EnumPushMessageCode.ClaimPushMsg_RJ.ToString(); claimStatusDesc = EnumClaimStatusDesc.Rejected.ToString(); break;
                }

                IDictionary<string, NotificationMessage>? notiMsgListJson = null;
                notiMsgListJson = templateLoader.GetNotiMsgListJson();

                if (!string.IsNullOrEmpty(messageCode) && (notiMsgListJson != null && notiMsgListJson.Any()))
                {

                    try
                    {
                        Utils.MobileErrorLog("SendClaimNoti", $"productName => {productName}", JsonConvert.SerializeObject(notiMsgListJson), "", "", unitOfWork);
                        Utils.CmsErrorLog("SendClaimNoti", $"productName => {productName}", JsonConvert.SerializeObject(notiMsgListJson), "", "", unitOfWork);
                    }
                    catch (Exception ex) { }



                    var notiMessage = notiMsgListJson[messageCode.ToString()];
                    if (notiMessage != null)
                    {
                        NotificationMessage msg = new NotificationMessage
                        {
                            Title = notiMessage.Title,
                            Message = notiMessage.Message,
                            ImageUrl = notiMessage.ImageUrl,
                        };

                        msg.Message = string.Format(msg.Message, productName);

                        if (!string.IsNullOrEmpty(statusReason))
                        {
                            var reason = $" due to \"{statusReason}\".";
                            msg.Message = msg.Message.Replace(".", ""); // remove full stop
                            msg.Message = $"{msg.Message}{reason}";
                        }

                        notiMsg = msg.Message;

                        var notification = new Entities.MemberNotification()
                        {
                            Id = Guid.NewGuid(),
                            MemberId = appMemberId,
                            ClaimId = claimId.ToString(),
                            CreatedDate = Utils.GetDefaultDate(),
                            IsDeleted = false,
                            IsRead = false,
                            Type = EnumNotificationType.Claim.ToString(),
                            Message = msg.Message,
                            ClaimStatusCode = claimStatus.ToString(), //TODO
                            ClaimStatus = claimStatusDesc, //TODO
                        };


                        unitOfWork.GetRepository<Entities.MemberNotification>().Add(notification);
                        unitOfWork.SaveChanges();

                        SendNotification(new NotificationMessage
                        {
                            MemberId = appMemberId,
                            NotificationType = EnumNotificationType.Claim,
                            ClaimId = claimId,
                            Message  = msg.Message,
                            Title = msg.Title,
                            ImageUrl = msg.ImageUrl,
                        });
                    }


                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

            return notiMsg;
        }

        public async Task<string> SendServicingNoti(Guid appMemberId, Guid servicingId, EnumServicingStatus servicingStatus, EnumServiceType serviceType, string? policyNo = null)
        {
            string? notiMsg = "";

            try
            {
                var messageCode = "";


                if (servicingStatus == EnumServicingStatus.Received)
                {
                    messageCode = "Servicing_Received";
                }
                else if (servicingStatus == EnumServicingStatus.Approved)
                {
                    messageCode = "Servicing_Approved";
                }
                else if (servicingStatus == EnumServicingStatus.NotApproved)
                {
                    messageCode = "Servicing_NotApproved";
                }
                else if (servicingStatus == EnumServicingStatus.Paid)
                {
                    messageCode = "Servicing_Paid";
                }


                IDictionary<string, NotificationMessage>? notiMsgListJson = null;
                notiMsgListJson = templateLoader.GetNotiMsgListJson();

                
                

                if (!string.IsNullOrEmpty(messageCode) && (notiMsgListJson != null && notiMsgListJson.Any()))
                {
                    var notiMessage = notiMsgListJson[messageCode.ToString()];

                    Utils.MobileErrorLog("SendServicingNoti",
                     $"servicingId => {servicingId}, " +
                     $"servicingStatus => {servicingStatus}, " +
                     $"serviceType => {serviceType}, " +
                     $"messageCode => {messageCode}, " +
                     $"notiMessage => {JsonConvert.SerializeObject(notiMessage)}"
                     , $"notiMsgListJson => {JsonConvert.SerializeObject(notiMsgListJson)}", "", "", unitOfWork);

                    if (notiMessage != null)
                    {
                        NotificationMessage msg = new NotificationMessage
                        {
                            Title = notiMessage.Title,
                            Message = notiMessage.Message,
                            ImageUrl = notiMessage.ImageUrl,
                        };

                        // var serviceType1 = unitOfWork.GetRepository<Entities.ServiceType>()
                        //     .Query(x => x.ServiceTypeEnum == serviceType.ToString())
                        //     .FirstOrDefault();

                        //var requestType = serviceType1?.ServiceTypeNameEn;
                        EnumServiceType enumValue = serviceType;
                        string requestType = "";
                        if (enumValue == EnumServiceType.PolicyHolderInformation)
                            requestType = "Policy Holder Information";
                        else if (enumValue == EnumServiceType.InsuredPersonInformation)
                            requestType = "Insured Person Information";
                        else if (enumValue == EnumServiceType.BeneficiaryInformation)
                            requestType = "Beneficiary Information";
                        else if (enumValue == EnumServiceType.LapseReinstatement)
                            requestType = "Lapse Reinstatement";
                        else if (enumValue == EnumServiceType.HealthRenewal)
                            requestType = "Health Renewal";
                        else if (enumValue == EnumServiceType.PolicyLoanRepayment)
                            requestType = "Policy Loan Repayment";
                        else if (enumValue == EnumServiceType.AcpLoanRepayment)
                            requestType = "ACP Loan Repayment";
                        else if (enumValue == EnumServiceType.AdHocTopup)
                            requestType = "Ad Hoc Topup";
                        else if (enumValue == EnumServiceType.PartialWithdraw)
                            requestType = "Partial Withdraw";
                        else if (enumValue == EnumServiceType.PolicyLoan)
                            requestType = "Policy Loan";
                        else if (enumValue == EnumServiceType.PolicyPaidUp)
                            requestType = "Policy Paid Up";
                        else if (enumValue == EnumServiceType.PolicySurrender)
                            requestType = "Policy Surrender";
                        else if (enumValue == EnumServiceType.PaymentFrequency)
                            requestType = "Payment Frequency";
                        else if (enumValue == EnumServiceType.SumAssuredChange)
                            requestType = "Sum Assured Change";
                        else if (enumValue == EnumServiceType.RefundOfPayment)
                            requestType = "Refund Of Payment";

                        if (serviceType != EnumServiceType.PolicyHolderInformation
                            && serviceType != EnumServiceType.InsuredPersonInformation)
                        {
                            msg.Message = string.Format(msg.Message, requestType, $"for {policyNo}");
                        }
                        else
                        {
                            msg.Message = string.Format(msg.Message, requestType, "");
                        }


                        notiMsg = msg.Message;
                        using (var scope = this.serviceFactory.CreateScope())
                        {
                            var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork<aia_core.Entities.Context>>();

                            var notification = new Entities.MemberNotification()
                            {
                                Id = Guid.NewGuid(),
                                MemberId = appMemberId,
                                ServicingId = servicingId.ToString(),
                                CreatedDate = Utils.GetDefaultDate(),
                                IsDeleted = false,
                                IsRead = false,
                                Type = EnumNotificationType.Service.ToString(),
                                Message = msg.Message,
                                ServiceType = serviceType.ToString(), //New
                                ServiceStatus = servicingStatus.ToString(), //New
                            };

                            await unitOfWork.GetRepository<Entities.MemberNotification>().AddAsync(notification);
                            await unitOfWork.SaveChangesAsync();
                        }

                        SendNotification(new NotificationMessage
                        {
                            MemberId = appMemberId,
                            NotificationType = EnumNotificationType.Service,
                            ServiceType = serviceType,
                            ServicingId = servicingId,
                            Message = msg.Message,
                            Title = msg.Title,
                            ImageUrl = msg.ImageUrl,
                        });
                    }
                }

                    
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Notification Service Error | Ex message : {ex.Message} | Exception {ex}");
                throw ex;
            }

            return notiMsg;
        }

        public async Task SendNewSetupItemNoti(EnumSystemNotiType type, string id)
        {
            try
            {
                if (type == EnumSystemNotiType.Product || type == EnumSystemNotiType.Proposition
                    || type == EnumSystemNotiType.Promotion)
                {

                    var notiMsgListJson = templateLoader.GetNotiMsgListJson();
                    var messageCode = "";
                    NotificationMessage? message = null;

                    var memberIdList = unitOfWork.GetRepository<Entities.Member>()
                        .Query(x => x.IsVerified == true && x.IsActive == true)
                        .Select(x => $"{x.MemberId}")
                        .ToList();

                    if(memberIdList?.Any() == true) 
                    {

                        #region Notification
                        var notification = new Entities.MemberNotification()
                        {


                            CreatedDate = Utils.GetDefaultDate(),
                            IsDeleted = false,
                            IsRead = false,
                            Type = EnumNotificationType.Others.ToString(),
                            IsSytemNoti = true,
                            SystemNotiType = type.ToString(),
                            IsScheduled = false,
                            IsScheduledDone = false,
                            JobId = "",
                        };

                        notification.SystemNotiType = type.ToString();


                        var pushNotification = new NotificationMessage();

                        if (EnumSystemNotiType.Product == type)
                        {
                            messageCode = EnumPushMessageCode.Product.ToString();
                            message = notiMsgListJson[messageCode];

                            notification.ProductId = id;
                            notification.Message = message?.Message;

                            pushNotification.ProductId = id;
                            pushNotification.Message = message?.Message;
                        }
                        if (EnumSystemNotiType.Proposition == type)
                        {
                            messageCode = EnumPushMessageCode.Proposition.ToString();
                            message = notiMsgListJson[messageCode];

                            notification.Message = message?.Message;
                            notification.PropositionId = id;

                            pushNotification.PropositionId = id;
                            pushNotification.Message = message?.Message;



                        }
                        else if (EnumSystemNotiType.Promotion == type)
                        {
                            messageCode = EnumPushMessageCode.Promotion.ToString();
                            message = notiMsgListJson[messageCode];

                            notification.Message = message?.Message;
                            notification.PromotionId = id;

                            pushNotification.PromotionId = id;

                            var blog = unitOfWork.GetRepository<Entities.Blog>()
                           .Query(x => x.Id == new Guid(id) && x.IsActive == true && x.IsDelete == false)
                           .FirstOrDefault();

                            if (blog != null && message != null)
                            {
                                notification.Message = string.Format(message?.Message, blog?.TitleEn);
                                pushNotification.Message = notification.Message;
                            }
                        }
                        #endregion

                        #region #InsertBatchMessage

                        var notificationList = memberIdList.Select(memberId => new Entities.MemberNotification
                        {
                            
                            MemberId = Guid.Parse(memberId),
                            Id = Guid.NewGuid(),
                            CreatedDate = notification.CreatedDate,
                            IsDeleted = notification.IsDeleted,
                            IsRead = notification.IsRead,
                            Type = notification.Type,
                            IsSytemNoti = notification.IsSytemNoti,
                            SystemNotiType = notification.SystemNotiType,
                            IsScheduled = notification.IsScheduled,
                            IsScheduledDone = notification.IsScheduledDone,
                            JobId = notification.JobId,

                            Message = notification.Message,
                            ProductId = notification.ProductId,
                            PromotionId = notification.PromotionId,
                            PropositionId = notification.PropositionId,

                        }).ToList();
                        

                        unitOfWork.GetRepository<Entities.MemberNotification>().Add(notificationList);
                        unitOfWork.SaveChanges();

                        Console.WriteLine("SendNewSetupItemNoti => BatchInsert => MemberNotification");

                        #endregion




                        #region #SendBatchMessage

                        var memberDeviceList = unitOfWork.GetRepository<Entities.MemberDevice>()
                            .Query(x => memberIdList.Contains(x.MemberId) && !string.IsNullOrEmpty(x.PushToken))
                            .ToList();

                        if (memberDeviceList?.Any() == true)
                        {
                            var take = 500;
                            var current = 1;
                            var hasNext = true;

                            do
                            {
                                var skip = (current - 1) * take;

                                var customDeviceList = memberDeviceList.Skip(skip).Take(take)
                                .ToList();

                                if (customDeviceList?.Any() == true)
                                {

                                    

                                    //pushNotification.MemberId = memberId;
                                    pushNotification.NotificationType = EnumNotificationType.Others;
                                    pushNotification.IsSytemNoti = true;
                                    pushNotification.SystemNotiType = type;
                                    pushNotification.Message = notification.Message;
                                    pushNotification.Title = message?.Title;
                                    pushNotification.ImageUrl = message?.ImageUrl;
                                    pushNotification.PushTokenList = customDeviceList.Select(x => x.PushToken).ToList();

                                    var batchResponse = SendNotificationSdkMulticastMessage(pushNotification, id.ToString(), current);

                                }
                                else
                                {
                                    hasNext = false;
                                }

                                current++;

                            } while (hasNext);
                        }
                        #endregion
                    }



                }

            }
            catch (Exception ex)
            {
                Utils.MobileErrorLog("SendSystemNoti Ex", ex.Message, JsonConvert.SerializeObject(ex), "", "", unitOfWork);

            }
        }


        public async Task<BatchResponse> SendNotificationSdkMulticastMessage(NotificationMessage data, string notiId, int batch)
        {

            BatchResponse? batchResponse = null;
            try
            {
                Console.WriteLine($"SendNotificationSdkMulticastMessage " +
                                        $" => notiId => {notiId} " +
                                        $" => batch => {batch} ");

                var message = new FirebaseAdmin.Messaging.MulticastMessage
                {

                    Tokens = data.PushTokenList,

                    Notification = new Notification
                    {
                        Title = data.Title,
                        Body = data.Message,
                        ImageUrl = data.ImageUrl,

                    },
                    Data = new Dictionary<string, string>
                        {
                            { "Title", data.Title },
                            { "Body",  data.Message },
                            { "ImageUrl", data.ImageUrl },
                            { "Code", data.NotificationType?.ToString() },
                            { "ServicingId", data.ServicingId?.ToString() },
                            { "ServiceType", data.ServiceType?.ToString() },
                            { "ClaimId",  data.ClaimId?.ToString() },
                            { "IsSytemNoti", data.IsSytemNoti?.ToString() },
                            { "SystemNotiType", data.SystemNotiType?.ToString() },
                            { "ProductId", data.ProductId },
                            { "PromotionId", data.PromotionId },
                            { "PropositionId",  data.PropositionId },
                            { "NotificationId",  data.NotificationId },
                            { "PasswordChangedDate",  data.SystemNotiType == EnumSystemNotiType.PasswordChange ? $"{Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)}" : "" },
                            { "PolicyNumber",  data.PolicyNumber },
                        },

                };

                var messaging = FirebaseMessaging.DefaultInstance;
                batchResponse = messaging.SendEachForMulticastAsync(message).Result;

                #region #LogEachUser            

                using (var scope = serviceFactory.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<Context>>();

                    unitOfWork.GetRepository<Entities.PushNotificationLog>().Add(new PushNotificationLog
                    {
                        Id = $"{Guid.NewGuid()}",
                        NotificationId = notiId,
                        CreatedOn = DateTime.UtcNow,
                        PushToken = JsonConvert.SerializeObject(message),
                        SentOn = DateTime.UtcNow,
                        FirebaseResult = JsonConvert.SerializeObject(batchResponse),
                    });


                    unitOfWork.SaveChanges();

                    Console.WriteLine($"SendNotificationSdkMulticastMessage " +
                                        $" => notiId => {notiId} " +
                                        $" => batch => {batch} " +
                                        $" => PushNotificationLog" +
                                        $" => Done!");
                }

            }
            catch { }


            #endregion

            return batchResponse;

        }



        public async Task SendNotiFromCms(Guid? notiId)
        {
            try
            {
                Console.WriteLine($"NotificationService => SendNotiFromCms => notiId => {notiId} {Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)}");

                var notification = unitOfWork.GetRepository<Entities.CmsNotification>()
                    .Query(x => x.Id == notiId && x.IsDeleted == false && x.IsActive == true)
                    .FirstOrDefault();

                if(notification != null)
                {
                    

                    Entities.CmsNotificationJobLocker? notificationJobLocker = null; 

                    notificationJobLocker = unitOfWork.GetRepository<Entities.CmsNotificationJobLocker>()
                        .Query(x => x.NotiId == notification.Id)
                        .FirstOrDefault();

                    

                    if (notificationJobLocker == null) 
                    {

                        Console.WriteLine($"NotificationService => SendNotiFromCms => CmsNotificationJobLocker => notiId => {notiId} => Non Locked!");

                        List<string>? memberIdList = new List<string>();

                        if(notification.Audience == EnumNotiAudience.All.ToString())
                        {
                            memberIdList = unitOfWork.GetRepository<Entities.Member>()
                        .Query(x => x.IsVerified == true && x.IsActive == true)
                        .Select(x => $"{x.MemberId}")
                        .ToList();
                        }
                        else //Manual
                        {

                            var memberQuery = unitOfWork.GetRepository<Entities.Member>()
                            .Query(x => x.IsVerified == true && x.IsActive == true);

                            if (!string.IsNullOrEmpty(notification.MemberType) && notification.MemberType != EnumIndividualMemberType.All.ToString())
                            {
                                memberQuery = memberQuery.Where(x => x.MemberType == notification.MemberType);
                            }                           

                            if (!string.IsNullOrEmpty(notification.Country))
                            {
                                var countryList = notification.Country.Split(",");
                                memberQuery = memberQuery.Where(x => countryList.Contains(x.Country));
                            }

                            if (!string.IsNullOrEmpty(notification.Province))
                            {
                                var provinceList = notification.Province.Split(",");
                                memberQuery = memberQuery.Where(x => provinceList.Contains(x.Province));
                            }

                            if (!string.IsNullOrEmpty(notification.District))
                            {

                                var districtList = notification.District.Split(",");
                                memberQuery = memberQuery.Where(x => districtList.Contains(x.District));
                            }

                            if (!string.IsNullOrEmpty(notification.Township))
                            {
                                var townshipList = notification.Township.Split(",");
                                memberQuery = memberQuery.Where(x => townshipList.Contains(x.Township));
                            }



                            if(notification.ProductType?.Any() == true)
                            {
                                var values = notification.ProductType.Trim().Split(",");
                                Expression<Func<Entities.Member, bool>> searchExpression = entity => false;

                                foreach (var value in values)
                                {
                                    searchExpression = searchExpression.OrElse(entity => EF.Functions.Like(entity.ProductCodeList, $"%{value}%"));
                                }

                                memberQuery = memberQuery.Where(searchExpression);
                            }

                            if (notification.PolicyStatus?.Any() == true)
                            {
                                var values = notification.PolicyStatus.Trim().Split(",");
                                Expression<Func<Entities.Member, bool>> searchExpression = entity => false;

                                foreach (var value in values)
                                {
                                    searchExpression = searchExpression.OrElse(entity => EF.Functions.Like(entity.PolicyStatusList, $"%{value}%"));
                                }

                                memberQuery = memberQuery.Where(searchExpression);
                            }


                            memberIdList = memberQuery.Select(x => $"{x.MemberId}").ToList();

                            #region #UpdateAudienceCount
                            notification.AudienceCount = $"{memberIdList?.Count} members";
                            unitOfWork.SaveChanges();
                            #endregion

                        }




                        if (memberIdList?.Any() == true)
                        {
                            

                            #region #CmsNotificationJobLocker
                            notificationJobLocker = new CmsNotificationJobLocker
                            {
                                Id = Guid.NewGuid(),
                                NotiId = notification.Id,
                                Status = "Sending",
                                CreatedOn = Utils.GetDefaultDate(),
                            };

                            unitOfWork.GetRepository<Entities.CmsNotificationJobLocker>()
                                .Add(notificationJobLocker);
                            unitOfWork.SaveChanges();
                            #endregion

                            #region #InsertBatchMemberNotification

                            var notificationList = memberIdList
                                .Select(memberId => new Entities.MemberNotification
                                {

                                    MemberId = Guid.Parse(memberId),
                                    Id = Guid.NewGuid(),
                                    Message = notification.DescEn,
                                    MessageMm = notification.DescMm,
                                    TitleEn = notification.TitleEn,
                                    TitleMm = notification.TitleMm,
                                    Type = EnumNotificationType.Others.ToString(),
                                    CreatedDate = Utils.GetDefaultDate(),
                                    IsDeleted = false,
                                    IsRead = false,
                                    IsSytemNoti = true,
                                    SystemNotiType = EnumSystemNotiType.Announcement.ToString(),
                                    IsScheduled = false,
                                    IsScheduledDone = true,
                                    JobId = "",
                                    CmsNotificationId = notiId,
                                    ImageUrl = notification.Image,

                                }).ToList();


                            unitOfWork.GetRepository<Entities.MemberNotification>().Add(notificationList);
                            unitOfWork.SaveChanges();

                            
                            Console.WriteLine($"NotificationService => SendNotiFromCms => InsertBatchMemberNotification => notiId => {notiId} => Done!");

                            #endregion



                            #region #SendBatchMessage

                            var memberDeviceList = unitOfWork.GetRepository<Entities.MemberDevice>()
                                .Query(x => memberIdList.Contains(x.MemberId) && !string.IsNullOrEmpty(x.PushToken))
                                .ToList();

                            if (memberDeviceList?.Any() == true)
                            {
                                notification.SendingStatus = EnumNotiStatus.Sending.ToString();

                                var take = 500;
                                var current = 1;
                                var hasNext = true;

                                do
                                {
                                    var skip = (current - 1) * take;

                                    var customDeviceList = memberDeviceList.Skip(skip).Take(take)
                                    .ToList();

                                    if (customDeviceList?.Any() == true)
                                    {

                                        var pushNotification = new NotificationMessage();
                                        pushNotification.IsSytemNoti = true;
                                        pushNotification.NotificationType = EnumNotificationType.Others;
                                        pushNotification.SystemNotiType = EnumSystemNotiType.Announcement;
                                        pushNotification.Message = notification.DescEn;
                                        pushNotification.Title = notification.TitleEn;
                                        pushNotification.ImageUrl = notification.FullImageUrl;
                                        pushNotification.NotificationId = notiId.ToString();
                                        pushNotification.PushTokenList = customDeviceList.Select(x => x.PushToken).ToList();

                                        var batchResponse = SendNotificationSdkMulticastMessage(pushNotification, notiId.ToString(), current);

                                    }
                                    else
                                    {
                                        hasNext = false;
                                    }

                                    current++;

                                } while (hasNext);


                                notification.SendingStatus = EnumNotiStatus.Sent.ToString();

                            }
                            
                            Console.WriteLine($"NotificationService => SendNotiFromCms => SendBatchMessage => notiId => {notiId} => Done!");
                            #endregion



                            

                            #region #CmsNotificationJobLocker
                            notificationJobLocker.Status = "Sent";
                            unitOfWork.SaveChanges();
                            #endregion
                        }
                    }
                    else
                    {
                        Console.WriteLine($"NotificationService => SendNotiFromCms => CmsNotificationJobLocker => notiId => {notiId} => Locked!");
                    }
                    
                }
                    

                    

            }
            catch (Exception ex)
            {

                Console.WriteLine($"NotificationService => SendNotiFromCms => Ex => {ex.Message} {JsonConvert.SerializeObject(ex)}");
            }
        }

    }


    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> OrElse<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
        }
    }
}
