using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public partial class OnetimeToken
{
    public Guid Id { get; set; }
    public string? Otp {get;set;}
}