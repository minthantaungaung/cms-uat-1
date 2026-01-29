using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Request.PaymentChangeConfig;
using aia_core.Model.Cms.Response;
using aia_core.Model.Cms.Response.PaymentChangeConfig;
using aia_core.RecurringJobs;
using aia_core.Services;
using aia_core.UnitOfWork;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Google.Apis.Discovery;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Repository.Cms
{
    public interface IPaymentChangeConfigRepository
    {
        ResponseModel<string> Create(PaymentChangeConfigRequest model);
        ResponseModel<string> Update(PaymentChangeConfigUpdateRequest model);
        ResponseModel<List<PaymentChangeConfigResponse>> List();
        ResponseModel<PaymentChangeConfigResponse> Get(Guid? id);
    }

    public class PaymentChangeConfigRepository : BaseRepository, IPaymentChangeConfigRepository
    {
        public PaymentChangeConfigRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config, IErrorCodeProvider errorCodeProvider, IUnitOfWork<Context> unitOfWork)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            
        }

        ResponseModel<string> IPaymentChangeConfigRepository.Create(PaymentChangeConfigRequest model)
        {
            try
            {
                var isExistCode = unitOfWork.GetRepository<Entities.PaymentChangeConfig>().Query(x => x.Code == model.Code).Any();
                

                if (isExistCode) return new ResponseModel<string> { Code = 400, Message = "Code is already used." };

                var data = new Entities.PaymentChangeConfig
                {
                    Value = model.Value,
                    DescEn = model.DescEn,
                    DescMm = model.DescMm,
                    Code = model.Code,
                    Id = Guid.NewGuid(),
                    Status = true,
                    CreatedOn = Utils.GetDefaultDate(),
                    Type = model.Type.ToString(),

                };

                unitOfWork.GetRepository<Entities.PaymentChangeConfig>().Add(data);
                unitOfWork.SaveChanges();                   


                CmsAuditLog(
                        objectGroup: EnumObjectGroup.PaymentChangeConfig,
                        objectAction: EnumObjectAction.Create);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0, "success");
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
        }

        ResponseModel<PaymentChangeConfigResponse> IPaymentChangeConfigRepository.Get(Guid? id)
        {
            try
            {


                var data = unitOfWork.GetRepository<Entities.PaymentChangeConfig>()
                    .Query(x => x.Id == id)
                    .Select(x => new PaymentChangeConfigResponse
                    {
                        Id = x.Id,
                        Value = x.Value,
                        DescEn = x.DescEn,
                        DescMm = x.DescMm,
                        Code = x.Code,
                        Status = x.Status,
                        Type = x.Type
                    }
                    )
                    .FirstOrDefault();


                CmsAuditLog(
                        objectGroup: EnumObjectGroup.PaymentChangeConfig,
                        objectAction: EnumObjectAction.View);

                return errorCodeProvider.GetResponseModel<PaymentChangeConfigResponse>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PaymentChangeConfigResponse>(ErrorCode.E400);
            }
        }

        ResponseModel<List<PaymentChangeConfigResponse>> IPaymentChangeConfigRepository.List()
        {
            try
            {


                var list = unitOfWork.GetRepository<Entities.PaymentChangeConfig>()
                    .Query()
                    .OrderByDescending(x => x.CreatedOn)
                    .Select(x => new PaymentChangeConfigResponse
                    { 
                        Id = x.Id,
                        Value = x.Value,
                        DescEn = x.DescEn,
                        DescMm = x.DescMm,
                        Code = x.Code,
                        Status = x.Status,
                        Type = x.Type
                    }
                    )
                    .ToList();
                

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.PaymentChangeConfig,
                        objectAction: EnumObjectAction.View);

                return errorCodeProvider.GetResponseModel<List<PaymentChangeConfigResponse>>(ErrorCode.E0, list);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<PaymentChangeConfigResponse>>(ErrorCode.E400);
            }
        }

        ResponseModel<string> IPaymentChangeConfigRepository.Update(PaymentChangeConfigUpdateRequest model)
        {
            try
            {
                var data = unitOfWork.GetRepository<Entities.PaymentChangeConfig>().Query(x => x.Id == model.Id).FirstOrDefault();
                if (data == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                var isExistCode = unitOfWork.GetRepository<Entities.PaymentChangeConfig>().Query(x => x.Id != model.Id && x.Code == model.Code).Any();
                if (isExistCode) return new ResponseModel<string> { Code = 400, Message = "Code is already used." };



                data.Code = model.Code ?? data.Code;
                data.DescEn = model.DescEn ?? data.DescEn;
                data.DescMm = model.DescMm ?? data.DescMm;
                data.Value = model.Value ?? data.Value;
                data.UpdatedOn = Utils.GetDefaultDate();
                data.Type = model.Type.ToString() ?? data.Type;
                data.Status = model.Status ?? data.Status;
                
                unitOfWork.SaveChanges();


                CmsAuditLog(
                        objectGroup: EnumObjectGroup.PaymentChangeConfig,
                        objectAction: EnumObjectAction.Update);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0, "success");
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
        }
    }
}
