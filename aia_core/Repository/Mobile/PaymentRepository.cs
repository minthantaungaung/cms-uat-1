using aia_core.Entities;
using aia_core.Model.Cms.Response;
using aia_core.Model.Mobile.Request;
using aia_core.Model.Mobile.Request.Payment;
using aia_core.Model.Mobile.Response.Payment;
using aia_core.Services;
using aia_core.UnitOfWork;
using Azure;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Spreadsheet;
using FastMember;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Repository.Mobile
{
    public interface IPaymentRepository
    {     
        Task<ResponseModel<InitiatePaymentUrlResponseModel>> InitiatePaymentUrl(InitiatePaymentUrlRequestModel model);
        ResponseModel<PagedList<GetPaymentHistory>> GetPaymentHistory(int page, int size);
        ResponseModel<GetPaymentHistory> GetPaymentDetails(string paymentId);

        Task<ResponseModel> SendPaymentNoti(SendPaymentNotiRequestModel model);

        ResponseModel<object> TestGenerateClientSideSignature();

        ResponseModel<object> GenerateApiKey(string otp);
    }
    public class PaymentRepository : BaseRepository, IPaymentRepository
    {
        private readonly string  paymentUrl;
        private readonly string upstreamAppKey;
        private readonly string upstreamSecretKey;
        private readonly Dictionary<string, string> ApiKeys;
        private readonly INotificationService notificationService;
        public PaymentRepository(IHttpContextAccessor httpContext
            , IAzureStorageService azureStorage
            , IErrorCodeProvider errorCodeProvider
            , IUnitOfWork<Entities.Context> unitOfWork
            , INotificationService notificationService)
            : base(httpContext
                  , azureStorage
                  , errorCodeProvider
                  , unitOfWork)
        {
            paymentUrl = AppSettingsHelper.GetSetting("AiaPaymentGateway:PaymentUrl");
            upstreamAppKey = AppSettingsHelper.GetSetting("AiaPaymentGateway:UpstreamAppKey");
            upstreamSecretKey = AppSettingsHelper.GetSetting("AiaPaymentGateway:UpstreamSecretKey");

            ApiKeys = new Dictionary<string, string>
            {
                { 
                    AppSettingsHelper.GetSetting("AiaPlusNotiApi:ApiKey"), 
                    AppSettingsHelper.GetSetting("AiaPlusNotiApi:ApiSecret") 
                } 
            };

            this.notificationService = notificationService;
        }

        ResponseModel<GetPaymentHistory> IPaymentRepository.GetPaymentDetails(string paymentId)
        {
            var memberGuid = GetMemberIDFromToken();


            var clientNoList = GetClientNoListByIdValue(memberGuid);

            var policyNoList = unitOfWork.GetRepository<Entities.Policy>()
                .Query(x => clientNoList.Contains(x.PolicyHolderClientNo) || clientNoList.Contains(x.InsuredPersonClientNo))
                .Select(x => x.PolicyNo)
                .ToList();

            var query = unitOfWork.GetRepository<TxnsPayment>()
            .Query(x => policyNoList.Contains(x.PolicyNumber));

            var paymentDetails = unitOfWork.GetRepository<TxnsPayment>()
                .Query(x => x.TransactionID == paymentId)
                .Select(x => new GetPaymentHistory
                {
                    TransactionId = x.TransactionID,
                    TransactionStatus = "Success",
                    TransactionDate = x.TxnsDate,
                    Amount = x.Amount,
                    PaymentChannel = x.PaymentChannel,
                    PolicyNumber = x.PolicyNumber,
                    PaymentType = x.PaymentType,
                })
                .FirstOrDefault();

            if (paymentDetails == null)
                return errorCodeProvider.GetResponseModel<GetPaymentHistory>(ErrorCode.E400);

            var productType = unitOfWork.GetRepository<Policy>()
                    .Query(x => x.PolicyNo == paymentDetails.PolicyNumber)
                    .Select(x => x.ProductType)
                    .FirstOrDefault();

            if (!string.IsNullOrEmpty(productType))
            {
                var product = unitOfWork.GetRepository<Entities.Product>()
                .Query(x => x.ProductTypeShort == productType && x.IsActive == true && x.IsDelete == false)
                .FirstOrDefault();

                paymentDetails.ProductCode = product?.ProductTypeShort;
                paymentDetails.ProductName = product?.TitleEn;
                paymentDetails.ProductNameMM = product?.TitleMm;
                paymentDetails.ProductLogo = GetFileFullUrl(EnumFileType.Product, product.LogoImage);
            }

           

            return errorCodeProvider.GetResponseModel(ErrorCode.E0, paymentDetails);
        }

        ResponseModel<PagedList<GetPaymentHistory>> IPaymentRepository.GetPaymentHistory(int page, int size)
        {
            var memberGuid = GetMemberIDFromToken();

            try
            {

                var clientNoList = GetClientNoListByIdValue(memberGuid);

                var policyNoList = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => clientNoList.Contains(x.PolicyHolderClientNo) || clientNoList.Contains(x.InsuredPersonClientNo))
                    .Select(x => x.PolicyNo)
                    .ToList();

                var query = unitOfWork.GetRepository<TxnsPayment>()
                .Query(x => policyNoList.Contains(x.PolicyNumber));

                var totalCount = query.Count();

                var list = query
                    .OrderByDescending(x => x.TxnsDate)
                    .Skip((page - 1) * size).Take(size)
                    .Select(x => new GetPaymentHistory
                    {
                        TransactionId = x.TransactionID,
                        TransactionStatus = "Success",
                        TransactionDate = x.TxnsDate,
                        Amount = x.Amount,
                        PaymentChannel = x.PaymentChannel,
                        PolicyNumber = x.PolicyNumber,
                        PaymentType = x.PaymentType,
                    })
                    .ToList();

                foreach (var item in list)
                {
                    var productType = unitOfWork.GetRepository<Policy>()
                        .Query(x => x.PolicyNo == item.PolicyNumber)
                        .Select(x => x.ProductType)
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(productType))
                    {
                        var product = unitOfWork.GetRepository<Entities.Product>()
                        .Query(x => x.ProductTypeShort == productType && x.IsActive == true && x.IsDelete == false)
                        .FirstOrDefault();

                        item.ProductCode = product?.ProductTypeShort;
                        item.ProductName = product?.TitleEn;
                        item.ProductNameMM = product?.TitleMm;
                        item.ProductLogo = GetFileFullUrl(EnumFileType.Product, product.LogoImage);
                    }
                }

                var result = new PagedList<GetPaymentHistory>(
                       source: list,
                       totalCount: totalCount,
                       pageNumber: page,
                       pageSize: size);

                return errorCodeProvider.GetResponseModel(ErrorCode.E0, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetPaymentHistory => {ex.Message} {ex.StackTrace}");
                return errorCodeProvider.GetResponseModel<PagedList<GetPaymentHistory>>(ErrorCode.E500);
            }

            
        }

        public async Task<ResponseModel<InitiatePaymentUrlResponseModel>> InitiatePaymentUrl(InitiatePaymentUrlRequestModel model)
        {
            var memberGuid = GetMemberIDFromToken();

            var client = new HttpClient();

            // AIA-12345678901234567890-123456789

            DateTimeOffset offset = new DateTimeOffset(Utils.GetDefaultDate());
            long unixMs = offset.ToUnixTimeMilliseconds();
            var orderId = $"AIA+{model.PolicyNumber}-{unixMs}";
            // Create model with actual values
            var paymentRequest = new PaymentRequest
            {
                amount = Math.Round(model.Amount, 2),
                orderId = orderId,
                paymentType = "Premium Payment",
                policyNumber = model.PolicyNumber,
            };

            // Serialize to JSON
            var json = JsonConvert.SerializeObject(paymentRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Set headers

            var data = $"{paymentRequest.amount}{paymentRequest.orderId}{paymentRequest.paymentType}{paymentRequest.policyNumber}";
            var upstreamAppCheckSum = GenerateHmac(upstreamSecretKey, data);

            client.DefaultRequestHeaders.Add("upstreamAppKey", upstreamAppKey);
            client.DefaultRequestHeaders.Add("upstreamAppCheckSum", upstreamAppCheckSum);

            try
            {
                Console.WriteLine($"InitiatePaymentUrl Request: OrderId => {orderId} {json}");

                var bufferTxns = new BufferTxnsPayment
                {
                    TransactionId = paymentRequest.orderId,
                    PremiumPolicyNo = paymentRequest.policyNumber,
                    Amount = paymentRequest.amount,
                    CreatedAt = Utils.GetDefaultDate(),
                    UserId = memberGuid.Value,
                };

                unitOfWork.GetRepository<BufferTxnsPayment>()
                    .Add(bufferTxns);
                unitOfWork.SaveChanges();

                var response = await client.PostAsync(paymentUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"InitiatePaymentUrl Upstream API Status Code: OrderId => {orderId} {response.StatusCode}");
                Console.WriteLine($"InitiatePaymentUrl Upstream API Response: OrderId => {orderId} {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    bufferTxns.IsGenereatePaymentLinkSuccess = true;
                    unitOfWork.SaveChanges();

                    var paymentResponse = JsonConvert.DeserializeObject<PaymentResponse>(responseContent);
                    if (paymentResponse != null && paymentResponse.code == "200")
                    {
                        return errorCodeProvider.GetResponseModel(ErrorCode.E0, new InitiatePaymentUrlResponseModel
                        {
                            GeneratedPaymentUrl = paymentResponse.datas.paymentUrl,
                            RefPolicyNumber = model.PolicyNumber,
                            RefOrderId = paymentRequest.orderId,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"InitiatePaymentUrl Exception: OrderId => {orderId}  {ex.Message} {ex.StackTrace}");
            }

            return errorCodeProvider.GetResponseModel<InitiatePaymentUrlResponseModel>(ErrorCode.E502);

        }        

        public async Task<ResponseModel> SendPaymentNoti(SendPaymentNotiRequestModel model)
        {
            if (!ApiKeys.ContainsKey(model.ApiKey))
                return errorCodeProvider.GetResponseModel(ErrorCode.E401, "Invalid API Key");

            var secret = ApiKeys[model.ApiKey];

            // Validate timestamp (Prevent replay attacks)
            long requestTime = long.Parse(model.Timestamp);
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Math.Abs(currentTime - requestTime) > 300) // 5 min window
                return errorCodeProvider.GetResponseModel(ErrorCode.E401, "Request Expired");

            // Recompute the expected signature
            string expectedSignature = GenerateHmacSignature(secret, $"POST:/notification:{model.Timestamp}");

            if (model.Signature != expectedSignature)
                return errorCodeProvider.GetResponseModel(ErrorCode.E401, "Invalid Signature");

            #region # Send Noti

            var policyHolderClientNo = unitOfWork.GetRepository<Policy>()
                .Query(x => x.PolicyNo == model.PolicyNo)
                .Select(x => x.PolicyHolderClientNo)
                .FirstOrDefault();

            if (policyHolderClientNo == null)
                return errorCodeProvider.GetResponseModel(ErrorCode.E404, "Invalid Policy No");

            var IdList = unitOfWork.GetRepository<Client>()
                .Query(x => x.ClientNo == policyHolderClientNo)
                .Select(x => new { x.Nrc, x.PassportNo, x.Other })
                .FirstOrDefault();

            if (IdList == null)
                return errorCodeProvider.GetResponseModel(ErrorCode.E404, "Invalid Policy No");


            
            var memberId = unitOfWork.GetRepository<Entities.Member>()
            .Query(x => ((!string.IsNullOrEmpty(IdList.Nrc) && x.Nrc == IdList.Nrc)
            || (!string.IsNullOrEmpty(IdList.PassportNo) && x.Passport == IdList.PassportNo)
            || (!string.IsNullOrEmpty(IdList.Other) && x.Others == IdList.Other))
            && x.IsVerified == true && x.IsActive == true)
            .Select(x => x.MemberId)
                     .FirstOrDefault();
            
            if(memberId == null || memberId == Guid.Empty)
                return errorCodeProvider.GetResponseModel(ErrorCode.E404, "No AIA+ User Found");

            Console.WriteLine($"SendPaymentNoti: memberId: {memberId}");

            var notification = new Entities.MemberNotification()
            {
                IsDeleted = false,
                IsRead = false,
                Id = Guid.NewGuid(),
                Type = EnumNotificationType.Others.ToString(),
                IsSytemNoti = true,
                SystemNotiType = EnumSystemNotiType.Payment.ToString(),
                MemberId = memberId,
                CreatedDate = Utils.GetDefaultDate(),
                Message = model.Message,
                TitleEn = model.Title,
                TitleMm = model.Title,
                PremiumPolicyNo = model.PolicyNo,
                CommonKeyId = model.OrderId,
            };

            unitOfWork.GetRepository<Entities.MemberNotification>().Add(notification);
            unitOfWork.SaveChanges();

            await notificationService.SendNotification(new NotificationMessage
            {
                MemberId = memberId,
                NotificationType = EnumNotificationType.Others,
                IsSytemNoti = true,
                SystemNotiType = EnumSystemNotiType.Announcement,
                Message = model.Message,
                Title = model.Title,
                PolicyNumber = model.PolicyNo,
                CommonKeyId = model.OrderId,
                NotificationId = notification.Id.ToString(),
            });

            #endregion

            return errorCodeProvider.GetResponseModel(ErrorCode.E0);

        }

        ResponseModel<object> IPaymentRepository.TestGenerateClientSideSignature()
        {
            var secret = ApiKeys[AppSettingsHelper.GetSetting("AiaPlusNotiApi:ApiKey")];

            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            string signature = GenerateHmacSignature(secret, $"POST:/notification:{timestamp}");

            var response = new
            {
                timestamp = timestamp,
                signature = signature
            };

            return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, response);
        }

        #region #Funs
        public static string GenerateHmac(string secret, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(secret);
            var messageBytes = Encoding.UTF8.GetBytes(data);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower(); // hex string
            }
        }

        public static string GenerateHmacSignature(string secret, string data)
        {
            using (var hmac = new HMACSHA256(Convert.FromBase64String(secret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hash);
            }
        }

        private static string GetRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        ResponseModel<object> IPaymentRepository.GenerateApiKey(string otp)
        {
            if (ValidateTestEndpointsOtp(otp) == false)
            {
                return errorCodeProvider.GetResponseModel<object>(ErrorCode.E403);
            }

            var apiKeys = new
            {
                ApiKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
                ApiSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                AlertMessage = "Please keep your API Secret safe. It is used to sign the request and should be kept secret.",
            };

            unitOfWork.GetRepository<ApiKeys>()
                .Add(new Entities.ApiKeys
                {
                    ApiKey = apiKeys.ApiKey,
                    ApiSecret = apiKeys.ApiSecret,
                    CreatedAt = Utils.GetDefaultDate(),
                });

            unitOfWork.SaveChanges();

            return errorCodeProvider.GetResponseModel<object>(ErrorCode.E0, apiKeys);
        }
        #endregion
    }


    public class PaymentRequest
    {
        public decimal amount { get; set; }
        public string? orderId { get; set; }
        public string? paymentType { get; set; }
        public string? policyNumber { get; set; }
    }

    public class PaymentResponse
    {
        public string code { get; set; }
        public string message { get; set; }
        public PaymentData datas { get; set; }
    }

    public class PaymentData
    {
        public string paymentUrl { get; set; }
    }

}
