using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Cms.Response.Faq
{
    public class FaqResponse
    {
        public Guid? Id { get; set; }
        public string? TopicTitleEn { get; set; }
        public string? TopicTitleMm { get; set; }
        public IFormFile? TopicIconFile { get; set; }
        public string? TopicIconFileUrl { get; set; }
        public List<FaqQuestion>? FaqQuestions { get; set; }
        public int?  QuestionCount { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    public class FaqQuestion
    {
        public Guid? FaqQuestionId { get; set; }
        public Guid? FaqTopicId { get; set; }
        public string? QuestionEn { get; set; }
        public string? QuestionMm { get; set; }

        public string? AnswerEn { get; set; }
        public string? AnswerMm { get; set; }
        public bool? IsFeatured { get; set; }
        public int? Sort { get; set; }
    }
}
