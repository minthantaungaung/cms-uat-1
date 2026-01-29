using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class PromotionProduct
{
    public Guid Id { get; set; }

    public Guid? BlogId { get; set; }

    public Guid? ProductId { get; set; }

    public virtual Blog? Blog { get; set; }
}
