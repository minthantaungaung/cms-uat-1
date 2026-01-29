using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ClaimSetting
{
    public Guid ID { get; set; }
    public string Name { get; set; }
    public string Name_MM { get; set; }
    public string Code {get;set;}
    public DateTime? CreatedDate { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public Guid? UpdatedBy { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDelete { get; set; }
    public int? Sort { get; set; }
}