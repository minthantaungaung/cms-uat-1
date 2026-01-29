using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class DocConfig
{
    public Guid Id { get; set; }

    public string? DocType { get; set; }

    public string? DocTypeId { get; set; }

    public string? DocName { get; set; }

    public string? ShowingFor { get; set; }

    public DateTime? CreatedOn { get; set; }

    public DateTime? UpdatedOn { get; set; }
}
