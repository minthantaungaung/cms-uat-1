using aia_core.Entities;
using aia_core.Model.Cms.Request;
using aia_core.Model.Cms.Request.DocConfig;
using aia_core.Model.Cms.Request.PaymentChangeConfig;
using aia_core.Model.Cms.Response;
using aia_core.Model.Cms.Response.DocConfig;
using aia_core.Model.Cms.Response.PaymentChangeConfig;
using aia_core.RecurringJobs;
using aia_core.Services;
using aia_core.UnitOfWork;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Google.Apis.Discovery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Repository.Cms
{
    public interface IDocConfigRepository
    {
        ResponseModel<string> Create(DocConfigRequest model);
        ResponseModel<string> Update(DocConfigUpdateRequest model);
        ResponseModel<List<DocConfigResponse>> List(string docType, string docTypeId);
        ResponseModel<DocConfigResponse> Get(Guid? id);
        ResponseModel<string> Delete(Guid? id);


        ResponseModel<PagedList<DocConfigResponse>> List(string docType, string docTypeId, int page, int size);
    }

    public class DocConfigRepository : BaseRepository, IDocConfigRepository
    {
        public DocConfigRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config, IErrorCodeProvider errorCodeProvider, IUnitOfWork<Context> unitOfWork)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            
        }

        public ResponseModel<string> Create(DocConfigRequest model)
        {
            try
            {

                var data = new Entities.DocConfig
                {
                    DocType = model.DocType,
                    DocTypeId = model.DocTypeId,
                    DocName = model.DocName,
                    ShowingFor = model.ShowingFor.ToString(),
                    Id = Guid.NewGuid(),
                    CreatedOn = Utils.GetDefaultDate(),

                };

                unitOfWork.GetRepository<Entities.DocConfig>().Add(data);
                unitOfWork.SaveChanges();                   


                CmsAuditLog(
                        objectGroup: EnumObjectGroup.DocConfig,
                        objectAction: EnumObjectAction.Create);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0, "success");
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
        }

        public ResponseModel<DocConfigResponse> Get(Guid? id)
        {
            try
            {


                var data = unitOfWork.GetRepository<Entities.DocConfig>()
                    .Query(x => x.Id == id)
                    .Select(x => new DocConfigResponse
                    {
                        Id = x.Id,
                        DocType = x.DocType,
                        DocTypeId = x.DocTypeId,
                        DocName = x.DocName,
                        ShowingFor = x.ShowingFor,
                        CreatedOn = x.CreatedOn,
                        UpdatedOn = x.UpdatedOn,
                    }
                    )
                    .FirstOrDefault();


                CmsAuditLog(
                        objectGroup: EnumObjectGroup.DocConfig,
                        objectAction: EnumObjectAction.View);

                return errorCodeProvider.GetResponseModel<DocConfigResponse>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<DocConfigResponse>(ErrorCode.E400);
            }
        }

        public ResponseModel<List<DocConfigResponse>> List(string docType, string docTypeId)
        {
            try
            {

                var query = unitOfWork.GetRepository<Entities.DocConfig>().Query();

                if(!string.IsNullOrEmpty(docType) )
                {
                    query = query.Where(x => x.DocType.Contains(docType));
                }

                if(!string.IsNullOrEmpty (docTypeId) )
                {
                    query = query.Where(x => x.DocTypeId.Contains(docTypeId));
                }

                var list = query
                    .OrderByDescending(x => x.CreatedOn)
                    .Select(x => new DocConfigResponse
                    { 
                        Id = x.Id,
                        DocType = x.DocType,
                        DocTypeId = x.DocTypeId,
                        DocName = x.DocName,
                        ShowingFor = (x.ShowingFor == EnumDocShowingFor.All.ToString()) ? "All documents" : "Latest document",
                        CreatedOn = x.CreatedOn,
                        UpdatedOn = x.UpdatedOn,
                    }
                    )
                    .ToList();
                

                CmsAuditLog(
                        objectGroup: EnumObjectGroup.DocConfig,
                        objectAction: EnumObjectAction.View);

                return errorCodeProvider.GetResponseModel<List<DocConfigResponse>>(ErrorCode.E0, list);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<List<DocConfigResponse>>(ErrorCode.E400);
            }
        }

        public ResponseModel<string> Update(DocConfigUpdateRequest model)
        {
            try
            {
                var data = unitOfWork.GetRepository<Entities.DocConfig>().Query(x => x.Id == model.Id).FirstOrDefault();
                if (data == null) return errorCodeProvider.GetResponseModel<string>(ErrorCode.E400);

                
                data.DocType = model.DocType ?? data.DocType;
                data.DocTypeId = model.DocTypeId ?? data.DocTypeId;
                data.DocName = model.DocName ?? data.DocName;
                data.ShowingFor = model.ShowingFor.ToString() ?? data.ShowingFor;
                data.UpdatedOn = Utils.GetDefaultDate();
                
                unitOfWork.SaveChanges();


                CmsAuditLog(
                        objectGroup: EnumObjectGroup.DocConfig,
                        objectAction: EnumObjectAction.Update);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0, "success");
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
        }

        ResponseModel<string> IDocConfigRepository.Delete(Guid? id)
        {
            try
            {
                var entity = unitOfWork.GetRepository<Entities.DocConfig>().Query(x => x.Id == id).FirstOrDefault();
                unitOfWork.GetRepository<Entities.DocConfig>().Delete(entity);
                unitOfWork.SaveChanges();


                CmsAuditLog(
                        objectGroup: EnumObjectGroup.DocConfig,
                        objectAction: EnumObjectAction.Delete);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E0, "success");
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<string>(ErrorCode.E500);
            }
        }

        ResponseModel<PagedList<DocConfigResponse>> IDocConfigRepository.List(string docType, string docTypeId, int page, int size)
        {
            try
            {

                var query = unitOfWork.GetRepository<Entities.DocConfig>().Query();

                if (!string.IsNullOrEmpty(docType))
                {
                    query = query.Where(x => x.DocType.Contains(docType));
                }

                if (!string.IsNullOrEmpty(docTypeId))
                {
                    query = query.Where(x => x.DocTypeId.Contains(docTypeId));
                }


                var list = query
                    .OrderByDescending(x => x.CreatedOn)
                    .Select(x => new DocConfigResponse
                    {
                        Id = x.Id,
                        DocType = x.DocType,
                        DocTypeId = x.DocTypeId,
                        DocName = x.DocName,
                        ShowingFor = (x.ShowingFor == EnumDocShowingFor.All.ToString()) ? "All documents" : "Latest document",
                        CreatedOn = x.CreatedOn,
                        UpdatedOn = x.UpdatedOn,
                    }
                    )
                    .Skip((page - 1) * size).Take(size)
                    .ToList();

                var totalCount = query.Count();

                var data = new PagedList<DocConfigResponse>(
                    source: list,
                    totalCount: totalCount,
                    pageNumber: page,
                    pageSize: size);


                CmsAuditLog(
                        objectGroup: EnumObjectGroup.DocConfig,
                        objectAction: EnumObjectAction.View);

                return errorCodeProvider.GetResponseModel<PagedList<DocConfigResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                CmsErrorLog(null, ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);

                return errorCodeProvider.GetResponseModel<PagedList<DocConfigResponse>>(ErrorCode.E500);
            }
        }
    }
}
