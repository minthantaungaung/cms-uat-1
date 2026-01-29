using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Entities
{
    public class CI_Product
    {
        public Guid? Id { get; set; }
        public Guid? DisabiltiyId { get; set; }
        public Guid? ProductId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
