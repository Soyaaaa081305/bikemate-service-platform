using System.Collections.ObjectModel;
using BIKEMATES_ADMIN.Services;
using Microsoft.Maui.Graphics;

namespace BIKEMATES_ADMIN.Pages;

public partial class Operations : ContentPage
{
    public ObservableCollection<ServiceItem> ServiceItems { get; } = new();
    public ObservableCollection<ServiceItem> VisibleServices { get; } = new();

    private readonly List<AdminServiceCategory> _categories = [];
    private ServiceItem? _selectedService;
    private int? _selectedServiceId;
    private bool _updatingCategorySearch;

    public Operations()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadServicesAsync();
    }

    private async Task LoadServicesAsync()
    {
        try
        {
            _categories.Clear();
            _categories.AddRange(await BikeMateDatabaseService.GetServiceCategoriesAsync());
            RefreshServiceCategoryPicker();

            ServiceItems.Clear();
            foreach (var service in await BikeMateDatabaseService.GetShopServicesAsync())
            {
                ServiceItems.Add(ServiceItem.FromApi(service));
            }

            RefreshServiceList();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Services", $"Unable to load services from API: {ex.Message}", "OK");
        }
    }

    private async void AddService_Clicked(object sender, EventArgs e)
    {
        if (!ValidateServiceInputs(out var request))
        {
            return;
        }

        try
        {
            await BikeMateDatabaseService.AddShopServiceAsync(request);
            await LoadServicesAsync();
            ClearEditor();
            await DisplayAlert("Service Added", "The service is now available from the shop services API.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Service Error", ex.Message, "OK");
        }
    }

    private async void UpdateService_Clicked(object sender, EventArgs e)
    {
        var selected = GetSelectedService();
        if (selected is null)
        {
            await DisplayAlert("Select Service", "Tap a service below first.", "OK");
            return;
        }

        if (!ValidateServiceInputs(out var request))
        {
            return;
        }

        try
        {
            await BikeMateDatabaseService.UpdateShopServiceAsync(selected.ServiceId, request);
            await LoadServicesAsync();
            ClearEditor();
            await DisplayAlert("Service Updated", "The selected service was updated in the API.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Service Error", ex.Message, "OK");
        }
    }

    private async void DeleteService_Clicked(object sender, EventArgs e)
    {
        var selected = GetSelectedService();
        if (selected is null)
        {
            await DisplayAlert("Select Service", "Tap a service below first.", "OK");
            return;
        }

        var confirm = await DisplayAlert("Deactivate Service", $"Deactivate {selected.Name}?", "Deactivate", "Cancel");
        if (!confirm)
        {
            return;
        }

        try
        {
            await BikeMateDatabaseService.DeleteShopServiceAsync(selected.ServiceId);
            await LoadServicesAsync();
            ClearEditor();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Service Error", ex.Message, "OK");
        }
    }

    private void ServicesCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectService(e.CurrentSelection.FirstOrDefault() as ServiceItem);
    }

    private void SelectService(ServiceItem? service)
    {
        _selectedService = service;
        _selectedServiceId = service?.ServiceId;
        if (service is null)
        {
            ServiceEditorStatusLabel.Text = "Select a service below to update it, or enter new details to add a shop service.";
            return;
        }

        SetSelectedServiceCategory(service.CategoryName);
        ServiceNameEntry.Text = service.Name;
        PriceEntry.Text = service.Price.ToString("0.##");
        DurationEntry.Text = service.EstimatedMinutes.ToString();
        DescriptionEditor.Text = service.Description;
        ActiveSwitch.IsToggled = service.IsActive;
        ServiceEditorStatusLabel.Text = $"Editing {service.Name}";
    }

    private void ServiceSearchBar_TextChanged(object sender, TextChangedEventArgs e) => RefreshServiceList();

    private void ServiceCategorySearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingCategorySearch)
        {
            return;
        }

        RefreshServiceCategoryPicker();
    }

    private async void AddServiceCategory_Clicked(object sender, EventArgs e)
    {
        var categoryName = ServiceCategorySearchBar.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            await DisplayAlert("Service Category", "Type the service category name first.", "OK");
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
            var category = await BikeMateDatabaseService.AddServiceCategoryAsync(
                new UpsertAdminServiceCategory(categoryName, description));
            await ReloadServiceCategoriesAsync(category.CategoryName);
            await DisplayAlert("Service Category", $"{category.CategoryName} is ready for shop services and customer filters.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Service Category", ex.Message, "OK");
        }
    }

    private bool ValidateServiceInputs(out UpsertAdminShopService request)
    {
        request = new UpsertAdminShopService(0, string.Empty, null, 0m, 0, true);

        var categoryName = CategoryPicker.SelectedItem?.ToString() ?? ServiceCategorySearchBar.Text?.Trim() ?? string.Empty;
        var category = _categories.FirstOrDefault(item => string.Equals(item.CategoryName, categoryName, StringComparison.OrdinalIgnoreCase));
        var name = ServiceNameEntry.Text?.Trim() ?? string.Empty;
        var description = DescriptionEditor.Text?.Trim();
        decimal.TryParse(PriceEntry.Text, out var price);
        int.TryParse(DurationEntry.Text, out var minutes);

        if (category is null || string.IsNullOrWhiteSpace(name) || price <= 0 || minutes <= 0)
        {
            _ = DisplayAlert("Missing Details", "Please select a service category, or add the typed category first. Then enter service name, base price, and estimated minutes.", "OK");
            return false;
        }

        request = new UpsertAdminShopService(category.CategoryId, name, description, price, minutes, ActiveSwitch.IsToggled);
        return true;
    }

    private void RefreshServiceList()
    {
        var search = ServiceSearchBar?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
        VisibleServices.Clear();

        foreach (var service in ServiceItems.Where(service =>
            string.IsNullOrWhiteSpace(search) ||
            service.Name.ToLowerInvariant().Contains(search) ||
            service.CategoryName.ToLowerInvariant().Contains(search) ||
            service.Description.ToLowerInvariant().Contains(search)))
        {
            VisibleServices.Add(service);
        }

        if (_selectedServiceId is int selectedId)
        {
            var selected = VisibleServices.FirstOrDefault(service => service.ServiceId == selectedId);
            ServicesCollectionView.SelectedItem = selected;
            _selectedService = selected ?? ServiceItems.FirstOrDefault(service => service.ServiceId == selectedId);
        }
    }

    private void ClearEditor()
    {
        _selectedService = null;
        _selectedServiceId = null;
        CategoryPicker.SelectedItem = null;
        ServiceCategorySearchBar.Text = string.Empty;
        RefreshServiceCategoryPicker();
        ServiceNameEntry.Text = string.Empty;
        PriceEntry.Text = string.Empty;
        DurationEntry.Text = string.Empty;
        DescriptionEditor.Text = string.Empty;
        ActiveSwitch.IsToggled = true;
        ServicesCollectionView.SelectedItem = null;
        ServiceEditorStatusLabel.Text = "Select a service below to update it, or enter new details to add a shop service.";
    }

    private ServiceItem? GetSelectedService()
    {
        if (_selectedService is not null)
        {
            return _selectedService;
        }

        return _selectedServiceId is int id
            ? ServiceItems.FirstOrDefault(service => service.ServiceId == id)
            : null;
    }

    private async Task ReloadServiceCategoriesAsync(string? selectedCategory = null)
    {
        _categories.Clear();
        _categories.AddRange(await BikeMateDatabaseService.GetServiceCategoriesAsync());
        RefreshServiceCategoryPicker();
        if (!string.IsNullOrWhiteSpace(selectedCategory))
        {
            SetSelectedServiceCategory(selectedCategory);
        }
    }

    private void RefreshServiceCategoryPicker()
    {
        var search = ServiceCategorySearchBar?.Text?.Trim() ?? string.Empty;
        var categories = _categories
            .Select(category => category.CategoryName)
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

    private void SetSelectedServiceCategory(string categoryName)
    {
        _updatingCategorySearch = true;
        ServiceCategorySearchBar.Text = categoryName;
        _updatingCategorySearch = false;
        RefreshServiceCategoryPicker();
        CategoryPicker.SelectedItem = _categories
            .Select(category => category.CategoryName)
            .FirstOrDefault(name => string.Equals(name, categoryName, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record ServiceItem(
    int ServiceId,
    int CategoryId,
    string CategoryName,
    string Name,
    string Description,
    decimal Price,
    int EstimatedMinutes,
    bool IsActive)
{
    public string PriceText => $"PHP {Price:N2}";
    public string DurationText => $"{EstimatedMinutes} min";
    public string StatusText => IsActive ? "ACTIVE" : "INACTIVE";
    public Color StatusColor => IsActive ? Color.FromArgb("#16A34A") : Color.FromArgb("#6B7280");

    public static ServiceItem FromApi(AdminShopService service)
    {
        return new ServiceItem(
            service.ShopServiceId,
            service.CategoryId,
            service.CategoryName,
            service.ServiceName,
            service.ServiceDescription ?? string.Empty,
            service.BasePrice,
            service.EstimatedMinutes,
            service.IsActive);
    }
}
