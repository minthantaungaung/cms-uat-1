using aia_core.Model.Cms.Response;
using aia_core.Model.Mobile.Request;
using aia_core.Model.Mobile.Response;
using aia_core.Model.Mobile.Response.DocConfig;
using aia_core.Model.Mobile.Servicing.Data.Response;
using aia_core.Services;
using aia_core.UnitOfWork;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Repository.Mobile
{
    public interface IDocConfigRepository
    {
        ResponseModel<List<AiaCmsDocResponse>> GetDocumentList(string policyNumber, int page, int size);
        ResponseModel<AiaCmsDocBase64Response> DownloadBase64(string documentId, string policyNumber);

        ResponseModel<AiaCmsGetDocListResponse> GetDocumentListWithPaging(string policyNumber, int page, int size);
    }

    public class DocConfigRepository : BaseRepository, IDocConfigRepository
    {
        private readonly IAiaCmsApiService cmsApiService;

        public DocConfigRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork,
            IAiaCmsApiService cmsApiService)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
            this.cmsApiService = cmsApiService;
        }

        ResponseModel<AiaCmsDocBase64Response> IDocConfigRepository.DownloadBase64(string documentId, string policyNumber)
        {
            try
            {

                var memberId = GetMemberIDFromToken();
                var clientNoList = GetClientNoListByIdValue(memberId);

                var policyInfo = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == policyNumber)
                    .Select(x => new { x.PolicyHolderClientNo, x.InsuredPersonClientNo })
                    .FirstOrDefault();

                if (policyInfo != null && clientNoList != null)
                {
                    if (clientNoList.Contains(policyInfo.PolicyHolderClientNo) == false && clientNoList.Contains(policyInfo.InsuredPersonClientNo) == false)
                    {
                        return errorCodeProvider.GetResponseModel<AiaCmsDocBase64Response>(ErrorCode.E403);
                    }
                }

                

                var response = cmsApiService.DownloadBase64(documentId).Result;
                if(response?.data != null)
                {
                    return errorCodeProvider.GetResponseModel<AiaCmsDocBase64Response>(ErrorCode.E0,
                        new AiaCmsDocBase64Response { base64 = response.data.base64 }
                        );
                }

                return errorCodeProvider.GetResponseModel<AiaCmsDocBase64Response>(ErrorCode.E400);
            }
            catch (Exception ex)
            {
                return errorCodeProvider.GetResponseModel<AiaCmsDocBase64Response>(ErrorCode.E500);
            }
        }

        ResponseModel<List<AiaCmsDocResponse>> IDocConfigRepository.GetDocumentList(string policyNumber, int page, int size)
        {
            try
            {
                var memberId = GetMemberIDFromToken();
                var clientNoList = GetClientNoListByIdValue(memberId);

                var policyInfo = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == policyNumber)
                    .Select(x => new { x.PolicyHolderClientNo, x.InsuredPersonClientNo})
                    .FirstOrDefault();

                if (policyInfo != null && clientNoList != null)
                {
                    if (clientNoList.Contains(policyInfo.PolicyHolderClientNo) == false && clientNoList.Contains(policyInfo.InsuredPersonClientNo) == false)
                    {
                        return errorCodeProvider.GetResponseModel<List<AiaCmsDocResponse>>(ErrorCode.E403);
                    }
                }
                

                var list = new List<AiaCmsDocResponse>();

                policyNumber = policyNumber.Substring(0, 10);

                var model = new GetDocumentListRequest { PolicyNo = new string[] { policyNumber }, pageNum = page, pageSize = size };

                var docList = cmsApiService.GetDocumentList(model).Result;

                Console.WriteLine($"Aia Cms GetDocumentList => Request {JsonConvert.SerializeObject(model)} Reponse => {JsonConvert.SerializeObject(docList)}");

                if(docList?.data?.data != null)
                {
                    foreach (var doc in docList.data.data)
                    {
                        var createdDate = DateTime.ParseExact(doc.createDate, "yyyy-MM-dd HH:mm:ss", null);

                        var item = new AiaCmsDocResponse
                        {
                            FileName = doc.fileShowName,
                            DocTypeId = doc.doctypeId,
                            Format = doc.format,
                            CreatedDate = createdDate,
                            DocumentId = doc.documentRid,
                        };

                        list.Add(item);
                    }
                }

                var responses = new List<AiaCmsDocResponse>();               



                if(list != null)
                {
                    var docConfigList = unitOfWork.GetRepository<Entities.DocConfig>().Query().ToList();

                    docConfigList?.ForEach(docConfig =>
                    {
                        var matchedList = list.Where(x => x.DocTypeId == docConfig.DocTypeId).OrderByDescending(x => x.CreatedDate).ToList();

                                         
                        if (docConfig.ShowingFor == EnumDocShowingFor.All.ToString())
                        {
                            matchedList?.ForEach(matched =>
                            {
                                var item = new AiaCmsDocResponse
                                {
                                    FileName = docConfig.DocName,
                                    DocTypeId = matched.DocTypeId,
                                    Format = matched.Format,
                                    CreatedDate = matched.CreatedDate,
                                    DocumentId = matched.DocumentId,
                                    DocTypeName = docConfig.DocType,
                                };

                                responses.Add(item);
                            });
                        }
                        else if (docConfig.ShowingFor == EnumDocShowingFor.Latest.ToString())
                        {
                            var latest = matchedList?.FirstOrDefault();

                            if (latest != null)
                            {
                                var item = new AiaCmsDocResponse
                                {
                                    FileName = docConfig.DocName,
                                    DocTypeId = latest.DocTypeId,
                                    Format = latest.Format,
                                    CreatedDate = latest.CreatedDate,
                                    DocumentId = latest.DocumentId,
                                    DocTypeName = docConfig.DocType,
                                };

                                responses.Add(item);
                            }
                        }
                    });
                }

                Console.WriteLine($"Aia Cms GetDocumentList => FinalResponse => {responses.Count} {JsonConvert.SerializeObject(responses)}");
                return errorCodeProvider.GetResponseModel<List<AiaCmsDocResponse>>(ErrorCode.E0, responses);
            }
            catch (Exception ex)
            {
                MobileErrorLog("GetDocList => Ex", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<List<AiaCmsDocResponse>>(ErrorCode.E500);
            }
        }

        ResponseModel<AiaCmsGetDocListResponse> IDocConfigRepository.GetDocumentListWithPaging(string policyNumber, int page, int size)
        {
            try
            {
                var memberId = GetMemberIDFromToken();
                var clientNoList = GetClientNoListByIdValue(memberId);

                var policyInfo = unitOfWork.GetRepository<Entities.Policy>()
                    .Query(x => x.PolicyNo == policyNumber)
                    .Select(x => new { x.PolicyHolderClientNo, x.InsuredPersonClientNo })
                    .FirstOrDefault();

                if (policyInfo != null && clientNoList != null)
                {
                    if (clientNoList.Contains(policyInfo.PolicyHolderClientNo) == false && clientNoList.Contains(policyInfo.InsuredPersonClientNo) == false)
                    {
                        return errorCodeProvider.GetResponseModel<AiaCmsGetDocListResponse>(ErrorCode.E403);
                    }
                }

                var cmsGetDocListResponse = new AiaCmsGetDocListResponse();

                var list = new List<AiaCmsDocResponse>();

                policyNumber = policyNumber.Substring(0, 10);

                var docTypeIdArray = unitOfWork.GetRepository<Entities.DocConfig>().Query().Select(x => x.DocTypeId).ToArray();
                var model = new GetDocumentListRequest { PolicyNo = new string[] { policyNumber }, pageNum = page, pageSize = size, docTypeId = docTypeIdArray };

                Console.WriteLine($"Aia Cms GetDocumentList " +
                                    $"=> PolicyNo => {policyNumber} " +
                                    $"=> PageNo => {page}" +
                                    $"=> RequestDatetime {Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)} " +
                                    $"=> RequestJson {JsonConvert.SerializeObject(model)}");

                var docList = cmsApiService.GetDocumentList(model).Result;

                Console.WriteLine($"Aia Cms GetDocumentList " +
                                    $"=> PolicyNo => {policyNumber} " +
                                    $"=> PageNo => {page}" +
                                    $"=> ResponseDatetime {Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)} " +
                                    $"=> ResponseJson {JsonConvert.SerializeObject(docList)}");

                if (docList?.data?.data != null)
                {
                    foreach (var doc in docList.data.data)
                    {
                        var createdDate = DateTime.ParseExact(doc.createDate, "yyyy-MM-dd HH:mm:ss", null);

                        var item = new AiaCmsDocResponse
                        {
                            FileName = doc.fileShowName,
                            DocTypeId = doc.doctypeId,
                            Format = doc.format,
                            CreatedDate = createdDate,
                            DocumentId = doc.documentRid,
                        };

                        list.Add(item);
                    }
                }

                var responses = new List<AiaCmsDocResponse>();



                if (list != null)
                {
                    var docConfigList = unitOfWork.GetRepository<Entities.DocConfig>().Query().ToList();

                    docConfigList?.ForEach(docConfig =>
                    {
                        var matchedList = list.Where(x => x.DocTypeId == docConfig.DocTypeId).OrderByDescending(x => x.CreatedDate).ToList();


                        if (docConfig.ShowingFor == EnumDocShowingFor.All.ToString())
                        {
                            matchedList?.ForEach(matched =>
                            {
                                var item = new AiaCmsDocResponse
                                {
                                    FileName = docConfig.DocName,
                                    DocTypeId = matched.DocTypeId,
                                    Format = matched.Format,
                                    CreatedDate = matched.CreatedDate,
                                    DocumentId = matched.DocumentId,
                                    DocTypeName = docConfig.DocType,
                                };

                                responses.Add(item);
                            });
                        }
                        else if (docConfig.ShowingFor == EnumDocShowingFor.Latest.ToString())
                        {
                            var latest = matchedList?.FirstOrDefault();

                            if (latest != null)
                            {
                                var item = new AiaCmsDocResponse
                                {
                                    FileName = docConfig.DocName,
                                    DocTypeId = latest.DocTypeId,
                                    Format = latest.Format,
                                    CreatedDate = latest.CreatedDate,
                                    DocumentId = latest.DocumentId,
                                    DocTypeName = docConfig.DocType,
                                };

                                responses.Add(item);
                            }
                        }
                    });
                }

                cmsGetDocListResponse.rawDocTotalCount = docList?.data?.total ?? 0;
                cmsGetDocListResponse.rawDocPageCount = docList?.data?.totalPage ?? 0;
                cmsGetDocListResponse.rawDocCurrentPage = page;
                cmsGetDocListResponse.rawDocHasNextPage = page < cmsGetDocListResponse.rawDocPageCount ? true : false;

                cmsGetDocListResponse.filterDocList = responses;
                cmsGetDocListResponse.filterDocCount = responses.Count;

                

                Console.WriteLine($"Aia Cms GetDocumentList " +
                                    $"=> PolicyNo => {policyNumber} " +
                                    $"=> PageNo => {page}" +
                                    $"=> FinalResponseDatetime {Utils.GetDefaultDate().ToString(DefaultConstants.DateTimeFormat)} " +
                                    $"=> FinalResponseJson {JsonConvert.SerializeObject(cmsGetDocListResponse)}");

                return errorCodeProvider.GetResponseModel(ErrorCode.E0, cmsGetDocListResponse);
            }
            catch (Exception ex)
            {
                MobileErrorLog("GetDocList => Ex", ex.Message, JsonConvert.SerializeObject(ex), httpContext?.HttpContext.Request.Path);
                return errorCodeProvider.GetResponseModel<AiaCmsGetDocListResponse>(ErrorCode.E500);
            }
        }
    }
}
