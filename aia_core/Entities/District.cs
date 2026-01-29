using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public partial class District
{
    public string? province_code { get; set; }
    [Key]
    public string district_code { get; set; }
    public string? district_eng_name { get; set; }
    public string? district_bur_name {get;set;}
}