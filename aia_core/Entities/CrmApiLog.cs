
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public class CrmApiLog
{
    [Key]   
    public Guid ID { get; set; }
    public string RequestBody { get; set; }
    public string ResponseBody { get; set; }
    public DateTime CreatedDate { get; set; }
    
}