namespace BikeMate.Services;

public interface ILocalIdCardScannerService
{
    Task<LocalIdCardScanResult> ScanAsync(string title = "Scan your ID card", CancellationToken cancellationToken = default);
}
