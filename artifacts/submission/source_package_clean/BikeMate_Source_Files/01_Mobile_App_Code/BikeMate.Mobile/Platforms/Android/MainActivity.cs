using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using BikeMate.Helpers;
using BikeMate.Services;
using Google.MLKit.Vision.Documentscanner;
using Java.Lang;

namespace BikeMate;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTop)]
[IntentFilter(new[] { Intent.ActionView },
              Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
              DataScheme = "bikemate",
              DataHost = "payment-success")]
[IntentFilter(new[] { Intent.ActionView },
              Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
              DataScheme = "bikemate",
              DataHost = "payment-cancelled")]
public class MainActivity : MauiAppCompatActivity
{
    private const int DocumentScannerRequestCode = 7421;
    private static TaskCompletionSource<string?>? _documentScanCompletion;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleDeepLink(Intent);
    }

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

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
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

            var outputPath = CopyProcessedScanToCache(imageUri);
            completion.TrySetResult(outputPath);
        }
        catch (System.Exception ex)
        {
            completion.TrySetException(ex);
        }
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        HandleDeepLink(intent);
    }

    private static void HandleDeepLink(Intent? intent)
    {
        if (PaymentReturnService.CaptureReturn(intent?.DataString))
        {
            intent?.SetData(null);
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

                activity.StartIntentSenderForResult(
                    intentSender,
                    DocumentScannerRequestCode,
                    null,
                    0,
                    0,
                    0);
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
