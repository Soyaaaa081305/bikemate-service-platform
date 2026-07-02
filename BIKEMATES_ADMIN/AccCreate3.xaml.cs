using BIKEMATES_ADMIN.Services;
using System.Text.RegularExpressions;

namespace BIKEMATES_ADMIN.Pages.Account;

public partial class AccCreate3 : ContentPage
{
    private readonly AccountCreationDraft _draft;
    private IReadOnlyList<PhilippineRegion> _regions = [];
    private IReadOnlyList<PhilippineLocality> _localities = [];
    private IReadOnlyList<PhilippineBarangay> _barangays = [];
    private bool _loadingLocations;
    private bool _updatingPickers;
    private bool _restoredDraft;

    public AccCreate3()
        : this(new AccountCreationDraft())
    {
    }

    public AccCreate3(AccountCreationDraft draft)
    {
        _draft = draft;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadRegionsAsync();
        await RestoreDraftAsync();
    }

    protected override void OnDisappearing()
    {
        SaveDraft();
        base.OnDisappearing();
    }

    private async Task LoadRegionsAsync()
    {
        if (_regions.Count > 0 || _loadingLocations)
        {
            return;
        }

        _loadingLocations = true;
        LocationStatusLabel.Text = "Loading Philippine regions...";
        try
        {
            _regions = await BikeMateDatabaseService.GetPhilippineRegionsAsync();
            SetPickerItems(RegionPicker, _regions.Select(x => x.Name));
            LocationStatusLabel.Text = _regions.Count == 0 ? "No regions were returned. Please try again." : "";
        }
        catch (Exception ex)
        {
            LocationStatusLabel.Text = $"Unable to load Philippine locations. {ex.Message}";
        }
        finally
        {
            _loadingLocations = false;
        }
    }

    private async void OnRegionChanged(object sender, EventArgs e)
    {
        if (_updatingPickers)
        {
            return;
        }

        var region = SelectedRegion();
        if (region is null)
        {
            return;
        }

        _loadingLocations = true;
        LocationStatusLabel.Text = "Loading cities and municipalities...";
        ProvinceLabel.Text = "";
        _localities = [];
        _barangays = [];
        ResetPicker(CityPicker, "Select city or municipality");
        ResetPicker(BarangayPicker, "Select barangay");
        try
        {
            _localities = await BikeMateDatabaseService.GetPhilippineLocalitiesAsync(region.Code);
            SetPickerItems(CityPicker, _localities.Select(LocalityDisplayName));
            LocationStatusLabel.Text = _localities.Count == 0 ? "No cities or municipalities were returned." : "";
        }
        catch (Exception ex)
        {
            LocationStatusLabel.Text = $"Unable to load cities or municipalities. {ex.Message}";
        }
        finally
        {
            _loadingLocations = false;
        }
    }

    private async void OnCityChanged(object sender, EventArgs e)
    {
        if (_updatingPickers)
        {
            return;
        }

        var locality = SelectedLocality();
        if (locality is null)
        {
            return;
        }

        ProvinceLabel.Text = string.IsNullOrWhiteSpace(locality.Province) ? "" : $"Province: {locality.Province}";
        _loadingLocations = true;
        LocationStatusLabel.Text = "Loading barangays...";
        _barangays = [];
        ResetPicker(BarangayPicker, "Select barangay");
        try
        {
            _barangays = await BikeMateDatabaseService.GetPhilippineBarangaysAsync(locality.Code);
            SetPickerItems(BarangayPicker, _barangays.Select(x => x.Name));
            LocationStatusLabel.Text = _barangays.Count == 0 ? "No barangays were returned." : "";
        }
        catch (Exception ex)
        {
            LocationStatusLabel.Text = $"Unable to load barangays. {ex.Message}";
        }
        finally
        {
            _loadingLocations = false;
        }
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        SaveDraft();
        var region = SelectedRegion();
        var locality = SelectedLocality();
        var barangay = SelectedBarangay();

        if (string.IsNullOrWhiteSpace(_draft.ShopName) ||
            region is null ||
            locality is null ||
            barangay is null ||
            string.IsNullOrWhiteSpace(_draft.ShopAddress) ||
            string.IsNullOrWhiteSpace(_draft.ShopZipCode))
        {
            await DisplayAlert("Missing Information", "Please complete all required shop details.", "OK");
            return;
        }

        if (!Regex.IsMatch(_draft.ShopZipCode.Trim(), @"^\d{4}$"))
        {
            await DisplayAlert("Invalid Zip Code", "Enter a 4-digit Philippine zip code.", "OK");
            return;
        }

        await Navigation.PushAsync(new AccCreate4(_draft));
    }

