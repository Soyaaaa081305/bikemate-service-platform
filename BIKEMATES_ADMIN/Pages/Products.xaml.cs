using System.Collections.ObjectModel;
using BIKEMATES_ADMIN.Services;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Media;

namespace BIKEMATES_ADMIN.Pages;

public partial class Products : ContentPage
{
    public ObservableCollection<ProductItem> ProductItems { get; } = new();
    public ObservableCollection<ProductItem> VisibleProducts { get; } = new();

    private ProductItem? _selectedProduct;
    private ImageSource? _selectedImage;
    private bool _loaded;

    public Products()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loaded)
        {
            _loaded = true;
            await LoadProductsAsync();
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            ProductItems.Clear();
            foreach (var product in await BikeMateDatabaseService.GetProductsAsync())
            {
                ProductItems.Add(ProductItem.FromApi(product));
            }

            RefreshProductGrid();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Products", $"Unable to load products from API: {ex.Message}", "OK");
        }
    }

    private async void PickProductImage_Clicked(object sender, EventArgs e)
    {
        try
        {
            FileResult? photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions
            {
                Title = "Choose product image"
            });

            if (photo is null)
                return;

            Stream stream = await photo.OpenReadAsync();
            _selectedImage = ImageSource.FromStream(() => stream);
            ProductPreviewImage.Source = _selectedImage;
            ProductPreviewLabel.IsVisible = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Image", $"Unable to pick image: {ex.Message}", "OK");
        }
    }

    private async void AddProduct_Clicked(object sender, EventArgs e)
    {
        if (!ValidateProductInputs(out string name, out string category, out decimal price, out int stock, out string? description))
            return;

        try
        {
            var created = await BikeMateDatabaseService.AddProductAsync(new UpsertAdminProduct(
                name,
                BuildStoredDescription(category, description),
                price,
                stock,
                true));

            ProductItems.Insert(0, ProductItem.FromApi(created, _selectedImage));
            ClearEditor();
            RefreshProductGrid();
            await DisplayAlert("Product Added", "The product was saved to the shop inventory API.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Product Error", ex.Message, "OK");
        }
    }

    private async void UpdateProduct_Clicked(object sender, EventArgs e)
    {
        if (_selectedProduct is null)
        {
            await DisplayAlert("Select Product", "Tap a product in the grid first.", "OK");
            return;
        }

        if (!ValidateProductInputs(out string name, out string category, out decimal price, out int stock, out string? description))
            return;

        try
        {
            var updated = await BikeMateDatabaseService.UpdateProductAsync(
                _selectedProduct.ProductId,
                new UpsertAdminProduct(name, BuildStoredDescription(category, description), price, stock, _selectedProduct.IsActive));

            int index = ProductItems.IndexOf(_selectedProduct);
            if (index >= 0)
            {
                ProductItems[index] = ProductItem.FromApi(updated, _selectedImage ?? _selectedProduct.ProductImage);
            }

            ClearEditor();
            RefreshProductGrid();
            await DisplayAlert("Product Updated", "The selected product was updated in the API.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Product Error", ex.Message, "OK");
        }
    }

    private async void DeleteProduct_Clicked(object sender, EventArgs e)
    {
        if (_selectedProduct is null)
        {
            await DisplayAlert("Select Product", "Tap a product in the grid first.", "OK");
            return;
        }

        bool confirm = await DisplayAlert("Delete Product", $"Delete {_selectedProduct.Name}?", "Delete", "Cancel");
        if (!confirm)
            return;

        try
        {
            await BikeMateDatabaseService.DeleteProductAsync(_selectedProduct.ProductId);
            ProductItems.Remove(_selectedProduct);
            ClearEditor();
            RefreshProductGrid();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Product Error", ex.Message, "OK");
        }
    }

    private void ProductsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedProduct = e.CurrentSelection.FirstOrDefault() as ProductItem;
        if (_selectedProduct is null)
            return;

        ProductNameEntry.Text = _selectedProduct.Name;
        CategoryPicker.SelectedItem = _selectedProduct.Category;
        PriceEntry.Text = _selectedProduct.Price.ToString("0.##");
        StockEntry.Text = _selectedProduct.Stock.ToString();
        DescriptionEditor.Text = _selectedProduct.Description;
        _selectedImage = _selectedProduct.ProductImage;
        ProductPreviewImage.Source = _selectedImage;
        ProductPreviewLabel.IsVisible = _selectedImage is null;
    }

    private void ProductSearchBar_TextChanged(object sender, TextChangedEventArgs e) => RefreshProductGrid();

    private bool ValidateProductInputs(out string name, out string category, out decimal price, out int stock, out string? description)
    {
        name = ProductNameEntry.Text?.Trim() ?? string.Empty;
        category = CategoryPicker.SelectedItem?.ToString() ?? string.Empty;
        description = DescriptionEditor.Text?.Trim();
        decimal.TryParse(PriceEntry.Text, out price);
        int.TryParse(StockEntry.Text, out stock);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category) || price <= 0 || stock < 0)
        {
            _ = DisplayAlert("Missing Details", "Please enter product name, category, price, and stock.", "OK");
            return false;
        }

        return true;
    }

    private void RefreshProductGrid()
    {
        string search = ProductSearchBar?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
        VisibleProducts.Clear();

        foreach (ProductItem product in ProductItems.Where(p =>
            string.IsNullOrWhiteSpace(search) ||
            p.Name.ToLowerInvariant().Contains(search) ||
            p.Category.ToLowerInvariant().Contains(search) ||
            p.Description.ToLowerInvariant().Contains(search)))
        {
            VisibleProducts.Add(product);
        }
    }

    private void ClearEditor()
    {
        _selectedProduct = null;
        _selectedImage = null;
        ProductNameEntry.Text = string.Empty;
        CategoryPicker.SelectedItem = null;
        PriceEntry.Text = string.Empty;
        StockEntry.Text = string.Empty;
        DescriptionEditor.Text = string.Empty;
        ProductPreviewImage.Source = null;
        ProductPreviewLabel.IsVisible = true;
        ProductsCollectionView.SelectedItem = null;
    }

    private static string BuildStoredDescription(string category, string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? $"Category: {category}"
            : $"Category: {category}\n{description}";
    }
}

