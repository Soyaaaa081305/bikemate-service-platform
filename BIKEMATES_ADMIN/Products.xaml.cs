using System.Collections.ObjectModel;
using BIKEMATES_ADMIN.Pages.Main;

namespace BIKEMATES_ADMIN;

public partial class Products : ContentPage
{
    private readonly ObservableCollection<ProductItem> _products = new();

    public Products()
    {
        InitializeComponent();
        ProductGridView.ItemsSource = _products;
        CategoryPicker.ItemsSource = new[]
        {
            "Frames",
            "Drivetrain",
            "Wheels",
            "Brakes",
            "Cockpit",
            "Accessories",
            "Wearables"
        };
        CategoryPicker.SelectedIndex = 0;
        LoadSampleProducts();
        RefreshProducts();
    }

    private void LoadSampleProducts()
    {
        if (_products.Count > 0)
        {
            return;
        }

        _products.Add(new ProductItem("Frames", "Alloy Mountain Frame", 8500m, 4));
        _products.Add(new ProductItem("Brakes", "Hydraulic Brake Set", 3200m, 7));
        _products.Add(new ProductItem("Wearables", "Bike Helmet", 1450m, 12));
    }

    private void RefreshProducts()
    {
        ProductPicker.ItemsSource = null;
        ProductPicker.ItemsSource = _products.Select(product => product.DisplayName).ToList();
        ProductCountLabel.Text = _products.Count.ToString();
        LowStockLabel.Text = _products.Count(product => product.Stock <= 4).ToString();
        InventorySummaryLabel.Text = _products.Count == 0
            ? "No products added yet."
            : string.Join(Environment.NewLine, _products.Select(product => product.Summary));
    }

    private async void OnAddProductClicked(object sender, EventArgs e)
    {
        var name = ProductNameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("Product Needed", "Enter a product name first.", "OK");
            return;
        }

        if (!decimal.TryParse(PriceEntry.Text, out var price) || price < 0)
        {
            await DisplayAlert("Price Needed", "Enter a valid price.", "OK");
            return;
        }

        if (!int.TryParse(StockEntry.Text, out var stock) || stock < 0)
        {
            await DisplayAlert("Stock Needed", "Enter a valid stock count.", "OK");
            return;
        }

        var category = CategoryPicker.SelectedItem?.ToString() ?? "Accessories";
        _products.Add(new ProductItem(category, name, price, stock));
        ProductNameEntry.Text = string.Empty;
        PriceEntry.Text = string.Empty;
        StockEntry.Text = string.Empty;
        ProductNotesEditor.Text = string.Empty;
        RefreshProducts();
        ProductChangeLogLabel.Text = $"{name} was added to the GridView.";
        await DisplayAlert("Product Added", $"{name} is now listed in {category}.", "OK");
    }

    private void OnProductGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not ProductItem selected)
        {
            return;
        }

        var index = _products.IndexOf(selected);
        if (index < 0)
        {
            return;
        }

        ProductPicker.SelectedIndex = index;
        ProductChangeLogLabel.Text = $"{selected.Name} selected from the GridView.";
    }

    private void OnProductSelected(object sender, EventArgs e)
    {
        if (ProductPicker.SelectedIndex < 0 || ProductPicker.SelectedIndex >= _products.Count)
        {
            return;
        }

        var product = _products[ProductPicker.SelectedIndex];
        CategoryPicker.SelectedItem = product.Category;
        ProductNameEntry.Text = product.Name;
        PriceEntry.Text = product.Price.ToString("0.##");
        StockEntry.Text = product.Stock.ToString();
    }

    private async void OnEditProductClicked(object sender, EventArgs e)
    {
        if (ProductPicker.SelectedIndex < 0 || ProductPicker.SelectedIndex >= _products.Count)
        {
            await DisplayAlert("Select Product", "Choose a product to edit.", "OK");
            return;
        }

        if (!decimal.TryParse(PriceEntry.Text, out var price) || !int.TryParse(StockEntry.Text, out var stock))
        {
            await DisplayAlert("Check Details", "Price and stock must be valid numbers.", "OK");
            return;
        }

        var current = _products[ProductPicker.SelectedIndex];
        _products[ProductPicker.SelectedIndex] = current with
        {
            Category = CategoryPicker.SelectedItem?.ToString() ?? current.Category,
            Name = string.IsNullOrWhiteSpace(ProductNameEntry.Text) ? current.Name : ProductNameEntry.Text.Trim(),
            Price = price,
            Stock = stock
        };
        RefreshProducts();
        ProductChangeLogLabel.Text = $"{_products[ProductPicker.SelectedIndex].Name} was updated in the GridView.";
        await DisplayAlert("Product Updated", "The selected product was updated.", "OK");
    }

    private async void OnDeleteProductClicked(object sender, EventArgs e)
    {
        if (ProductPicker.SelectedIndex < 0 || ProductPicker.SelectedIndex >= _products.Count)
        {
            await DisplayAlert("Select Product", "Choose a product to delete.", "OK");
            return;
        }

        var product = _products[ProductPicker.SelectedIndex];
        var confirmed = await DisplayAlert("Delete Product", $"Remove {product.Name}?", "Delete", "Cancel");
        if (!confirmed)
        {
            return;
        }

        _products.RemoveAt(ProductPicker.SelectedIndex);
        ProductPicker.SelectedIndex = -1;
        RefreshProducts();
        ProductChangeLogLabel.Text = $"{product.Name} was removed from the GridView.";
    }

    private async void OnMenuClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MenuPage());
    private async void OnHomeClicked(object sender, EventArgs e) => await Navigation.PushAsync(new MainPage());
    private async void OnMessagesClicked(object sender, EventArgs e) => await Navigation.PushAsync(new Messages());
    private async void OnProfileClicked(object sender, EventArgs e) => await Navigation.PushAsync(new ShopProfile());

    public sealed record ProductItem(string Category, string Name, decimal Price, int Stock)
    {
        public string DisplayName => $"{Name} - {Category}";
        public string Summary => $"{Category}: {Name} | PHP {Price:0.00} | Stock: {Stock}";
        public string PriceText => $"PHP {Price:0.00}";
        public string StockText => $"Stock: {Stock}";
        public string StockState => Stock <= 4 ? "LOW" : "OK";
    }
}
