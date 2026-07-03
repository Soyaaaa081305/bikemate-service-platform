using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class ProductImage
{
    public int ProductImageId { get; set; }

    public int ProductId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
