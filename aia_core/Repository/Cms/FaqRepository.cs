using aia_core.Entities;
using aia_core.Model.Cms.Request.Faq;
using aia_core.Model.Cms.Response.Faq;
using aia_core.Model.Mobile.Response.Faq;
using aia_core.Services;
using aia_core.UnitOfWork;
using DocumentFormat.OpenXml.Office2010.Excel;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Repository.Cms
{
    public interface IFaqRepository
    {
        Task<ResponseModel<PagedList<FaqResponse>>> List(ListFaqRequest model);
        Task<ResponseModel<FaqResponse>> Create(CreateFaqRequest model);
        Task<ResponseModel<FaqResponse>> Get(Guid id);
        Task<ResponseModel> Update(UpdateFaqRequest model);
        Task<ResponseModel> Delete(Guid id);
        Task<ResponseModel> ToggleActive([Required] Guid id);

    }
    public class FaqRepository: BaseRepository, IFaqRepository
    {
        public FaqRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage,
            IConfiguration config, IErrorCodeProvider errorCodeProvider, IUnitOfWork<Context> unitOfWork)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {

        }

        public async Task<ResponseModel<FaqResponse>> Create(CreateFaqRequest model)
        {
            try
            {


               

                var faqTopic = new Entities.FaqTopic
                {
                    Id = Guid.NewGuid(),
                    TopicIcon = "",
                    TopicTitleEn = model.TopicTitleEn,
                    TopicTitleMm = model.TopicTitleMm,
                    CreatedBy = GetCmsUser()?.Name,
                    CreatedOn = Utils.GetDefaultDate(),
                    IsActive = true,
                    IsDeleted = false,
                };


                #region #upload-image
                if (model.TopicIcon != null)
                {
                    string imageName = $"{Utils.GetDefaultDate().Ticks}-{model.TopicIcon.FileName}";
                    var uploadResult = azureStorage.UploadAsync(imageName, model.TopicIcon).Result;


                    if (uploadResult.Code == (int)HttpStatusCode.OK)
                    {
                        faqTopic.TopicIcon = imageName;
                    }

                }
                #endregion

                unitOfWork.GetRepository<Entities.FaqTopic>().Add(faqTopic);

                var sort = 1;
                model.FaqQuestions?.ForEach(question =>
                {
                    unitOfWork.GetRepository<Entities.FaqQuestion>().Add(new Entities.FaqQuestion
                    { 
                        Id = Guid.NewGuid(),
                        FaqTopicId = faqTopic.Id,
                        QuestionEn = question.QuestionEn,
                        QuestionMm = question.QuestionMm,
                        AnswerEn = question.AnswerEn,
                        AnswerMm = question.AnswerMm,
                        Sort = sort,
                        IsFeatured = question.IsFeatured,
                    });

                    sort++;
                });
                unitOfWork.SaveChanges();

                #region #Response
                var faqQuestionList = new List<aia_core.Model.Cms.Response.Faq.FaqQuestion>();

                var entityFaqQuestion = unitOfWork.GetRepository<Entities.FaqQuestion>()
                    .Query(x => x.FaqTopicId == faqTopic.Id)
                    .OrderBy(x => x.Sort)
                    .ToList();

                var _sort = 1;
                entityFaqQuestion?.ForEach(question =>
                {
                    faqQuestionList.Add(new aia_core.Model.Cms.Response.Faq.FaqQuestion
                    {
                        FaqTopicId = faqTopic.Id,
                        FaqQuestionId = question.Id,
                        QuestionEn = question.QuestionEn,
                        QuestionMm = question.QuestionMm,
                        AnswerEn = question.AnswerEn,
                        AnswerMm = question.AnswerMm,
                        Sort = _sort,
                        IsFeatured = question.IsFeatured,
                    });

                    _sort++;
                });


                var response = new FaqResponse
                {
                    Id = faqTopic.Id,
                    TopicTitleEn = faqTopic.TopicTitleEn,
                    TopicTitleMm = faqTopic.TopicTitleMm,
                    TopicIconFileUrl = faqTopic.TopicIcon,
                    IsActive = faqTopic.IsActive,
                    IsDeleted = faqTopic.IsDeleted,
                    CreatedBy = faqTopic.CreatedBy,
                    CreatedOn = faqTopic.CreatedOn,
                    FaqQuestions = faqQuestionList,
                };
                #endregion


                #region #Log

                var logFaqTopic = CmsAuditLog(
                        objectGroup: EnumObjectGroup.Faq,
                        objectAction: EnumObjectAction.Create,
                        objectId: faqTopic.Id,
                        objectName: faqTopic.TopicTitleEn,
                        newData: JsonConvert.SerializeObject(faqTopic, Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                            }));


                #endregion



                

                return errorCodeProvider.GetResponseModel<FaqResponse>(ErrorCode.E0, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{httpContext?.HttpContext?.Request.Method} " +
                    $"=> {httpContext?.HttpContext?.Request.Path} " +
                    $"=> Exception " +
                    $"=> {JsonConvert.SerializeObject(ex)}");

                return errorCodeProvider.GetResponseModel<FaqResponse>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel> Delete(Guid id)
        {
            try
            {
                var faqTopic = unitOfWork.GetRepository<Entities.FaqTopic>().Query(x => x.Id == id)
                   .FirstOrDefault();

                if (faqTopic == null) return errorCodeProvider.GetResponseModel(ErrorCode.E400);

                faqTopic.IsDeleted = true;
                faqTopic.UpdatedBy = GetCmsUser().Name;
                faqTopic.UpdatedOn = Utils.GetDefaultDate();
                unitOfWork.SaveChanges();

                var log = CmsAuditLog(
                        objectGroup: EnumObjectGroup.Faq,
                        objectAction: EnumObjectAction.Delete,
                        objectId: faqTopic.Id,
                        objectName: faqTopic.TopicTitleEn,
                        newData: JsonConvert.SerializeObject(faqTopic, Formatting.Indented,
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

        public async Task<ResponseModel<FaqResponse>> Get(Guid id)
        {
            try
            {

                var faqTopic = unitOfWork.GetRepository<Entities.FaqTopic>()
                    .Query(x => x.Id == id)
                    .FirstOrDefault();

                if (faqTopic == null) return errorCodeProvider.GetResponseModel<FaqResponse>(ErrorCode.E400);

                var faqQuestionList = unitOfWork.GetRepository<Entities.FaqQuestion>().
                    Query(x => x.FaqTopicId == id)
                    .OrderBy(x => x.Sort)
                    .ToList();

                var response = new FaqResponse()
                {
                    Id = id,
                    TopicTitleEn = faqTopic.TopicTitleEn,
                    TopicTitleMm = faqTopic.TopicTitleMm,
                    TopicIconFileUrl = GetFileFullUrl(faqTopic.TopicIcon),
                    FaqQuestions = faqQuestionList.Select(x => new Model.Cms.Response.Faq.FaqQuestion
                    { 
                        IsFeatured = x.IsFeatured,
                        Sort = x.Sort,
                        QuestionEn = x.QuestionEn,
                        QuestionMm = x.QuestionMm,
                        AnswerEn = x.AnswerEn,
                        AnswerMm = x.AnswerMm,

                    }).ToList(),
                };


                var log = CmsAuditLog(
                        objectGroup: EnumObjectGroup.Faq,
                        objectAction: EnumObjectAction.View,
                        objectId: faqTopic.Id,
                        objectName: faqTopic.TopicTitleEn,
                        newData: JsonConvert.SerializeObject(faqTopic
                        , Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                            }

                        )
                        
                        );

                return errorCodeProvider.GetResponseModel<FaqResponse>(ErrorCode.E0, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{httpContext?.HttpContext?.Request.Method} " +
                    $"=> {httpContext?.HttpContext?.Request.Path} " +
                    $"=> Exception " +
                    $"=> {JsonConvert.SerializeObject(ex)}");

                return errorCodeProvider.GetResponseModel<FaqResponse>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel<PagedList<FaqResponse>>> List(ListFaqRequest model)
        {
            try
            {
                var query = unitOfWork.GetRepository<Entities.FaqTopic>()
                    .Query(x => x.IsDeleted == false);

                if(!string.IsNullOrEmpty(model.Title))
                {
                    query = query.Where(x => x.TopicTitleEn.Contains(model.Title));
                }

                if(model.IsActive != null)
                {
                    query = query.Where(x => x.IsActive == model.IsActive);
                }

                var count = query.Count();
                var list = query.Skip((model.Page - 1) * model.Size).Take(model.Size)
                    .Include(x => x.FaqQuestions)
                    .Select(x => new FaqResponse
                    { 
                        Id = x.Id,
                        TopicTitleEn = x.TopicTitleEn,
                        TopicTitleMm = x.TopicTitleMm,
                        TopicIconFileUrl     = x.TopicIcon,
                        QuestionCount = x.FaqQuestions.Count,
                        IsActive = x.IsActive,
                        IsDeleted = x.IsDeleted,
                        CreatedBy = x.CreatedBy,
                        CreatedOn = x.CreatedOn,
                        UpdatedBy = x.UpdatedBy,
                        UpdatedOn = x.UpdatedOn,
                    })
                    .OrderByDescending(x => x.CreatedOn)
                    .ToList();

                list?.ForEach(item => 
                {

                    item.UpdatedOn = item.UpdatedOn ?? item.CreatedOn;
                    item.UpdatedBy = item.UpdatedBy ?? item.CreatedBy;
                    item.TopicIconFileUrl = GetFileFullUrl(item.TopicIconFileUrl);
                });

                var data = new PagedList<FaqResponse>(
                    source: list,
                    totalCount: count,
                    pageNumber: model.Page,
                    pageSize: model.Size);

                var log = CmsAuditLog(
                        objectGroup: EnumObjectGroup.Faq,
                objectAction: EnumObjectAction.List,
                        newData : JsonConvert.SerializeObject(list, Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                            })
                     );

                return errorCodeProvider.GetResponseModel<PagedList<FaqResponse>>(ErrorCode.E0, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{httpContext?.HttpContext?.Request.Method} " +
                    $"=> {httpContext?.HttpContext?.Request.Path} " +
                    $"=> Exception " +
                    $"=> {JsonConvert.SerializeObject(ex)}");

                return errorCodeProvider.GetResponseModel<PagedList<FaqResponse>>(ErrorCode.E500);
            }
        }

        public async Task<ResponseModel> ToggleActive(Guid id)
        {
            try
            {
                var faqTopic = unitOfWork.GetRepository<Entities.FaqTopic>().Query(x => x.Id == id)
                    .FirstOrDefault();

                if (faqTopic == null) return errorCodeProvider.GetResponseModel(ErrorCode.E400);

                faqTopic.IsActive = !faqTopic.IsActive;
                faqTopic.UpdatedBy = GetCmsUser().Name;
                faqTopic.UpdatedOn  = Utils.GetDefaultDate();
                unitOfWork.SaveChanges();

                var log = CmsAuditLog(
                        objectGroup: EnumObjectGroup.Faq,
                        objectAction: EnumObjectAction.ToggleStatus,
                        objectId: faqTopic.Id,
                        objectName: faqTopic.TopicTitleEn,
                        newData: JsonConvert.SerializeObject(faqTopic, Formatting.Indented,
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

        public async Task<ResponseModel> Update(UpdateFaqRequest model)
        {
            try
            {
                var faqTopic = unitOfWork.GetRepository<Entities.FaqTopic>().Query(x => x.Id == model.Id)
                    .Include(x => x.FaqQuestions)
                    .FirstOrDefault();

                if (faqTopic == null) return errorCodeProvider.GetResponseModel(ErrorCode.E400);

                var oldData = JsonConvert.SerializeObject(faqTopic, Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                            });

                faqTopic.TopicTitleEn = !string.IsNullOrEmpty(model.TopicTitleEn) ? model.TopicTitleEn : faqTopic.TopicTitleEn;
                faqTopic.TopicTitleMm = !string.IsNullOrEmpty(model.TopicTitleMm) ? model.TopicTitleMm : faqTopic.TopicTitleMm;
                faqTopic.UpdatedBy = GetCmsUser()?.Name;
                faqTopic.UpdatedOn = Utils.GetDefaultDate();

                #region #upload-image
                if (model.TopicIcon != null)
                {
                    string imageName = $"{Utils.GetDefaultDate().Ticks}-{model.TopicIcon.FileName}";
                    var uploadResult = azureStorage.UploadAsync(imageName, model.TopicIcon).Result;


                    if (uploadResult.Code == (int)HttpStatusCode.OK)
                    {
                        faqTopic.TopicIcon = imageName;
                    }

                }
                #endregion
                
                if(model.FaqQuestions?.Any() == true)
                {
                    var oldFaqList = unitOfWork.GetRepository<Entities.FaqQuestion>().Query(x => x.FaqTopicId == model.Id).ToList();
                    unitOfWork.GetRepository<Entities.FaqQuestion>().Delete(oldFaqList);

                    var sort = 1;
                    model.FaqQuestions?.ForEach(question =>
                    {
                        unitOfWork.GetRepository<Entities.FaqQuestion>().Add(new Entities.FaqQuestion
                        {
                            Id = Guid.NewGuid(),
                            FaqTopicId = faqTopic.Id,
                            QuestionEn = question.QuestionEn,
                            QuestionMm = question.QuestionMm,
                            AnswerEn = question.AnswerEn,
                            AnswerMm = question.AnswerMm,
                            Sort = sort,
                            IsFeatured = question.IsFeatured,
                        });

                        sort++;
                    });
                }

                unitOfWork.SaveChanges();

                #region #Log

                var logFaqTopic = CmsAuditLog(
                        objectGroup: EnumObjectGroup.Faq,
                        objectAction: EnumObjectAction.Update,
                        objectId: faqTopic.Id,
                        objectName: faqTopic.TopicTitleEn,
                        oldData: oldData,
                        newData: JsonConvert.SerializeObject(faqTopic, Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                            }));


                #endregion

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
    }
}
