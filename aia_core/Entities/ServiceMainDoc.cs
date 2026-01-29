using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ServiceMainDoc
{
    public Guid Id { get; set; }

    public string? ServiceType { get; set; }

    public Guid? ServiceId { get; set; }

    public Guid? MainId { get; set; }

    public string? FormId { get; set; }

    public string? DocName { get; set; }

    public string? CmsReqeust { get; set; }

    public string? CmsResponse { get; set; }

    public DateTime? CmsRequestOn { get; set; }

    public DateTime? CmsResponseOn { get; set; }

    public string? UploadStatus { get; set; }

    public string? DocType { get; set; }

    public string? NrcDocType { get; set; }
}
