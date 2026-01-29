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
using aia_core.Model.Cms.Request.Crm;
using Microsoft.AspNetCore.Http;
using DocumentFormat.OpenXml.Office2010.Excel;
using Hangfire.Logging;
using Azure;
using DocumentFormat.OpenXml.Spreadsheet;

namespace aia_core.Repository.Cms
{
    public interface ICrmRepository
    {
        Task<ResponseModel<string>> Update(UpdateClaimCrmRequest model);

        void SaveCrmRequest(Guid id, string request);

        void SaveCrmResponse(Guid id, string response);
    }

    public class CrmRepository : BaseRepository, ICrmRepository
    {
        #region "const"
        private readonly IRecurringJobRunner recurringJobRunner;
        private readonly INotificationService notificationService;
         public CrmRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config,
            IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Context> unitOfWork, IRecurringJobRunner recurringJobRunner,INotificationService notificationService)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.recurringJobRunner = recurringJobRunner;
            this.notificationService = notificationService;
        }
        #endregion

        public async Task<ResponseModel<string>> Update(UpdateClaimCrmRequest model)
        {
            

            try
            {

                #region "Signature Validation"
                string signature = httpContext.HttpContext.Request.Headers["Signature"];
                bool isSignatureValid = SignatureValidation(signature, model);
                if (!isSignatureValid)
                {
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E502);
                }
                #endregion


                var crmSystemUser = unitOfWork.GetRepository<Entities.Staff>()
                .Query(x => x.Name == "Crm System User")
                .FirstOrDefault();


                try
                {
                    if(model.channel == "100004")
                    {
                        string _serviceStatus = model.Status;
                        Entities.ServicingStatus servicingStatus = unitOfWork.GetRepository<Entities.ServicingStatus>().Query(x=> x.crm_code == model.Status).FirstOrDefault();
                        if(servicingStatus != null)
                        {
                            if(servicingStatus.short_desc == "RJ")
                            {
                                _serviceStatus = "NotApproved";
                            }
                            else
                            {
                                _serviceStatus = servicingStatus.long_desc;
                            }
                        }
                        else
                        {
                            bool isStatusExist = unitOfWork.GetRepository<Entities.ClaimStatus>().Query(x=> x.ShortDesc == model.Status).Any();
                            if(!isStatusExist)
                            {
                                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E206);
                            }
                        }
                        //Service
                        ServiceMain serviceMain = unitOfWork.GetRepository<ServiceMain>().Query(x=> x.ServiceID == new Guid(model.service_id)).FirstOrDefault();
                        if(serviceMain != null)
                        {
                            serviceMain.ServiceStatus = _serviceStatus;

                            //serviceMain.UpdatedBy = new Guid("6a3e024d-f1f5-4bde-bc8b-5dc1de7b5f5a"); //Crm System User // crmSystemUser?.Id;

                            serviceMain.UpdatedOn = Utils.GetDefaultDate();
                            serviceMain.UpdateChannel = "CRM";

                            unitOfWork.SaveChanges();

                            if(model.case_category == "CC0006")
                            {
                                ServicingRequest serviceChild = unitOfWork.GetRepository<ServicingRequest>().Query(x=> x.ServicingID == serviceMain.ServiceID).FirstOrDefault();
                                serviceChild.Status = _serviceStatus;
                                serviceChild.UpdateChannel = "CRM";
                            }
                            else if(model.case_category == "CC0009")
                            {
                                ServiceBeneficiary serviceChild = unitOfWork.GetRepository<ServiceBeneficiary>().Query(x=> x.ID == serviceMain.ServiceID).FirstOrDefault();
                                serviceChild.Status = _serviceStatus;
                            }
                            else if(model.case_category == "CC012")
                            {
                                ServiceLapseReinstatement serviceChild = unitOfWork.GetRepository<ServiceLapseReinstatement>().Query(x=> x.ID == serviceMain.ServiceID).FirstOrDefault();
                                serviceChild.Status = _serviceStatus;
                            }
                            else if(model.case_category == "CC018")
                            {
                                ServiceHealthRenewal serviceChild = unitOfWork.GetRepository<ServiceHealthRenewal>().Query(x=> x.ID == serviceMain.ServiceID).FirstOrDefault();
                                serviceChild.Status = _serviceStatus;
                            }
                            else if(model.case_category == "CC011" & serviceMain.ServiceType == EnumServiceType.PolicyLoanRepayment.ToString())
                            {
                                ServicePolicyLoanRepayment serviceChild = unitOfWork.GetRepository<ServicePolicyLoanRepayment>().Query(x=> x.ID == serviceMain.ServiceID).FirstOrDefault();
                                serviceChild.Status = _serviceStatus;
                            }
                            else if(model.case_category == "CC011" & serviceMain.ServiceType == EnumServiceType.AcpLoanRepayment.ToString())
                            {
                                ServiceACPLoanRepayment serviceChild = unitOfWork.GetRepository<ServiceACPLoanRepayment>().Query(x=> x.ID == serviceMain.ServiceID).FirstOrDefault();
                                serviceChild.Status = _serviceStatus;
                            }
                            else if(model.case_category == "CC013")
                            {
                                ServiceAdhocTopup serviceChild = unitOfWork.GetRepository<ServiceAdhocTopup>().Query(x=> x.ID == serviceMain.ServiceID).FirstOrDefault();
                                serviceChild.Status = _serviceStatus;
                            }
                            else if(model.case_category == "CC030")
                            {
                                ServicePartialWithdraw serviceChild = unitOfWork.GetRepository<ServicePartialWithdraw>().Query(x=> x.ID == serviceMain.ServiceID).FirstOrDefault();
                                serviceChild.Status = _serviceStatus;
                            }
                            else if(model.case_category == "CC002")
                            {
                                ServicePolicyLoan serviceChild = unitOfWork.GetRepository<ServicePolicyLoan>().Query(x=> x.ID == serviceMain.ServiceID).FirstOrDefault();
                                serviceChild.Status = _serviceStatus;
                            }
                            else if(model.case_category == "CC003")
                            {
                                ServicePolicyPaidUp serviceChild = unitOfWork.GetRepository<ServicePolicyPaidUp>().Query(x=> x.ID == serviceMain.ServiceID).FirstOrDefault();
                                serviceChild.Status = _serviceStatus;
                            }
                            else if(model.case_category == "CC001")
                            {
                                ServicePolicySurrender serviceChild = unitOfWork.GetRepository<ServicePolicySurrender>().Query(x=> x.ID == serviceMain.ServiceID).FirstOrDefault();
                                serviceChild.Status = _serviceStatus;
                            }
                            else if(model.case_category == "CC007")
                            {
                                ServicePaymentFrequency serviceChild = unitOfWork.GetRepository<ServicePaymentFrequency>().Query(x=> x.ID == serviceMain.ServiceID).FirstOrDefault();
                                serviceChild.Status = _serviceStatus;
                            }
                            else if(model.case_category == "CC031")
                            {
                                ServiceSumAssuredChange serviceChild = unitOfWork.GetRepository<ServiceSumAssuredChange>().Query(x=> x.ID == serviceMain.ServiceID).FirstOrDefault();
                                serviceChild.Status = _serviceStatus;
                            }
                            else if(model.case_category == "CC004")
                            {
                                ServiceRefundOfPayment serviceChild = unitOfWork.GetRepository<ServiceRefundOfPayment>().Query(x=> x.ID == serviceMain.ServiceID).FirstOrDefault();
                                serviceChild.Status = _serviceStatus;
                            }
                            unitOfWork.SaveChanges();

                            //var statusChange = unitOfWork.GetRepository<Entities.ServiceStatusUpdate>()
                            //.Query(x => x.ServiceMainID == serviceMain.ServiceID).FirstOrDefault();

                            var statusChange = unitOfWork.GetRepository<Entities.ServiceStatusUpdate>()
                            .Query(x => x.ServiceID == serviceMain.ServiceID).FirstOrDefault();

                            EnumServicingStatus enumStatusValue = (EnumServicingStatus)Enum.Parse(typeof(EnumServicingStatus), statusChange.NewStatus);
                            EnumServiceType enumTypeValue = (EnumServiceType)Enum.Parse(typeof(EnumServiceType), statusChange.ServiceType);

                            notificationService.SendServicingNoti(statusChange.MemberID??new Guid(), serviceMain.ServiceID??new Guid(), enumStatusValue
                                                , enumTypeValue, statusChange.PolicyNumber);
                            statusChange.IsDone = true;
                            unitOfWork.SaveChanges();
                        }
                        else
                        {
                            return errorCodeProvider.GetResponseModel<string>(ErrorCode.E208);
                        }
                    } 
                    else if(model.channel == "100005")
                    {
                        bool isPolicyExist = unitOfWork.GetRepository<Policy>().Query(x=> x.PolicyNo == model.Policy_number).Any();
                        if(!isPolicyExist)
                        {
                            return errorCodeProvider.GetResponseModel<string>(ErrorCode.E205);
                        }

                        string _claimStatus = model.Status;
                        Entities.ClaimStatus claimStatus = unitOfWork.GetRepository<Entities.ClaimStatus>().Query(x=> x.CrmCode == model.Status).FirstOrDefault();
                        if(claimStatus != null)
                        {
                            _claimStatus = claimStatus.ShortDesc;
                        }
                        else
                        {
                            bool isStatusExist = unitOfWork.GetRepository<Entities.ClaimStatus>().Query(x=> x.ShortDesc == model.Status).Any();
                            if(!isStatusExist)
                            {
                                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E206);
                            }
                        }

                        Claim claim = unitOfWork.GetRepository<Claim>().Query(x=> x.ClaimId == model.Claim_id).FirstOrDefault();
                        if(claim != null)
                        {
                            ClaimsStatusUpdate claimStatusUpdate = new ClaimsStatusUpdate();
                            claimStatusUpdate.Id = Guid.NewGuid();
                            claimStatusUpdate.ClaimId = claim.ClaimId;
                            claimStatusUpdate.OldStatus = claim.Status;
                            claimStatusUpdate.OldStatus = _claimStatus;
                            claimStatusUpdate.NewStatus = _claimStatus;
                            claimStatusUpdate.CreatedDate = Utils.GetDefaultDate();
                            claimStatusUpdate.IsDone = false;
                            claimStatusUpdate.IsDeleted = false;
                            claimStatusUpdate.ChangedByAiaPlus = true;
                            claimStatusUpdate.NewStatusDesc = "Paid";
                            claimStatusUpdate.NewStatusDescMm = "Paid";
                            
                            unitOfWork.GetRepository<ClaimsStatusUpdate>().Add(claimStatusUpdate);


                            //TLS
                            var claimTran = unitOfWork.GetRepository<ClaimTran>()
                                .Query(x => x.ClaimId == Guid.Parse(model.Claim_id))
                                .FirstOrDefault();

                            if(claimTran != null)
                            {
                                

                                claimTran.ClaimStatusCode = "PD";
                                claimTran.ClaimStatus = "Paid";

                                claimTran.UpdatedBy = "CRM"; //crmSystemUser?.Id.ToString();
                                claimTran.UpdatedOn = Utils.GetDefaultDate();
                            }
                            //TLS

                            unitOfWork.SaveChanges();


                            // claim.ClaimId = model.Claim_id;
                            // claim.ClaimIdIl = model.Claim_id_IL;
                            // claim.PolicyNo = model.Policy_number;
                            // claim.ProductType = model.Product_type;
                            // claim.ClaimType = model.Claim_type;
                            // claim.Status = _claimStatus;
                            // claim.ClaimentClientNo = model.Claiment_client_no;
                            // claim.ReceivedDate = model.Received_date;
                            // claim.RejectReason = model.Reject_reason;
                            // claim.UpdatedDate = Utils.GetDefaultDate();
                            
                            // if(model.Bank_info!=null)
                            // {
                            //     claim.BankName = model.Bank_info.Bank_code;
                            //     claim.AccountNo = model.Bank_info.Bank_account_number;
                            // }

                            // unitOfWork.SaveChanges();
                        }
                        else
                        {
                            return errorCodeProvider.GetResponseModel<string>(ErrorCode.E207);
                            // Claim data = new Claim();
                            // data.ClaimId = model.Claim_id;
                            // data.ClaimIdIl = model.Claim_id_IL;
                            // data.PolicyNo = model.Policy_number;
                            // data.ProductType = model.Product_type;
                            // data.ClaimType = model.Claim_type;
                            // data.Status = _claimStatus;
                            // data.ClaimentClientNo = model.Claiment_client_no;
                            // data.ReceivedDate = model.Received_date;
                            // data.RejectReason = model.Reject_reason;
                            // data.CreatedDate = Utils.GetDefaultDate();
                            // data.UpdatedDate = Utils.GetDefaultDate();

                            // // if(model.Bank_info!=null)
                            // // {
                            // //     data.BankName = model.Bank_info.Bank_code;
                            // //     data.AccountNo = model.Bank_info.Bank_account_number;
                            // // }

                            // unitOfWork.GetRepository<Claim>().Add(data);
                            // unitOfWork.SaveChanges();
                        }
                    }

                    // ClaimsStatusUpdate claimStatusUpdate = unitOfWork.GetRepository<ClaimsStatusUpdate>().Query(x=> x.ClaimId == model.Claim_id)
                    // .OrderByDescending(o=> o.CreatedDate).FirstOrDefault();

                    // claimStatusUpdate.PayableAmountFromIL = model.Claim_Amount;
                    // unitOfWork.SaveChanges();
                }
                catch (System.Exception ex)
                {
                    CmsErrorLog("Update CRM API Claim Table Error", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                    Console.WriteLine($"Error : {ex.InnerException}");
                    return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
                }
               

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0); 
            }
            catch (System.Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
            
        }

        private bool SignatureValidation(string requestSignature, UpdateClaimCrmRequest model)
        {
            string signature = "";
            if(!String.IsNullOrEmpty(model.Claim_id))
            {
                signature = Utils.ComputeSha256Hash(model.Generate_id + "|" + model.Request_date + "|" + model.Claim_id + "|aiaplus");
            }
            else if(!String.IsNullOrEmpty(model.service_id))
            {
                signature = Utils.ComputeSha256Hash(model.Generate_id + "|" + model.Request_date + "|" + model.service_id + "|aiaplus");
            }

            if(signature == requestSignature)
            {
                bool isExist = unitOfWork.GetRepository<CrmSignature>().Query(x=> x.SignatureValue == requestSignature).Any();
                if(isExist)
                {
                    return false;
                }
                else
                {
                    CrmSignature data = new CrmSignature();
                    data.ID = Guid.NewGuid();
                    data.SignatureValue = requestSignature;
                    data.RequestBody = System.Text.Json.JsonSerializer.Serialize(model);
                    data.CreatedDate = Utils.GetDefaultDate();
                    unitOfWork.GetRepository<CrmSignature>().Add(data);
                    unitOfWork.SaveChanges();

                    return true;
                }
            }
            return signature == requestSignature;
        }

        private void SaveLog(UpdateClaimCrmRequest model)
        {
            CrmApiLog data = new CrmApiLog();
            data.ID = Guid.NewGuid();
            data.RequestBody = System.Text.Json.JsonSerializer.Serialize(model);
            data.CreatedDate = Utils.GetDefaultDate();
            unitOfWork.GetRepository<CrmApiLog>().Add(data);
            unitOfWork.SaveChanges();
        }

        void ICrmRepository.SaveCrmRequest(Guid id, string request)
        {
            try
            {
                CrmApiLog data = new CrmApiLog();
                data.ID = id;
                data.RequestBody = request;
                data.CreatedDate = Utils.GetDefaultDate();
                unitOfWork.GetRepository<CrmApiLog>().Add(data);
                unitOfWork.SaveChanges();
            }
            catch { }
        }

        void ICrmRepository.SaveCrmResponse(Guid id, string response)
        {
            try
            {

                var entity = unitOfWork.GetRepository<CrmApiLog>().Query(x => x.ID == id).FirstOrDefault();

                if (entity != null)
                {
                    entity.ResponseBody = response;
                    unitOfWork.SaveChanges();
                }
                
            }
            catch { }
        }
    }
}