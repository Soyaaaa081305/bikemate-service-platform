using BikeMate.Api.Services;
using BikeMate.Core.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeMate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class FilesController(IFileStorageService fileStorageService) : ControllerBase
{
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<UploadedFileDto>> Upload([FromForm] IFormFile? file, [FromForm] string? folder, CancellationToken cancellationToken)
    {
        return await SaveUploadAsync(file, folder ?? "general", cancellationToken);
    }

    [HttpPost("onboarding-upload")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<UploadedFileDto>> UploadOnboarding([FromForm] IFormFile? file, [FromForm] string? folder, CancellationToken cancellationToken)
    {
        var requestedFolder = string.IsNullOrWhiteSpace(folder) ? "shop-applications" : folder.Trim();
        var allowedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "shop-applications",
            "shop-owner-ids",
            "shop-business-permits",
            "shop-images"
        };

        if (!allowedFolders.Contains(requestedFolder))
        {
            return BadRequest(new { error = "That upload folder is not allowed for account applications." });
        }

        return await SaveUploadAsync(file, requestedFolder, cancellationToken);
    }

    private async Task<ActionResult<UploadedFileDto>> SaveUploadAsync(IFormFile? file, string folder, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest(new { error = "Select a file before uploading." });
        }

        try
        {
            var uploaded = await fileStorageService.SaveFileAsync(file, folder ?? "general", cancellationToken);
            return Ok(uploaded);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
