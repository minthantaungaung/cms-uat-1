using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Cms.Request.Faq
{
    public class FaqRequest
    {
        [Required]
        public string? TopicTitleEn {  get; set; }

        [Required]
        public string? TopicTitleMm { get; set; }
        public IFormFile? TopicIcon { get; set; }

       

    }


    public class FaqQuestion
    {
        [Required]
        public string? QuestionEn { get; set; }

        [Required]
        public string? QuestionMm { get; set; }

        [Required]

        public string? AnswerEn {  get; set; }

        [Required]
        public string? AnswerMm { get; set; }

        [Required]
        public bool? IsFeatured { get; set; }

        [Required]
        public int? Sort {  get; set; }
    }

    public class CreateFaqRequest : FaqRequest
    {
        [Required]
        public List<FaqQuestion>? FaqQuestions { get; set; }
    }

    public class UpdateFaqRequest : FaqRequest
    {
        [Required]
        public Guid? Id { get; set; }
        public List<FaqQuestion>? FaqQuestions { get; set; }
    }

    public class ListFaqRequest
    {
        [Required]
        public int Page { get; set; }

        [Required]
        public int Size { get; set; }

        public string? Title { get; set; }

        public bool? IsActive { get; set; }
    }


    public class ToggleFaqRequest
    {
        [Required]
        public Guid Id { get; set; }
    }
}
