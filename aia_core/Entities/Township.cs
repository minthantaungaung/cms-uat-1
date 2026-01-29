using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public partial class Township
{
    public string? district_code { get; set; }
    [Key]
    public string township_code { get; set; }
    public string? township_eng_name { get; set; }
    public string? township_bur_name {get;set;}
}