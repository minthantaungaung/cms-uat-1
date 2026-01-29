using aia_core.Model.Mobile.Response.Faq;
using aia_core.Services;
using aia_core.UnitOfWork;
using Azure;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Repository.Mobile
{
    public interface IFaqRepository
    {
        ResponseModel<FaqListResponse> GetList();
        ResponseModel<List<FaqQuestion>> GetListByTopicId(Guid? topicId, string? question);
        ResponseModel<FaqListResponse> GetFaqSearch(string? question);
    }
    public class FaqRepository : BaseRepository, IFaqRepository
    {
        

        public FaqRepository(IHttpContextAccessor httpContext, IAzureStorageService azureStorage, IErrorCodeProvider errorCodeProvider,
            IUnitOfWork<Entities.Context> unitOfWork)
            : base(httpContext, azureStorage, errorCodeProvider, unitOfWork)
        {
        }

        ResponseModel<FaqListResponse> IFaqRepository.GetFaqSearch(string? question)
        {
            try
            {


                var faqList = unitOfWork.GetRepository<Entities.FaqQuestion>()
                    .Query(x => x.QuestionEn.Contains(question) || x.QuestionMm.Contains(question))
                    .OrderBy(x => x.Sort)
                    .Select(x => new FaqQuestion
                    {
                        QuestionEn = x.QuestionEn,
                        QuestionMm = x.QuestionMm,
                        AnswerEn = x.AnswerEn,
                        AnswerMm = x.AnswerMm,
                    })
                    .ToList();

                return errorCodeProvider.GetResponseModel(ErrorCode.E0, new FaqListResponse { FaqPopularQuestionList = faqList }) ;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{httpContext?.HttpContext?.Request.Method} " +
                    $"=> {httpContext?.HttpContext?.Request.Path} " +
                    $"=> Exception " +
                    $"=> {JsonConvert.SerializeObject(ex)}");

                return errorCodeProvider.GetResponseModel<FaqListResponse>(ErrorCode.E500);
            }
        }

        ResponseModel<FaqListResponse> IFaqRepository.GetList()
        {
            try
            {
                var faqTopicList = unitOfWork.GetRepository<Entities.FaqTopic>()
                .Query(x => x.IsDeleted == false && x.IsActive == true)
                .Include(x => x.FaqQuestions)
                .OrderByDescending(x => x.CreatedOn)
                .ToList();

                var response = new FaqListResponse();

                response.FaqTopicList = faqTopicList.Select(x => new FaqTopic
                {
                    Id = x.Id,
                    TopicIcon = GetFileFullUrl(x.TopicIcon),
                    TopicNameEn = x.TopicTitleEn,
                    TopicNameMm = x.TopicTitleMm,
                })
                .ToList();

                response.FaqPopularQuestionList = new List<FaqQuestion>();
                faqTopicList?.ForEach(topic =>
                {
                    var featuredFaqList = topic.FaqQuestions.Where(x => x.IsFeatured == true)
                    .OrderBy(x => x.Sort)
                    .Select(x => new FaqQuestion
                    {
                        QuestionEn = x.QuestionEn,
                        QuestionMm = x.QuestionMm,
                        AnswerEn = x.AnswerEn,
                        AnswerMm = x.AnswerMm,
                    })
                    .ToList();

                    response.FaqPopularQuestionList.AddRange(featuredFaqList);
                });

                return errorCodeProvider.GetResponseModel(ErrorCode.E0, response);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{httpContext?.HttpContext?.Request.Method} " +
                    $"=> {httpContext?.HttpContext?.Request.Path} " +
                    $"=> Exception " +
                    $"=> {JsonConvert.SerializeObject(ex)}");

                return errorCodeProvider.GetResponseModel<FaqListResponse>(ErrorCode.E500);
            }
            
        }

        ResponseModel<List<FaqQuestion>> IFaqRepository.GetListByTopicId(Guid? topicId, string? question)
        {
            try
            {


                var query = unitOfWork.GetRepository<Entities.FaqQuestion>()
                .Query(x => x.FaqTopicId == topicId);

                if(!string.IsNullOrEmpty(question))
                {
                    query = query.Where(x => x.QuestionEn.Contains(question) || x.QuestionMm.Contains(question));
                }

                var faqList = query
                    .OrderBy(x => x.Sort)
                    .Select(x => new FaqQuestion
                    {
                        QuestionEn = x.QuestionEn,
                        QuestionMm = x.QuestionMm,
                        AnswerEn = x.AnswerEn,
                        AnswerMm = x.AnswerMm,
                    })
                    .ToList();

                return errorCodeProvider.GetResponseModel(ErrorCode.E0, faqList);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{httpContext?.HttpContext?.Request.Method} " +
                    $"=> {httpContext?.HttpContext?.Request.Path} " +
                    $"=> Exception " +
                    $"=> {JsonConvert.SerializeObject(ex)}");

                return errorCodeProvider.GetResponseModel<List<FaqQuestion>>(ErrorCode.E500);
            }
        }
    }
}
