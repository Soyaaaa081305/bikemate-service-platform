using System.Text.Json;
using BikeMate.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/maps")]
[Authorize]
public sealed class MapsController(IHttpClientFactory httpClientFactory, IConfiguration configuration) : ControllerBase
{
    [HttpGet("geocode")]
    public async Task<ActionResult<MapPointDto>> Geocode([FromQuery] string address, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return BadRequest(new { error = "Address is required." });
        }

        using var document = await GetGoogleJsonAsync(
            $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={Uri.EscapeDataString(ApiKey())}",
            cancellationToken);
        var result = FirstResultOrError(document.RootElement);
        if (result.Error is not null)
        {
            return result.Error;
        }

        var dto = ToPointDto(result.Value);
        return dto is null ? NotFound(new { error = "No map result found." }) : Ok(dto);
    }

    [HttpGet("reverse-geocode")]
    public async Task<ActionResult<MapPointDto>> ReverseGeocode([FromQuery] decimal latitude, [FromQuery] decimal longitude, CancellationToken cancellationToken)
    {
        using var document = await GetGoogleJsonAsync(
            $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}&key={Uri.EscapeDataString(ApiKey())}",
            cancellationToken);
        var result = FirstResultOrError(document.RootElement);
        if (result.Error is not null)
        {
            return result.Error;
        }

        var dto = ToPointDto(result.Value);
        return dto is null ? NotFound(new { error = "No map result found." }) : Ok(dto);
    }

    [HttpGet("directions")]
    public async Task<ActionResult<MapDirectionsDto>> Directions(
        [FromQuery] decimal originLat,
        [FromQuery] decimal originLng,
        [FromQuery] decimal destinationLat,
        [FromQuery] decimal destinationLng,
        CancellationToken cancellationToken)
    {
        var origin = $"{originLat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{originLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        var destination = $"{destinationLat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{destinationLng.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        using var document = await GetGoogleJsonAsync(
            $"https://maps.googleapis.com/maps/api/directions/json?origin={Uri.EscapeDataString(origin)}&destination={Uri.EscapeDataString(destination)}&mode=driving&key={Uri.EscapeDataString(ApiKey())}",
            cancellationToken);

        var root = document.RootElement;
        var googleError = GoogleError(root);
        if (googleError is not null)
        {
            return googleError;
        }

        if (!root.TryGetProperty("routes", out var routes) ||
            routes.ValueKind != JsonValueKind.Array ||
            routes.GetArrayLength() == 0)
        {
            return NotFound(new { error = "No directions found." });
        }

        var route = routes[0];
        var leg = route.GetProperty("legs")[0];
        var distance = leg.GetProperty("distance");
        var duration = leg.GetProperty("duration");
        var polyline = route.TryGetProperty("overview_polyline", out var overview) &&
            overview.TryGetProperty("points", out var points)
                ? points.GetString()
                : null;

        return Ok(new MapDirectionsDto(
            distance.GetProperty("text").GetString() ?? "",
            duration.GetProperty("text").GetString() ?? "",
            distance.TryGetProperty("value", out var distanceValue) ? distanceValue.GetInt32() : null,
            duration.TryGetProperty("value", out var durationValue) ? durationValue.GetInt32() : null,
            polyline));
    }

    [HttpGet("places/autocomplete")]
    public async Task<ActionResult<IReadOnlyCollection<MapPlaceSuggestionDto>>> Autocomplete([FromQuery] string input, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Ok(Array.Empty<MapPlaceSuggestionDto>());
        }

        using var document = await GetGoogleJsonAsync(
            $"https://maps.googleapis.com/maps/api/place/autocomplete/json?input={Uri.EscapeDataString(input)}&components=country:ph&key={Uri.EscapeDataString(ApiKey())}",
            cancellationToken);
        var googleError = GoogleError(document.RootElement);
        if (googleError is not null)
        {
            return googleError;
        }

        var predictions = document.RootElement.TryGetProperty("predictions", out var rows) && rows.ValueKind == JsonValueKind.Array
            ? rows.EnumerateArray()
                .Select(x => new MapPlaceSuggestionDto(
                    x.TryGetProperty("place_id", out var id) ? id.GetString() ?? "" : "",
                    x.TryGetProperty("description", out var description) ? description.GetString() ?? "" : ""))
                .Where(x => !string.IsNullOrWhiteSpace(x.PlaceId) && !string.IsNullOrWhiteSpace(x.Description))
                .ToArray()
            : [];
        return Ok(predictions);
    }

    [HttpGet("places/details")]
    public async Task<ActionResult<MapPointDto>> PlaceDetails([FromQuery] string placeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(placeId))
        {
            return BadRequest(new { error = "Place ID is required." });
        }

        using var document = await GetGoogleJsonAsync(
            $"https://maps.googleapis.com/maps/api/place/details/json?place_id={Uri.EscapeDataString(placeId)}&fields=place_id,formatted_address,geometry&key={Uri.EscapeDataString(ApiKey())}",
            cancellationToken);
        var googleError = GoogleError(document.RootElement);
        if (googleError is not null)
        {
            return googleError;
        }

        if (!document.RootElement.TryGetProperty("result", out var result))
        {
            return NotFound(new { error = "Place details not found." });
        }

        var dto = ToPointDto(result);
        return dto is null ? NotFound(new { error = "Place details not found." }) : Ok(dto);
    }

    private async Task<JsonDocument> GetGoogleJsonAsync(string url, CancellationToken cancellationToken)
    {
        _ = ApiKey();
        var http = httpClientFactory.CreateClient();
        await using var stream = await http.GetStreamAsync(url, cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private string ApiKey()
    {
        var apiKey = configuration["GoogleMaps:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) ||
            apiKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Google Maps is not configured. Set GoogleMaps:ApiKey on the API server.");
        }

        return apiKey;
    }

    private static (JsonElement Value, ActionResult? Error) FirstResultOrError(JsonElement root)
    {
        var googleError = GoogleError(root);
        if (googleError is not null)
        {
            return (default, googleError);
        }

        if (!root.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array ||
            results.GetArrayLength() == 0)
        {
            return (default, new NotFoundObjectResult(new { error = "No map result found." }));
        }

        return (results[0], null);
    }

    private static MapPointDto? ToPointDto(JsonElement result)
    {
        if (!result.TryGetProperty("geometry", out var geometry) ||
            !geometry.TryGetProperty("location", out var location))
        {
            return null;
        }

        var address = result.TryGetProperty("formatted_address", out var formatted)
            ? formatted.GetString() ?? ""
            : "";
        var placeId = result.TryGetProperty("place_id", out var id)
            ? id.GetString()
            : null;

        return new MapPointDto(
            location.GetProperty("lat").GetDecimal(),
            location.GetProperty("lng").GetDecimal(),
            address,
            placeId);
    }

    private static ActionResult? GoogleError(JsonElement root)
    {
        var status = root.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;
        if (string.IsNullOrWhiteSpace(status) || string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(status, "ZERO_RESULTS", StringComparison.OrdinalIgnoreCase))
        {
            return new NotFoundObjectResult(new { error = "No map result found." });
        }

        var message = root.TryGetProperty("error_message", out var errorElement)
            ? errorElement.GetString()
            : $"Google Maps returned {status}.";
        return new BadRequestObjectResult(new { error = message, status });
    }
}
