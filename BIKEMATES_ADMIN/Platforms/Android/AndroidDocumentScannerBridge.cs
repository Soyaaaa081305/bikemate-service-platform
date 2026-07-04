using Android.App;
using Android.Content;
using Google.MLKit.Vision.Documentscanner;
using Java.Lang;

namespace BIKEMATES_ADMIN;

internal static class AndroidDocumentScannerBridge
{
    private const int DocumentScannerRequestCode = 7422;
    private static TaskCompletionSource<string?>? _documentScanCompletion;

    public static Task<string?> ScanDocumentAsync(CancellationToken cancellationToken = default)
    {
        var activity = Platform.CurrentActivity as MainActivity
            ?? throw new InvalidOperationException("BikeMate could not open the document scanner because the Android activity is not ready.");

        if (_documentScanCompletion is not null)
        {
            throw new InvalidOperationException("A document scan is already in progress.");
        }

        var completion = new TaskCompletionSource<string?>();
        _documentScanCompletion = completion;
        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
            {
                _documentScanCompletion = null;
                completion.TrySetCanceled(cancellationToken);
            });
        }

        var options = new GmsDocumentScannerOptions.Builder()
            .SetGalleryImportAllowed(false)
            .SetPageLimit(1)
            .SetResultFormats(GmsDocumentScannerOptions.ResultFormatJpeg)
            .SetScannerMode(GmsDocumentScannerOptions.ScannerModeFull)
            .Build();

        var scanner = GmsDocumentScanning.GetClient(options);
        scanner.GetStartScanIntent(activity)
            .AddOnSuccessListener(new ScannerIntentSuccessListener(activity, completion))
            .AddOnFailureListener(new ScannerFailureListener(completion));
        return completion.Task;
    }

    public static void HandleActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        if (requestCode != DocumentScannerRequestCode)
        {
            return;
        }

        var completion = _documentScanCompletion;
        _documentScanCompletion = null;
        if (completion is null)
        {
            return;
        }

        if (resultCode != Result.Ok || data is null)
        {
            completion.TrySetResult(null);
            return;
        }

        try
        {
            var scanResult = GmsDocumentScanningResult.FromActivityResultIntent(data);
            var imageUri = scanResult?.Pages?.FirstOrDefault()?.ImageUri;
            if (imageUri is null)
            {
                completion.TrySetResult(null);
                return;
            }

            completion.TrySetResult(CopyProcessedScanToCache(imageUri));
        }
        catch (System.Exception ex)
        {
            completion.TrySetException(ex);
        }
    }

    private static string CopyProcessedScanToCache(Android.Net.Uri uri)
    {
        var resolver = Platform.AppContext.ContentResolver
            ?? throw new InvalidOperationException("BikeMate could not read the scanned document.");
        var targetPath = Path.Combine(FileSystem.CacheDirectory, $"mlkit-id-scan-{Guid.NewGuid():N}.jpg");
        using var input = resolver.OpenInputStream(uri)
            ?? throw new InvalidOperationException("BikeMate could not open the scanned document image.");
        using var output = File.Create(targetPath);
        input.CopyTo(output);
        return targetPath;
    }

    private sealed class ScannerIntentSuccessListener(MainActivity activity, TaskCompletionSource<string?> completion)
        : Java.Lang.Object, Android.Gms.Tasks.IOnSuccessListener
    {
        public void OnSuccess(Java.Lang.Object? result)
        {
            try
            {
                if (result is not IntentSender intentSender)
                {
                    completion.TrySetException(new InvalidOperationException("BikeMate could not open the document scanner intent."));
                    _documentScanCompletion = null;
                    return;
                }

                activity.StartIntentSenderForResult(intentSender, DocumentScannerRequestCode, null, 0, 0, 0);
            }
            catch (System.Exception ex)
            {
                _documentScanCompletion = null;
                completion.TrySetException(ex);
            }
        }
    }

    private sealed class ScannerFailureListener(TaskCompletionSource<string?> completion)
        : Java.Lang.Object, Android.Gms.Tasks.IOnFailureListener
    {
        public void OnFailure(Java.Lang.Exception exception)
        {
            _documentScanCompletion = null;
            completion.TrySetException(new InvalidOperationException(exception.Message, exception));
        }
    }
}
