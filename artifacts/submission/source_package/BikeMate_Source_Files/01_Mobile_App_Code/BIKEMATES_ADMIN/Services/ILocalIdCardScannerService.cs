namespace BIKEMATES_ADMIN.Services;

public interface ILocalIdCardScannerService
{
    Task<LocalIdCardScanResult> ScanAsync(string title = "Scan your ID card", CancellationToken cancellationToken = default);
}
