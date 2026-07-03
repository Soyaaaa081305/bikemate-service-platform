using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace BikeMate.Services;

internal sealed record EmergencyLocationSnapshot(
    decimal Latitude,
    decimal Longitude,
    string Address,
    bool IsGpsLocation,
    string? ErrorMessage);

internal static class LocationService
{
    public static async Task<EmergencyLocationSnapshot?> GetCurrentLocationAsync(Page page)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                return new EmergencyLocationSnapshot(0m, 0m, "", false, "Location permission was denied.");
            }

            Location? location = null;
            try
            {
                location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15)));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Real-time geolocation failed, falling back to last known: {ex}");
                location = await Geolocation.Default.GetLastKnownLocationAsync();
            }

            if (location is null)
            {
                return new EmergencyLocationSnapshot(0m, 0m, "", false, "GPS did not return a location.");
            }

            var address = await ReverseGeocodeAsync(location);
            return new EmergencyLocationSnapshot(
                (decimal)location.Latitude,
                (decimal)location.Longitude,
                string.IsNullOrWhiteSpace(address) ? $"Lat {location.Latitude:0.000000}, Lng {location.Longitude:0.000000}" : address,
                true,
                null);
        }
        catch (Exception ex)
        {
            await page.DisplayAlertAsync("Location unavailable", ex.Message, "OK");
            return new EmergencyLocationSnapshot(0m, 0m, "", false, ex.Message);
        }
    }

    public static async Task<string?> ReverseGeocodeAsync(Location location)
    {
        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(location.Latitude, location.Longitude);
            var place = placemarks?.FirstOrDefault();
            if (place is null)
            {
                return null;
            }

            var street = string.Join(" ", new[] { place.SubThoroughfare, place.Thoroughfare }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var locality = string.Join(", ", new[] { place.Locality, place.AdminArea }.Where(x => !string.IsNullOrWhiteSpace(x)));
            return string.Join(", ", new[] { street, locality, place.CountryName }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Reverse geocoding failed: {ex}");
            return null;
        }
    }
}
