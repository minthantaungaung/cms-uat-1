using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Entities
{
    public partial class FaqQuestion
    {
        public Guid? Id { get; set; }      
        public string? QuestionEn { get; set; }
        public string? QuestionMm { get; set; }
        public string? AnswerEn { get; set; }
        public string? AnswerMm { get; set; }
        public bool? IsFeatured { get; set; }
        public int? Sort { get; set; }
        public Guid? FaqTopicId { get; set; }
        public FaqTopic? FaqTopic { get; set; }
    }
}
