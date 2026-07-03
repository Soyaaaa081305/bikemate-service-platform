namespace BikeMate.Services;

public enum LocalIdCardReadabilityStatus
{
    Readable,
    NeedsManualReview,
    Unreadable
}

public sealed record LocalIdCardScanResult
{
    public bool IsSuccessful { get; init; }
    public LocalIdCardReadabilityStatus ReadabilityStatus { get; init; } = LocalIdCardReadabilityStatus.NeedsManualReview;
    public string ExtractedFullText { get; init; } = string.Empty;
    public string? PossibleFullName { get; init; }
    public string? PossibleIdNumber { get; init; }
    public string? PossibleBirthdate { get; init; }
    public string? PossibleAddress { get; init; }
    public string? PossibleExpirationDate { get; init; }
    public string? LocalProcessedImagePath { get; init; }
    public string? LocalTemporaryImagePath { get; init; }
    public string? OriginalFileName { get; init; }
    public string? ErrorMessage { get; init; }
    public bool WasCancelled { get; init; }
    public bool WasUploadedToServer { get; init; }
}
