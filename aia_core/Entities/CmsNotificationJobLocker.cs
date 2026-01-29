using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Entities
{
    public partial class CmsNotificationJobLocker
    {
        public Guid? Id { get; set; }
        public Guid? NotiId {  get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
