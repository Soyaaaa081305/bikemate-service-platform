using System;
using System.Collections.Generic;

namespace BikeMate.WebAdmin.Data;

public partial class Product
{
    public int ProductId { get; set; }

    public int ShopId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? ProductDescription { get; set; }

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual Shop Shop { get; set; } = null!;
}
