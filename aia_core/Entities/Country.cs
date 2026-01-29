using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public partial class Country
{
    public decimal id { get; set; }
    [Key]
    public string code { get; set; }
    public string? description { get; set; }
    public string? bur_description {get;set;}
}