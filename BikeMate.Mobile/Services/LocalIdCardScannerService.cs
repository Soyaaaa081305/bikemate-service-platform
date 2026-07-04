namespace BikeMate.Services;

public sealed class LocalIdCardScannerService
{
    public async Task<LocalIdCardScanResult> ScanAsync(string title = "Scan your ID card", CancellationToken cancellationToken = default)
    {
        try
        {
            var scanPath = await MainActivity.ScanDocumentAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(scanPath))
            {
                return Cancelled();
            }

            if (!File.Exists(scanPath))
            {
                return Failed("BikeMate could not find the scanned document image. Please scan again.");
            }

            var readability = EstimateReadability(scanPath);
            if (readability == LocalIdCardReadabilityStatus.Unreadable)
            {
                TryDelete(scanPath);
                return Failed("The scanned document image was too small or unreadable. Please scan the ID again in good lighting.");
            }

            return new LocalIdCardScanResult
            {
                IsSuccessful = true,
                ReadabilityStatus = readability,
                LocalProcessedImagePath = scanPath,
                LocalTemporaryImagePath = scanPath,
                OriginalFileName = Path.GetFileName(scanPath),
                WasUploadedToServer = false
            };
        }
        catch (OperationCanceledException)
        {
            return Cancelled();
        }
        catch (Exception ex)
        {
            return Failed(ex.Message);
        }
    }

    private static LocalIdCardReadabilityStatus EstimateReadability(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists || file.Length < 40 * 1024)
        {
            return LocalIdCardReadabilityStatus.Unreadable;
        }

        return file.Length >= 140 * 1024
            ? LocalIdCardReadabilityStatus.Readable
            : LocalIdCardReadabilityStatus.NeedsManualReview;
    }

    private static LocalIdCardScanResult Cancelled()
    {
        return new LocalIdCardScanResult
        {
            IsSuccessful = false,
            ReadabilityStatus = LocalIdCardReadabilityStatus.Unreadable,
            ErrorMessage = "ID scan was cancelled.",
            WasCancelled = true,
            WasUploadedToServer = false
        };
    }

    private static LocalIdCardScanResult Failed(string message)
    {
        return new LocalIdCardScanResult
        {
            IsSuccessful = false,
            ReadabilityStatus = LocalIdCardReadabilityStatus.Unreadable,
            ErrorMessage = message,
            WasUploadedToServer = false
        };
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}
