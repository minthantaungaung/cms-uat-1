using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public partial class Province
{
    public string? country_code { get; set; }
    [Key]
    public string province_code { get; set; }
    public string? province_eng_name { get; set; }
    public string? province_bur_name {get;set;}
}