using System.Collections.ObjectModel;
using BIKEMATES_ADMIN.Pages.Account;
using BIKEMATES_ADMIN.Services;
using Microsoft.Maui.Media;

namespace BIKEMATES_ADMIN.Pages;

public partial class ShopSetupPage : ContentPage
{
    public ObservableCollection<ProductItem> SetupProducts { get; } = new();
    public ObservableCollection<SetupServiceItem> SetupServices { get; } = new();

    private readonly List<AdminServiceCategory> _serviceCategories = [];
    private IReadOnlyList<string> _productCategories = [];
    private ShopSetupStatus _status;
    private SetupStep _currentStep;
    private string? _productImageUrl;
    private bool _updatingProductCategorySearch;
    private bool _updatingServiceCategorySearch;
    private bool _loaded;

    public ShopSetupPage(ShopSetupStatus status)
    {
        InitializeComponent();
        BindingContext = this;
        _status = status;
        _currentStep = FirstIncompleteStep(status);
        ApplyStatus();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_loaded)
        {
            return;
        }

        _loaded = true;
        LoadProductCategories();
        await LoadServiceCategoriesAsync();
        await RefreshStatusAsync();
        await RefreshOfferSummariesAsync();
    }

    private async Task LoadServiceCategoriesAsync()
    {
        try
        {
            _serviceCategories.Clear();
            _serviceCategories.AddRange(await BikeMateDatabaseService.GetServiceCategoriesAsync());
            RefreshServiceCategoryPicker();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Services", $"Unable to load service categories: {ex.Message}", "OK");
        }
    }

    private async Task RefreshStatusAsync()
    {
        try
        {
            _status = await BikeMateDatabaseService.GetShopSetupStatusAsync();
            ApplyStatus();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Shop Setup", $"Unable to refresh setup status: {ex.Message}", "OK");
        }
    }

    private async Task RefreshOfferSummariesAsync()
    {
        try
        {
            SetupProducts.Clear();
            foreach (var product in await BikeMateDatabaseService.GetProductsAsync())
            {
                SetupProducts.Add(ProductItem.FromApi(product));
            }

            SetupServices.Clear();
            foreach (var service in await BikeMateDatabaseService.GetShopServicesAsync())
            {
                SetupServices.Add(SetupServiceItem.FromApi(service));
            }

            LoadProductCategories();
            UpdateStepPresentation();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Shop Setup", $"Unable to load current products and services: {ex.Message}", "OK");
        }
    }

    private void ApplyStatus()
    {
        var profile = _status.Profile;
        ShopNameLabel.Text = profile.ShopName;
        DescriptionEditor.Text = profile.ShopDescription ?? string.Empty;

        ApplyImage(CoverImage, CoverPlaceholder, profile.ShopImageUrl);
        ApplyImage(LogoImage, LogoPlaceholder, profile.ShopLogoUrl);
        ApplyImage(ReviewCoverImage, ReviewCoverPlaceholder, profile.ShopImageUrl);
        ApplyImage(ReviewLogoImage, ReviewLogoPlaceholder, profile.ShopLogoUrl);

        var completed = new[]
        {
            _status.HasCoverPhoto,
            _status.HasProfilePicture,
            _status.HasDescription,
            _status.HasProducts,
            _status.HasServices
        }.Count(value => value);

        SetupProgress.Progress = completed / 5d;
        SetupStatusLabel.Text = _status.IsComplete
            ? "Setup complete. Your shop is ready for customer-facing pages."
            : $"{completed}/5 required items complete. Finish the remaining items before opening the dashboard.";

        CoverStepLabel.Text = StepText(_status.HasCoverPhoto, "Cover photo");
        LogoStepLabel.Text = StepText(_status.HasProfilePicture, "Profile picture");
        DescriptionStepLabel.Text = StepText(_status.HasDescription, "Description");
        ProductsStepLabel.Text = StepText(_status.HasProducts, "Product");
        ServicesStepLabel.Text = StepText(_status.HasServices, "Service");
        UpdateStepPresentation();
    }

    private static void ApplyImage(Image image, Label placeholder, string? url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            image.Source = ImageSource.FromUri(uri);
            placeholder.IsVisible = false;
            return;
        }

        image.Source = null;
        placeholder.IsVisible = true;
    }

    private static string StepText(bool complete, string label)
    {
        return complete ? $"Done - {label}" : $"Needed - {label}";
    }

    private async void ChooseCover_Clicked(object? sender, EventArgs e)
    {
        await PickAndSaveImageAsync("shop-cover", BikeMateDatabaseService.UpdateShopCoverImageAsync);
    }

    private async void ChooseLogo_Clicked(object? sender, EventArgs e)
    {
        await PickAndSaveImageAsync("shop-logo", BikeMateDatabaseService.UpdateShopLogoAsync);
    }

    private async void ChooseProductImage_Clicked(object? sender, EventArgs e)
    {
        try
        {
            var photo = (await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
            {
                Title = "Choose product image"
            })).FirstOrDefault();

            if (photo is null)
            {
                return;
            }

            var uploaded = await BikeMateDatabaseService.UploadShopFileAsync(photo, "product-images");
            _productImageUrl = uploaded.Url;
            ApplyImage(ProductImage, ProductImagePlaceholder, _productImageUrl);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Product Image", ex.Message, "OK");
        }
    }

    private async Task PickAndSaveImageAsync(string folder, Func<string, Task> saveAsync)
    {
        try
        {
            var photo = (await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
            {
                Title = folder == "shop-cover" ? "Choose cover photo" : "Choose profile picture"
            })).FirstOrDefault();

            if (photo is null)
            {
                return;
            }

            var uploaded = await BikeMateDatabaseService.UploadShopFileAsync(photo, folder);
            await saveAsync(uploaded.Url);
            await RefreshStatusAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Image Upload", ex.Message, "OK");
        }
    }

    private async void SaveIdentity_Clicked(object? sender, EventArgs e)
        => await SaveIdentityAsync(showConfirmation: true);

    private async Task<bool> SaveIdentityAsync(bool showConfirmation)
    {
        var description = DescriptionEditor.Text?.Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            await DisplayAlertAsync("Shop Description", "Enter a short description before saving.", "OK");
            return false;
        }

        try
        {
            await BikeMateDatabaseService.UpdateShopProfileAsync(_status.Profile with
            {
                ShopDescription = description
            });
            await RefreshStatusAsync();
            if (showConfirmation)
            {
                await DisplayAlertAsync("Shop Identity", "Shop details were saved.", "OK");
            }

            return true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Shop Identity", ex.Message, "OK");
            return false;
        }
    }

    private async void AddProduct_Clicked(object? sender, EventArgs e)
    {
        var name = ProductNameEntry.Text?.Trim() ?? string.Empty;
        var category = ProductCategoryPicker.SelectedItem?.ToString() ?? ProductCategorySearchBar.Text?.Trim() ?? string.Empty;
        var description = ProductDescriptionEditor.Text?.Trim();
        decimal.TryParse(ProductPriceEntry.Text, out var price);
        int.TryParse(ProductStockEntry.Text, out var stock);
        var productImageUrl = _productImageUrl?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category) || price <= 0 || stock < 0 || string.IsNullOrWhiteSpace(productImageUrl))
        {
            await DisplayAlertAsync("Product", "Enter product name, category, price, stock, and one product image.", "OK");
            return;
        }

        try
        {
            category = ProductCategoryStore.Add(category);
            LoadProductCategories(category);
            await BikeMateDatabaseService.AddProductAsync(new UpsertAdminProduct(
                name,
                BuildStoredProductDescription(category, description),
                price,
                stock,
                true,
                productImageUrl));

            ProductNameEntry.Text = string.Empty;
            ProductCategoryPicker.SelectedItem = null;
            ProductCategorySearchBar.Text = string.Empty;
            RefreshProductCategoryPicker();
            ProductPriceEntry.Text = string.Empty;
            ProductStockEntry.Text = string.Empty;
            ProductDescriptionEditor.Text = string.Empty;
            _productImageUrl = null;
            ApplyImage(ProductImage, ProductImagePlaceholder, null);

            await RefreshStatusAsync();
            await RefreshOfferSummariesAsync();
            await DisplayAlertAsync("Product", "Product was added to your shop inventory.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Product", ex.Message, "OK");
        }
    }

    private async void DeleteSetupProduct_Clicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: ProductItem product })
        {
            return;
        }

        var confirm = await DisplayAlertAsync(
            "Remove product",
            $"Remove {product.Name} from your shop setup?",
            "Remove",
            "Cancel");
        if (!confirm)
        {
            return;
        }

        try
        {
            await BikeMateDatabaseService.DeleteProductAsync(product.ProductId);
            await RefreshStatusAsync();
            await RefreshOfferSummariesAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Product", ex.Message, "OK");
        }
    }

    private async void AddService_Clicked(object? sender, EventArgs e)
    {
        var categoryName = ServiceCategoryPicker.SelectedItem?.ToString() ?? ServiceCategorySearchBar.Text?.Trim() ?? string.Empty;
        var category = _serviceCategories.FirstOrDefault(item =>
            string.Equals(item.CategoryName, categoryName, StringComparison.OrdinalIgnoreCase));
        var serviceName = ServiceNameEntry.Text?.Trim() ?? string.Empty;
        var description = ServiceDescriptionEditor.Text?.Trim();
        decimal.TryParse(ServicePriceEntry.Text, out var price);
        int.TryParse(ServiceMinutesEntry.Text, out var minutes);

        if (string.IsNullOrWhiteSpace(categoryName) || string.IsNullOrWhiteSpace(serviceName) || price <= 0 || minutes <= 0)
        {
            await DisplayAlertAsync("Service", "Enter a service category, service name, base price, and minutes.", "OK");
            return;
        }

        try
        {
            if (category is null)
            {
                category = await BikeMateDatabaseService.AddServiceCategoryAsync(new UpsertAdminServiceCategory(categoryName, null));
                await LoadServiceCategoriesAsync();
                SetSelectedServiceCategory(category.CategoryName);
            }

            await BikeMateDatabaseService.AddShopServiceAsync(new UpsertAdminShopService(
                category.CategoryId,
                serviceName,
                description,
                price,
                minutes,
                true));

            ServiceCategoryPicker.SelectedItem = null;
            ServiceCategorySearchBar.Text = string.Empty;
            RefreshServiceCategoryPicker();
            ServiceNameEntry.Text = string.Empty;
            ServicePriceEntry.Text = string.Empty;
            ServiceMinutesEntry.Text = string.Empty;
            ServiceDescriptionEditor.Text = string.Empty;

            await RefreshStatusAsync();
            await RefreshOfferSummariesAsync();
            await DisplayAlertAsync("Service", "Service was added to your shop offers.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Service", ex.Message, "OK");
        }
    }

    private async void Continue_Clicked(object? sender, EventArgs e)
    {
        if (!_status.IsComplete)
        {
            await DisplayAlertAsync("Shop Setup", "Complete the required setup items first.", "OK");
            return;
        }

        BIKEMATES_ADMIN.App.SetRootPage(new AppShell());
    }

    private void BackStep_Clicked(object? sender, EventArgs e)
    {
        if (_currentStep == SetupStep.Identity)
        {
            return;
        }

        _currentStep--;
        UpdateStepPresentation();
    }

    private async void NextStep_Clicked(object? sender, EventArgs e)
    {
        if (!await CanLeaveCurrentStepAsync())
        {
            return;
        }

        if (_currentStep < SetupStep.Review)
        {
            _currentStep++;
            UpdateStepPresentation();
        }
    }

    private async Task<bool> CanLeaveCurrentStepAsync()
    {
        if (_currentStep == SetupStep.Identity)
        {
            var typedDescription = DescriptionEditor.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(typedDescription) &&
                !string.Equals(typedDescription, _status.Profile.ShopDescription, StringComparison.Ordinal))
            {
                if (!await SaveIdentityAsync(showConfirmation: false))
                {
                    return false;
                }
            }

            if (!_status.HasCoverPhoto || !_status.HasProfilePicture || !_status.HasDescription)
            {
                await DisplayAlertAsync(
                    "Shop Identity",
                    "Add a cover photo, profile picture, and saved shop description before continuing.",
                    "OK");
                return false;
            }
        }
        else if (_currentStep == SetupStep.Products && !_status.HasProducts)
        {
            await DisplayAlertAsync("Products", "Add at least one product with an image before continuing.", "OK");
            return false;
        }
        else if (_currentStep == SetupStep.Services && !_status.HasServices)
        {
            await DisplayAlertAsync("Services", "Add at least one bookable service before continuing.", "OK");
            return false;
        }

        return true;
    }

    private async void SignOut_Clicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("Sign Out", "Return to the shop admin login screen?", "Sign out", "Stay");
        if (!confirm)
        {
            return;
        }

        AppSession.CurrentUser = null;
        AppSession.AccessToken = null;
        BIKEMATES_ADMIN.App.SetRootPage(new NavigationPage(new Login())
        {
            BarBackgroundColor = Colors.White,
            BarTextColor = Color.FromArgb("#242424")
        });
    }

    private static string BuildStoredProductDescription(string category, string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? $"Category: {category}"
            : $"Category: {category}\n{description}";
    }

    private void ProductCategorySearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingProductCategorySearch)
        {
            return;
        }

        RefreshProductCategoryPicker();
    }

    private async void AddProductCategory_Clicked(object? sender, EventArgs e)
    {
        var categoryName = ProductCategorySearchBar.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            await DisplayAlertAsync("Product Category", "Type the product category name first.", "OK");
            return;
        }

        try
        {
            var category = ProductCategoryStore.Add(categoryName);
            LoadProductCategories(category);
            await DisplayAlertAsync("Product Category", $"{category} is ready to use for products.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Product Category", ex.Message, "OK");
        }
    }

    private void ServiceCategorySearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingServiceCategorySearch)
        {
            return;
        }

        RefreshServiceCategoryPicker();
    }

    private async void AddServiceCategory_Clicked(object? sender, EventArgs e)
    {
        var categoryName = ServiceCategorySearchBar.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            await DisplayAlertAsync("Service Category", "Type the service category name first.", "OK");
            return;
        }

        var description = await DisplayPromptAsync(
            "Service Category",
            "Optional: describe when shops should use this category.",
            "Save",
            "Skip",
            "Category description");

        try
        {
            var category = await BikeMateDatabaseService.AddServiceCategoryAsync(new UpsertAdminServiceCategory(categoryName, description));
            await LoadServiceCategoriesAsync();
            SetSelectedServiceCategory(category.CategoryName);
            await DisplayAlertAsync("Service Category", $"{category.CategoryName} is ready for services and customer filters.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Service Category", ex.Message, "OK");
        }
    }

    private void LoadProductCategories(string? selectedCategory = null)
    {
        _productCategories = ProductCategoryStore.Load(SetupProducts.Select(product => product.Category));
        RefreshProductCategoryPicker();
        if (!string.IsNullOrWhiteSpace(selectedCategory))
        {
            SetSelectedProductCategory(selectedCategory);
        }
    }

    private void RefreshProductCategoryPicker()
    {
        var search = ProductCategorySearchBar?.Text?.Trim() ?? string.Empty;
        var categories = _productCategories
            .Where(category => string.IsNullOrWhiteSpace(search) || category.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();
        ProductCategoryPicker.ItemsSource = categories;
        if (categories.Count == 1 && string.Equals(categories[0], search, StringComparison.OrdinalIgnoreCase))
        {
            ProductCategoryPicker.SelectedItem = categories[0];
        }
        else if (ProductCategoryPicker.SelectedItem is string selected && !categories.Contains(selected, StringComparer.OrdinalIgnoreCase))
        {
            ProductCategoryPicker.SelectedItem = null;
        }
    }

    private void SetSelectedProductCategory(string categoryName)
    {
        _updatingProductCategorySearch = true;
        ProductCategorySearchBar.Text = categoryName;
        _updatingProductCategorySearch = false;
        RefreshProductCategoryPicker();
        ProductCategoryPicker.SelectedItem = _productCategories.FirstOrDefault(item => string.Equals(item, categoryName, StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshServiceCategoryPicker()
    {
        var search = ServiceCategorySearchBar?.Text?.Trim() ?? string.Empty;
        var categories = _serviceCategories
            .Select(category => category.CategoryName)
            .Where(category => string.IsNullOrWhiteSpace(search) || category.Contains(search, StringComparison.OrdinalIgnoreCase))
            .ToList();
        ServiceCategoryPicker.ItemsSource = categories;
        if (categories.Count == 1 && string.Equals(categories[0], search, StringComparison.OrdinalIgnoreCase))
        {
            ServiceCategoryPicker.SelectedItem = categories[0];
        }
        else if (ServiceCategoryPicker.SelectedItem is string selected && !categories.Contains(selected, StringComparer.OrdinalIgnoreCase))
        {
            ServiceCategoryPicker.SelectedItem = null;
        }
    }

    private void SetSelectedServiceCategory(string categoryName)
    {
        _updatingServiceCategorySearch = true;
        ServiceCategorySearchBar.Text = categoryName;
        _updatingServiceCategorySearch = false;
        RefreshServiceCategoryPicker();
        ServiceCategoryPicker.SelectedItem = _serviceCategories
            .Select(category => category.CategoryName)
            .FirstOrDefault(name => string.Equals(name, categoryName, StringComparison.OrdinalIgnoreCase));
    }

    private void UpdateStepPresentation()
    {
        IdentityStepFrame.IsVisible = _currentStep == SetupStep.Identity;
        ProductsStepFrame.IsVisible = _currentStep == SetupStep.Products;
        ServicesStepFrame.IsVisible = _currentStep == SetupStep.Services;
        ReviewStepFrame.IsVisible = _currentStep == SetupStep.Review;

        BackStepButton.IsVisible = _currentStep != SetupStep.Identity;
        NextStepButton.IsVisible = _currentStep != SetupStep.Review;
        ContinueButton.IsVisible = _currentStep == SetupStep.Review;
        ContinueButton.IsEnabled = _status.IsComplete;
        ContinueButton.Opacity = _status.IsComplete ? 1d : 0.55d;

        (StepTitleLabel.Text, SetupHeaderSubtitle.Text) = _currentStep switch
        {
            SetupStep.Identity => ("Step 1 of 4 - Shop identity", "Add public shop photos and description."),
            SetupStep.Products => ("Step 2 of 4 - Products", "Add at least one customer-visible product."),
            SetupStep.Services => ("Step 3 of 4 - Services", "Add at least one bookable service."),
            _ => ("Step 4 of 4 - Review", "Check everything before opening the dashboard.")
        };

        ReviewIdentityLabel.Text = _status.HasCoverPhoto && _status.HasProfilePicture && _status.HasDescription
            ? "Shop identity is complete."
            : "Shop identity still needs a cover photo, profile picture, and description.";
        ReviewProductCountLabel.Text = SetupProducts.Count == 1
            ? "1 product is ready for customers."
            : $"{SetupProducts.Count} products are ready for customers.";
        ReviewServiceCountLabel.Text = SetupServices.Count == 1
            ? "1 service is ready for booking."
            : $"{SetupServices.Count} services are ready for booking.";

        ApplyStepIndicator(IdentityStepIndicator, SetupStep.Identity, _status.HasCoverPhoto && _status.HasProfilePicture && _status.HasDescription);
        ApplyStepIndicator(ProductsStepIndicator, SetupStep.Products, _status.HasProducts);
        ApplyStepIndicator(ServicesStepIndicator, SetupStep.Services, _status.HasServices);
        ApplyStepIndicator(ReviewStepIndicator, SetupStep.Review, _status.IsComplete);
    }

    private void ApplyStepIndicator(Label label, SetupStep step, bool complete)
    {
        label.TextColor = step == _currentStep
            ? Color.FromArgb("#FF6B2C")
            : complete
                ? Color.FromArgb("#147A3D")
                : Color.FromArgb("#6B7280");
        label.FontAttributes = step == _currentStep ? FontAttributes.Bold : FontAttributes.None;
    }

    private static SetupStep FirstIncompleteStep(ShopSetupStatus status)
    {
        if (!status.HasCoverPhoto || !status.HasProfilePicture || !status.HasDescription)
        {
            return SetupStep.Identity;
        }

        if (!status.HasProducts)
        {
            return SetupStep.Products;
        }

        if (!status.HasServices)
        {
            return SetupStep.Services;
        }

        return SetupStep.Review;
    }

    private enum SetupStep
    {
        Identity,
        Products,
        Services,
        Review
    }
}

public sealed class SetupServiceItem
{
    private SetupServiceItem(string serviceName, string categoryName, decimal price, int minutes)
    {
        ServiceName = serviceName;
        CategoryName = categoryName;
        Price = price;
        Minutes = minutes;
    }

    public string ServiceName { get; }
    public string CategoryName { get; }
    public decimal Price { get; }
    public int Minutes { get; }
    public string PriceText => $"PHP {Price:N2}";
    public string DurationText => $"{Minutes} min";

    public static SetupServiceItem FromApi(AdminShopService service)
    {
        return new SetupServiceItem(
            service.ServiceName,
            service.CategoryName,
            service.BasePrice,
            service.EstimatedMinutes);
    }
}
