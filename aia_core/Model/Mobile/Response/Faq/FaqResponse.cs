using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response.Faq
{
    public class FaqTopic
    {
        public Guid? Id { get; set; }
        public string? TopicNameEn { get; set; }
        public string? TopicNameMm { get; set;}
        public string? TopicIcon { get; set; }
    }


    public class FaqQuestion    {
        public string? QuestionEn { get; set; }
        public string? QuestionMm { get; set; }
        public string? AnswerEn { get; set; }
        public string? AnswerMm { get; set; }
    }

    public class FaqListResponse
    {
        public List<FaqTopic>? FaqTopicList { get; set; }
        public List<FaqQuestion>? FaqPopularQuestionList { get; set; }
    }
}
