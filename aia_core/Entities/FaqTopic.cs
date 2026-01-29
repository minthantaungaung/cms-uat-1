using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Entities
{
    public partial class FaqTopic
    {
        public Guid? Id { get; set; }

        public string? TopicTitleEn { get; set; }

        public string? TopicTitleMm { get; set; }

        public string? TopicIcon { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsDeleted { get; set; }

        public string? CreatedBy { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? CreatedOn { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public virtual ICollection<FaqQuestion>? FaqQuestions { get; set; }
    }
}