public sealed class ProductItem
{
    public ProductItem(int productId, string name, string category, string description, decimal price, int stock, bool isActive, ImageSource? productImage)
    {
        ProductId = productId;
        Name = name;
        Category = category;
        Description = description;
        Price = price;
        Stock = stock;
        IsActive = isActive;
        ProductImage = productImage;
    }

    public int ProductId { get; }
    public string Name { get; }
    public string Category { get; }
    public string Description { get; }
    public decimal Price { get; }
    public int Stock { get; }
    public bool IsActive { get; }
    public ImageSource? ProductImage { get; }

    public string PriceText => $"PHP {Price:N2}";
    public string StockText => $"Stock: {Stock}";
    public string StockStatus => Stock <= 5 ? "LOW STOCK" : "IN STOCK";
    public Color StockStatusColor => Stock <= 5 ? Color.FromArgb("#DC2626") : Color.FromArgb("#16A34A");

    public static ProductItem FromApi(AdminProduct product, ImageSource? productImage = null)
    {
        var (category, description) = SplitStoredDescription(product.ProductDescription);
        return new ProductItem(
            product.ProductId,
            product.ProductName,
            category,
            description,
            product.Price,
            product.StockQuantity,
            product.IsActive,
            productImage);
    }

    private static (string Category, string Description) SplitStoredDescription(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return ("Uncategorized", string.Empty);
        }

        const string prefix = "Category:";
        if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return ("Uncategorized", value.Trim());
        }

        var lines = value.Split('\n', 2, StringSplitOptions.TrimEntries);
        var category = lines[0][prefix.Length..].Trim();
        var description = lines.Length > 1 ? lines[1].Trim() : string.Empty;
        return (string.IsNullOrWhiteSpace(category) ? "Uncategorized" : category, description);
    }
}




