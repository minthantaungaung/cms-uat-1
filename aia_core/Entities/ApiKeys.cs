using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Entities
{
    public class ApiKeys
    {      
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
