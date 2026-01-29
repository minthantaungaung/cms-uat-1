using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Entities
{
    public partial class PartialDisabilityProduct
    {
        public Guid? Id { get; set; }
        public Guid? DisabiltiyId { get; set; }
        public Guid? ProductId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
