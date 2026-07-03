using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using BikeMate.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/geography")]
[AllowAnonymous]
public sealed class GeographyController(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IConfiguration configuration,
    ILogger<GeographyController> logger) : ControllerBase
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("regions")]
    public async Task<ActionResult<IReadOnlyList<PhilippineRegionDto>>> Regions(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await GetRegionsAsync(cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "The Philippine geography provider could not return regions.");
            return Ok(FallbackRegions);
        }
    }

    [HttpGet("regions/{regionCode}/localities")]
    public async Task<ActionResult<IReadOnlyList<PhilippineLocalityDto>>> Localities(
        string regionCode,
        CancellationToken cancellationToken)
    {
        if (!IsPsgcCode(regionCode))
        {
            return BadRequest(new { error = "A valid 10-digit PSGC region code is required." });
        }

        try
        {
            return Ok(await GetLocalitiesAsync(regionCode, cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "The Philippine geography provider could not return localities for {RegionCode}.", regionCode);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Cities and municipalities are temporarily unavailable. Please try again."
            });
        }
    }

    [HttpGet("localities/{localityCode}/barangays")]
    public async Task<ActionResult<IReadOnlyList<PhilippineBarangayDto>>> Barangays(
        string localityCode,
        CancellationToken cancellationToken)
    {
        if (!IsPsgcCode(localityCode))
        {
            return BadRequest(new { error = "A valid 10-digit PSGC city or municipality code is required." });
        }

        try
        {
            return Ok(await GetBarangaysAsync(localityCode, cancellationToken));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "The Philippine geography provider could not return barangays for {LocalityCode}.", localityCode);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Barangays are temporarily unavailable. Please try again."
            });
        }
    }

    [HttpGet("resolve")]
    public async Task<ActionResult<PhilippineLocationMatchDto>> Resolve(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "A Philippine address or locality is required." });
        }

        try
        {
            var normalizedQuery = NormalizeForMatch(query);
            var regions = await GetRegionsAsync(cancellationToken);
            var localityGroups = await Task.WhenAll(
                regions.Select(async region => new
                {
                    Region = region,
                    Localities = await GetLocalitiesAsync(region.Code, cancellationToken)
                }));

            var match = localityGroups
                .SelectMany(group => group.Localities.Select(locality => new
                {
                    group.Region,
                    Locality = locality,
                    Score = MatchScore(normalizedQuery, locality.Name, locality.Province, group.Region.Name)
                }))
                .Where(candidate => candidate.Score > 0)
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Locality.Name)
                .FirstOrDefault();

            return match is null
                ? NotFound(new { error = "No matching Philippine city or municipality was found." })
                : Ok(new PhilippineLocationMatchDto(match.Region, match.Locality));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "The Philippine geography provider could not resolve {Query}.", query);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Philippine location matching is temporarily unavailable. Please try again."
            });
        }
    }

    private async Task<IReadOnlyList<PhilippineRegionDto>> GetRegionsAsync(CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync("psgc-regions", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var rows = await GetProviderAsync<IReadOnlyList<PsgcRegionRow>>("region", cancellationToken);
            return (IReadOnlyList<PhilippineRegionDto>)rows
                .Where(row => IsPsgcCode(row.Code) && !string.IsNullOrWhiteSpace(row.Name))
                .Select(row => new PhilippineRegionDto(row.Code, RepairEncoding(row.Name).Trim()))
                .DistinctBy(row => row.Code)
                .OrderBy(row => row.Code)
                .ToArray();
        }) ?? [];
    }

    private async Task<IReadOnlyList<PhilippineLocalityDto>> GetLocalitiesAsync(
        string regionCode,
        CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync($"psgc-localities-{regionCode}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var rows = await GetProviderAsync<IReadOnlyList<PsgcLocalityRow>>(
                $"municipal-city?id={Uri.EscapeDataString(regionCode)}",
                cancellationToken);
            var provinces = await GetProvincesAsync(regionCode, cancellationToken);

            return (IReadOnlyList<PhilippineLocalityDto>)rows
                .Where(row =>
                    IsPsgcCode(row.Code) &&
                    (string.Equals(row.Type, "City", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(row.Type, "Mun", StringComparison.OrdinalIgnoreCase)))
                .Select(row => new PhilippineLocalityDto(
                    row.Code,
                    RepairEncoding(row.Name).Trim(),
                    string.Equals(row.Type, "City", StringComparison.OrdinalIgnoreCase) ? "City" : "Municipality",
                    regionCode,
                    ProvinceFor(row.Code, provinces)))
                .DistinctBy(row => row.Code)
                .OrderBy(row => row.Name)
                .ToArray();
        }) ?? [];
    }

    private async Task<IReadOnlyList<PsgcProvinceRow>> GetProvincesAsync(
        string regionCode,
        CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync($"psgc-provinces-{regionCode}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var rows = await GetProviderAsync<IReadOnlyList<PsgcProvinceRow>>(
                $"province?id={Uri.EscapeDataString(regionCode)}",
                cancellationToken);
            return (IReadOnlyList<PsgcProvinceRow>)rows
                .Where(row => IsPsgcCode(row.Code) && !string.IsNullOrWhiteSpace(row.Name))
                .Select(row => row with { Name = RepairEncoding(row.Name).Trim() })
                .ToArray();
        }) ?? [];
    }

    private async Task<IReadOnlyList<PhilippineBarangayDto>> GetBarangaysAsync(
        string localityCode,
        CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync($"psgc-barangays-{localityCode}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var rows = await GetProviderAsync<IReadOnlyList<PsgcBarangayRow>>(
                $"barangay?id={Uri.EscapeDataString(localityCode)}",
                cancellationToken);
            return (IReadOnlyList<PhilippineBarangayDto>)rows
                .Where(row =>
                    IsPsgcCode(row.Code) &&
                    string.Equals(row.Type, "Bgy", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(row.Name))
                .Select(row => new PhilippineBarangayDto(
                    row.Code,
                    RepairEncoding(row.Name).Trim(),
                    localityCode))
                .DistinctBy(row => row.Code)
                .OrderBy(row => row.Name)
                .ToArray();
        }) ?? [];
    }

    private async Task<T> GetProviderAsync<T>(string endpoint, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["PhilippineGeography:BaseUrl"] ?? "https://psgc.rootscratch.com/";
        using var http = httpClientFactory.CreateClient();
        http.BaseAddress = new Uri(baseUrl.EndsWith('/') ? baseUrl : $"{baseUrl}/");
        http.Timeout = TimeSpan.FromSeconds(20);

        using var response = await http.GetAsync(endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken)
            ?? throw new JsonException("The Philippine geography provider returned an empty response.");
    }

    private static int MatchScore(
        string normalizedQuery,
        string localityName,
        string? provinceName,
        string regionName)
    {
        var locality = NormalizeForMatch(localityName);
        if (locality.Length < 4 || !normalizedQuery.Contains(locality, StringComparison.Ordinal))
        {
            return 0;
        }

        var region = NormalizeForMatch(regionName);
        var province = NormalizeForMatch(provinceName ?? "");
        return locality.Length * 10 +
               (province.Length >= 4 && normalizedQuery.Contains(province, StringComparison.Ordinal) ? province.Length * 2 : 0) +
               (region.Length >= 4 && normalizedQuery.Contains(region, StringComparison.Ordinal) ? region.Length : 0);
    }

    private static string? ProvinceFor(string localityCode, IReadOnlyList<PsgcProvinceRow> provinces)
    {
        return provinces
            .Where(province => localityCode.StartsWith(province.Code[..5], StringComparison.Ordinal))
            .Select(province => province.Name)
            .FirstOrDefault();
    }

    private static string NormalizeForMatch(string value)
    {
        var normalized = RepairEncoding(value)
            .ToLowerInvariant()
            .Replace("city of ", "", StringComparison.Ordinal)
            .Replace("municipality of ", "", StringComparison.Ordinal)
            .Replace(" city", "", StringComparison.Ordinal);

        return new string(normalized
            .Normalize(NormalizationForm.FormD)
            .Where(character =>
                CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark &&
                char.IsLetterOrDigit(character))
            .ToArray());
    }

    private static string RepairEncoding(string value)
    {
        if (!value.Contains('Ã') && !value.Contains('Â'))
        {
            return value;
        }

        return Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(value));
    }

    private static bool IsPsgcCode(string? value)
    {
        return value is { Length: 10 } && value.All(char.IsDigit);
    }

    private static readonly PhilippineRegionDto[] FallbackRegions =
    [
        new("0100000000", "Region I (Ilocos Region)"),
        new("0200000000", "Region II (Cagayan Valley)"),
        new("0300000000", "Region III (Central Luzon)"),
        new("0400000000", "Region IV-A (CALABARZON)"),
        new("1700000000", "MIMAROPA Region"),
        new("0500000000", "Region V (Bicol Region)"),
        new("0600000000", "Region VI (Western Visayas)"),
        new("0700000000", "Region VII (Central Visayas)"),
        new("0800000000", "Region VIII (Eastern Visayas)"),
        new("0900000000", "Region IX (Zamboanga Peninsula)"),
        new("1000000000", "Region X (Northern Mindanao)"),
        new("1100000000", "Region XI (Davao Region)"),
        new("1200000000", "Region XII (SOCCSKSARGEN)"),
        new("1300000000", "National Capital Region (NCR)"),
        new("1400000000", "Cordillera Administrative Region (CAR)"),
        new("1600000000", "Caraga"),
        new("1900000000", "Bangsamoro Autonomous Region in Muslim Mindanao (BARMM)")
    ];

    private sealed record PsgcRegionRow(
        [property: JsonPropertyName("psgc_id")] string Code,
        [property: JsonPropertyName("name")] string Name);

    private sealed record PsgcLocalityRow(
        [property: JsonPropertyName("psgc_id")] string Code,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("geographic_level")] string Type);

    private sealed record PsgcBarangayRow(
        [property: JsonPropertyName("psgc_id")] string Code,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("geographic_level")] string Type);

    private sealed record PsgcProvinceRow(
        [property: JsonPropertyName("psgc_id")] string Code,
        [property: JsonPropertyName("name")] string Name);
}
