using BikeMate.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;

namespace BikeMate.Tests.Services;

public sealed class FileStorageServiceTests
{
    private static FileStorageService CreateService(
        Dictionary<string, string?>? configValues = null,
        string? webRootPath = null,
        string? contentRootPath = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues ?? new Dictionary<string, string?>())
            .Build();

        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.WebRootPath).Returns(webRootPath ?? "");
        env.Setup(e => e.ContentRootPath).Returns(contentRootPath ?? "/tmp/bikemate-test");

        var httpContextAccessor = new Mock<IHttpContextAccessor>();

        return new FileStorageService(config, env.Object, httpContextAccessor.Object);
    }

    [Fact]
    public async Task SaveFileAsync_ThrowsForEmptyFile()
    {
        var sut = CreateService();
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(0);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SaveFileAsync(file.Object, "test", CancellationToken.None));
        Assert.Contains("Select a file", ex.Message);
    }

    [Fact]
    public async Task SaveFileAsync_ThrowsForOversizedFile()
    {
        var sut = CreateService(new Dictionary<string, string?>
        {
            ["Storage:MaxFileBytes"] = "1024"
        });
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(2048);
        file.Setup(f => f.FileName).Returns("big.jpg");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SaveFileAsync(file.Object, "test", CancellationToken.None));
        Assert.Contains("too large", ex.Message);
    }

    [Fact]
    public async Task SaveFileAsync_ThrowsForDisallowedContentType()
    {
        var sut = CreateService();
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(100);
        file.Setup(f => f.FileName).Returns("malware.exe");
        file.Setup(f => f.ContentType).Returns("application/x-msdownload");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SaveFileAsync(file.Object, "test", CancellationToken.None));
        Assert.Contains("accepts", ex.Message);
    }

    [Fact]
    public async Task SaveFileAsync_ThrowsForDisallowedExtension()
    {
        var sut = CreateService();
        var file = new Mock<IFormFile>();
        file.Setup(f => f.Length).Returns(100);
        file.Setup(f => f.FileName).Returns("script.sh");
        file.Setup(f => f.ContentType).Returns("text/plain");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SaveFileAsync(file.Object, "test", CancellationToken.None));
        Assert.Contains("accepts", ex.Message);
    }

    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.png", "image/png")]
    [InlineData("photo.webp", "image/webp")]
    [InlineData("document.pdf", "application/pdf")]
    [InlineData("video.mp4", "video/mp4")]
    public async Task SaveFileAsync_AcceptsAllowedFormats(string fileName, string contentType)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"bikemate-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var sut = CreateService(contentRootPath: tempDir);
            var stream = new MemoryStream(new byte[10]);
            var file = new Mock<IFormFile>();
            file.Setup(f => f.Length).Returns(10);
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.ContentType).Returns(contentType);
            file.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await sut.SaveFileAsync(file.Object, "uploads", CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(fileName, result.FileName);
            Assert.Equal(contentType, result.ContentType);
            Assert.Equal(10, result.SizeBytes);
            Assert.Contains("uploads", result.Url);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SaveFileAsync_SanitizesFolderName()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"bikemate-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var sut = CreateService(
                new Dictionary<string, string?>
                {
                    ["Storage:BaseUrl"] = "https://cdn.bikemate.app/uploads"
                },
                contentRootPath: tempDir);
            var file = new Mock<IFormFile>();
            file.Setup(f => f.Length).Returns(10);
            file.Setup(f => f.FileName).Returns("file.jpg");
            file.Setup(f => f.ContentType).Returns("image/jpeg");
            file.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await sut.SaveFileAsync(file.Object, "my folder/../../etc", CancellationToken.None);

            Assert.DoesNotContain("..", result.Url);
            Assert.DoesNotContain("/etc", result.Url);
            Assert.Contains("my-folder", result.Url);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
