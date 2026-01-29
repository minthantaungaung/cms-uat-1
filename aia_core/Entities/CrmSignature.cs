
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public class CrmSignature
{
    [Key]   
    public Guid ID { get; set; }
    public string SignatureValue { get; set; }
    public string RequestBody { get; set; }
    public DateTime CreatedDate { get; set; }
    
}