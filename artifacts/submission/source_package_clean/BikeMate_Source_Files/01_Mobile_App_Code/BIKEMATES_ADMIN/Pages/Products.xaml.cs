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
    private int? _selectedProductId;
    private string? _selectedImageUrl;
    private IReadOnlyList<string> _productCategories = [];
    private bool _updatingCategorySearch;
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

            ReloadProductCategories();
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

            ProductEditorStatusLabel.Text = "Uploading product image...";
            var uploaded = await BikeMateDatabaseService.UploadShopFileAsync(photo, "product-images");
            _selectedImageUrl = uploaded.Url;
            ApplyProductPreview(_selectedImageUrl);
            ProductEditorStatusLabel.Text = "Product image uploaded. Finish the product details to save it.";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Image", $"Unable to upload image: {ex.Message}", "OK");
        }
    }

    private async void AddProduct_Clicked(object sender, EventArgs e)
    {
        if (!ValidateProductInputs(out string name, out string category, out decimal price, out int stock, out string? description, out string imageUrl))
            return;

        try
        {
            await BikeMateDatabaseService.AddProductAsync(new UpsertAdminProduct(
                name,
                BuildStoredDescription(category, description),
                price,
                stock,
                true,
                imageUrl));

            await LoadProductsAsync();
            ClearEditor();
            await DisplayAlert("Product Added", "The product was saved to the shop inventory API.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Product Error", ex.Message, "OK");
        }
    }

    private async void UpdateProduct_Clicked(object sender, EventArgs e)
    {
        var selected = GetSelectedProduct();
        if (selected is null)
        {
            await DisplayAlert("Select Product", "Tap a product in the grid first.", "OK");
            return;
        }

        if (!ValidateProductInputs(out string name, out string category, out decimal price, out int stock, out string? description, out string imageUrl))
            return;

        try
        {
            await BikeMateDatabaseService.UpdateProductAsync(
                selected.ProductId,
                new UpsertAdminProduct(name, BuildStoredDescription(category, description), price, stock, selected.IsActive, imageUrl));

            await LoadProductsAsync();
            ClearEditor();
            await DisplayAlert("Product Updated", "The selected product was updated in the API.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Product Error", ex.Message, "OK");
        }
    }

    private async void DeleteProduct_Clicked(object sender, EventArgs e)
    {
        var selected = GetSelectedProduct();
        if (selected is null)
        {
            await DisplayAlert("Select Product", "Tap a product in the grid first.", "OK");
            return;
        }

        await DeleteProductAsync(selected);
    }

    private void EditProduct_Clicked(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ProductItem product })
        {
            ProductsCollectionView.SelectedItem = product;
            SelectProduct(product);
        }
    }

    private async void DeleteProductRow_Clicked(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: ProductItem product })
        {
            ProductsCollectionView.SelectedItem = product;
            SelectProduct(product);
            await DeleteProductAsync(product);
        }
    }

    private void ClearProductEditor_Clicked(object sender, EventArgs e)
    {
        ClearEditor();
    }

    private async Task DeleteProductAsync(ProductItem selected)
    {
        bool confirm = await DisplayAlert("Delete Product", $"Delete {selected.Name}?", "Delete", "Cancel");
        if (!confirm)
            return;

        try
        {
            await BikeMateDatabaseService.DeleteProductAsync(selected.ProductId);
            await LoadProductsAsync();
            ClearEditor();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Product Error", ex.Message, "OK");
        }
    }

    private void ProductsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectProduct(e.CurrentSelection.FirstOrDefault() as ProductItem);
    }

    private void SelectProduct(ProductItem? product)
    {
        _selectedProduct = product;
        _selectedProductId = product?.ProductId;
        if (product is null)
        {
            ProductEditorStatusLabel.Text = "Select an item below to update it, or enter new details to add a product.";
            return;
        }

        ProductNameEntry.Text = product.Name;
        SetSelectedCategory(product.Category);
        PriceEntry.Text = product.Price.ToString("0.##");
        StockEntry.Text = product.Stock.ToString();
        DescriptionEditor.Text = product.Description;
        _selectedImageUrl = product.ProductImageUrl;
        ApplyProductPreview(_selectedImageUrl);
        ProductEditorStatusLabel.Text = $"Editing {product.Name}";
    }

    private void ProductSearchBar_TextChanged(object sender, TextChangedEventArgs e) => RefreshProductGrid();

    private void ProductCategorySearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingCategorySearch)
        {
            return;
        }

        RefreshProductCategoryPicker();
    }

    private async void AddProductCategory_Clicked(object sender, EventArgs e)
    {
        var typed = ProductCategoryNameEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(typed))
        {
            await DisplayAlert("Product Category", "Enter the full category name before saving.", "OK");
            return;
        }

        try
        {
            var category = ProductCategoryStore.Add(typed);
            ReloadProductCategories();
            SetSelectedCategory(category);
            ProductCategoryNameEntry.Text = string.Empty;
            await DisplayAlert("Product Category", $"{category} is ready to use for products.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Product Category", ex.Message, "OK");
        }
    }

    private async void DeleteProductCategory_Clicked(object sender, EventArgs e)
    {
        var selectedCategory = CategoryPicker.SelectedItem?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(selectedCategory))
        {
            await DisplayAlert("Product Category", "Select a category to delete.", "OK");
            return;
        }

        if (ProductItems.Any(product => string.Equals(product.Category, selectedCategory, StringComparison.OrdinalIgnoreCase)))
        {
            await DisplayAlert("Product Category", "This category is currently used by products and cannot be deleted.", "OK");
            return;
        }

        var confirm = await DisplayAlert("Delete Category", $"Delete {selectedCategory} from this device's category list?", "Delete", "Cancel");
        if (!confirm)
        {
            return;
        }

        try
        {
            ProductCategoryStore.Remove(selectedCategory);
            ReloadProductCategories();
            ProductCategorySearchBar.Text = string.Empty;
            await DisplayAlert("Product Category", "Category removed from the available product category list.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Product Category", ex.Message, "OK");
        }
    }

    private bool ValidateProductInputs(out string name, out string category, out decimal price, out int stock, out string? description, out string imageUrl)
    {
        name = ProductNameEntry.Text?.Trim() ?? string.Empty;
        category = CategoryPicker.SelectedItem?.ToString() ?? string.Empty;
        description = DescriptionEditor.Text?.Trim();
        imageUrl = _selectedImageUrl?.Trim() ?? string.Empty;
        decimal.TryParse(PriceEntry.Text, out price);
        int.TryParse(StockEntry.Text, out stock);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category) || price <= 0 || stock < 0 || string.IsNullOrWhiteSpace(imageUrl))
        {
            _ = DisplayAlert("Missing Details", "Please enter product name, category, price, stock, and one product image.", "OK");
            return false;
        }

        try
        {
            category = ProductCategoryStore.Add(category);
            ReloadProductCategories();
            SetSelectedCategory(category);
        }
        catch (Exception ex)
        {
            _ = DisplayAlert("Product Category", ex.Message, "OK");
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

        if (_selectedProductId is int selectedId)
        {
            var selected = VisibleProducts.FirstOrDefault(product => product.ProductId == selectedId);
            ProductsCollectionView.SelectedItem = selected;
            _selectedProduct = selected ?? ProductItems.FirstOrDefault(product => product.ProductId == selectedId);
        }
    }

    private void ClearEditor()
    {
        _selectedProduct = null;
        _selectedProductId = null;
        _selectedImageUrl = null;
        ProductNameEntry.Text = string.Empty;
        CategoryPicker.SelectedItem = null;
        ProductCategorySearchBar.Text = string.Empty;
        ProductCategoryNameEntry.Text = string.Empty;
        RefreshProductCategoryPicker();
        PriceEntry.Text = string.Empty;
        StockEntry.Text = string.Empty;
        DescriptionEditor.Text = string.Empty;
        ProductPreviewImage.Source = null;
        ProductPreviewLabel.IsVisible = true;
        ProductsCollectionView.SelectedItem = null;
        ProductEditorStatusLabel.Text = "Select an item below to update it, or enter new details to add a product.";
    }

    private void ApplyProductPreview(string? imageUrl)
    {
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            ProductPreviewImage.Source = ImageSource.FromUri(uri);
            ProductPreviewLabel.IsVisible = false;
            return;
        }

        ProductPreviewImage.Source = null;
        ProductPreviewLabel.IsVisible = true;
    }

    private ProductItem? GetSelectedProduct()
    {
        if (_selectedProduct is not null)
        {
            return _selectedProduct;
        }

        return _selectedProductId is int id
            ? ProductItems.FirstOrDefault(product => product.ProductId == id)
            : null;
    }

    private static string BuildStoredDescription(string category, string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? $"Category: {category}"
            : $"Category: {category}\n{description}";
    }

    private void ReloadProductCategories()
    {
        _productCategories = ProductCategoryStore.Load(ProductItems.Select(product => product.Category));
        RefreshProductCategoryPicker();
    }

    private void RefreshProductCategoryPicker()
    {
        var search = ProductCategorySearchBar?.Text?.Trim() ?? string.Empty;
        var categories = _productCategories
            .Where(category => string.IsNullOrWhiteSpace(search) || category.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();
        CategoryPicker.ItemsSource = categories;
        if (categories.Count == 1 && string.Equals(categories[0], search, StringComparison.OrdinalIgnoreCase))
        {
            CategoryPicker.SelectedItem = categories[0];
        }
        else if (CategoryPicker.SelectedItem is string selected && !categories.Contains(selected, StringComparer.OrdinalIgnoreCase))
        {
            CategoryPicker.SelectedItem = null;
        }
    }

    private void SetSelectedCategory(string category)
    {
        if (!_productCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
        {
            ProductCategoryStore.Add(category);
            ReloadProductCategories();
        }

        _updatingCategorySearch = true;
        ProductCategorySearchBar.Text = category;
        _updatingCategorySearch = false;
        RefreshProductCategoryPicker();
        CategoryPicker.SelectedItem = _productCategories.FirstOrDefault(item => string.Equals(item, category, StringComparison.OrdinalIgnoreCase)) ?? category;
    }
}

public sealed class ProductItem
{
    public ProductItem(int productId, string name, string category, string description, decimal price, int stock, bool isActive, string? productImageUrl)
    {
        ProductId = productId;
        Name = name;
        Category = category;
        Description = description;
        Price = price;
        Stock = stock;
        IsActive = isActive;
        ProductImageUrl = productImageUrl;
    }

    public int ProductId { get; }
    public string Name { get; }
    public string Category { get; }
    public string Description { get; }
    public decimal Price { get; }
    public int Stock { get; }
    public bool IsActive { get; }
    public string? ProductImageUrl { get; }
    public ImageSource? ProductImage => Uri.TryCreate(ProductImageUrl, UriKind.Absolute, out var uri)
        ? ImageSource.FromUri(uri)
        : null;

    public string PriceText => $"PHP {Price:N2}";
    public string StockText => $"Stock: {Stock}";
    public string StockStatus => Stock <= 5 ? "LOW STOCK" : "IN STOCK";
    public Color StockStatusColor => Stock <= 5 ? Color.FromArgb("#DC2626") : Color.FromArgb("#16A34A");

    public static ProductItem FromApi(AdminProduct product)
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
            product.ProductImageUrl);
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




