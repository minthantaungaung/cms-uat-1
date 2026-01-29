using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public class UsersTemp
{
    [Key]
    public Guid id { get; set; }
    public string? name { get; set; }
    public DateTime? date_of_birth { get; set; }
    public string? gender { get; set; }
    public string? nrc { get; set; }
    public string? passport { get; set; }
    public string? others { get; set; }
    public string? phone_no { get; set; }
    public string? email { get; set; }
    public string? okta_user_id { get; set; }
    public string? okta_user_name { get; set; }
    public string? password { get; set; }
    public DateTime? registration_date { get; set; }
    public string? migration_status { get; set; }
    public string? migration_failed_reason { get; set; }
    public DateTime? record_created_date { get; set; }
    public DateTime? record_updated_date { get; set; }
    public bool? is_done { get; set; }
    public string? migrate_status { get; set; }
    public string? migrate_log { get; set; }
    public DateTime? migrate_date { get; set; }
    public string? okta_register_request { get; set; }
}