    private async Task RestoreDraftAsync()
    {
        if (_restoredDraft)
        {
            return;
        }

        _restoredDraft = true;
        ShopNameEntry.Text = _draft.ShopName;
        ShopDescriptionEditor.Text = _draft.ShopDescription;
        AddressEntry.Text = _draft.ShopAddress;
        ZipCodeEntry.Text = _draft.ShopZipCode;

        if (_regions.Count == 0)
        {
            return;
        }

        var regionIndex = IndexOf(_regions, region =>
            string.Equals(region.Code, _draft.ShopRegionCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(region.Name, _draft.ShopProvince, StringComparison.OrdinalIgnoreCase));
        if (regionIndex < 0)
        {
            return;
        }

        SelectPickerIndex(RegionPicker, regionIndex);
        var region = _regions[regionIndex];
        _localities = await BikeMateDatabaseService.GetPhilippineLocalitiesAsync(region.Code);
        SetPickerItems(CityPicker, _localities.Select(LocalityDisplayName));

        var localityIndex = IndexOf(_localities, locality =>
            string.Equals(locality.Code, _draft.ShopLocalityCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(locality.Name, _draft.ShopCity, StringComparison.OrdinalIgnoreCase));
        if (localityIndex < 0)
        {
            return;
        }

        SelectPickerIndex(CityPicker, localityIndex);
        var locality = _localities[localityIndex];
        ProvinceLabel.Text = string.IsNullOrWhiteSpace(locality.Province) ? "" : $"Province: {locality.Province}";
        _barangays = await BikeMateDatabaseService.GetPhilippineBarangaysAsync(locality.Code);
        SetPickerItems(BarangayPicker, _barangays.Select(x => x.Name));

        var barangayIndex = IndexOf(_barangays, barangay =>
            string.Equals(barangay.Name, _draft.ShopBarangay, StringComparison.OrdinalIgnoreCase));
        if (barangayIndex >= 0)
        {
            SelectPickerIndex(BarangayPicker, barangayIndex);
        }
    }

    private void SaveDraft()
    {
        var region = SelectedRegion();
        var locality = SelectedLocality();
        var barangay = SelectedBarangay();
        _draft.ShopName = ShopNameEntry.Text ?? string.Empty;
        _draft.ShopDescription = ShopDescriptionEditor.Text ?? string.Empty;
        _draft.ShopRegionCode = region?.Code ?? _draft.ShopRegionCode;
        _draft.ShopLocalityCode = locality?.Code ?? _draft.ShopLocalityCode;
        _draft.ShopProvince = locality?.Province ?? region?.Name ?? _draft.ShopProvince;
        _draft.ShopCity = locality?.Name ?? _draft.ShopCity;
        _draft.ShopBarangay = barangay?.Name ?? _draft.ShopBarangay;
        _draft.ShopAddress = AddressEntry.Text ?? string.Empty;
        _draft.ShopZipCode = ZipCodeEntry.Text ?? string.Empty;
    }

    private PhilippineRegion? SelectedRegion()
    {
        return RegionPicker.SelectedIndex >= 0 && RegionPicker.SelectedIndex < _regions.Count
            ? _regions[RegionPicker.SelectedIndex]
            : null;
    }

    private PhilippineLocality? SelectedLocality()
    {
        return CityPicker.SelectedIndex >= 0 && CityPicker.SelectedIndex < _localities.Count
            ? _localities[CityPicker.SelectedIndex]
            : null;
    }

    private PhilippineBarangay? SelectedBarangay()
    {
        return BarangayPicker.SelectedIndex >= 0 && BarangayPicker.SelectedIndex < _barangays.Count
            ? _barangays[BarangayPicker.SelectedIndex]
            : null;
    }

    private static string LocalityDisplayName(PhilippineLocality locality)
    {
        return string.IsNullOrWhiteSpace(locality.Province)
            ? locality.Name
            : $"{locality.Name}, {locality.Province}";
    }

    private void SetPickerItems(Picker picker, IEnumerable<string> items)
    {
        _updatingPickers = true;
        picker.Items.Clear();
        foreach (var item in items)
        {
            picker.Items.Add(item);
        }
        picker.SelectedIndex = -1;
        _updatingPickers = false;
    }

    private void ResetPicker(Picker picker, string title)
    {
        _updatingPickers = true;
        picker.Items.Clear();
        picker.Title = title;
        picker.SelectedIndex = -1;
        _updatingPickers = false;
    }

    private void SelectPickerIndex(Picker picker, int index)
    {
        _updatingPickers = true;
        picker.SelectedIndex = index;
        _updatingPickers = false;
    }

    private static int IndexOf<T>(IReadOnlyList<T> values, Func<T, bool> predicate)
    {
        for (var index = 0; index < values.Count; index++)
        {
            if (predicate(values[index]))
            {
                return index;
            }
        }

        return -1;
    }
}
