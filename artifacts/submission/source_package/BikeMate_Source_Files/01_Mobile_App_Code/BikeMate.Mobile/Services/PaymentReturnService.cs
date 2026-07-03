using System.Diagnostics;
using BikeMate.Views.Customer;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace BikeMate.Services;

public sealed record PaymentReturnInfo(string Status, int PaymentId, int RequestId, bool FromBookingFlow, string? Url);

public static class PaymentReturnService
{
    private const string ReturnStatusKey = "payment_return_status";
    private const string ReturnUrlKey = "payment_return_url";
    private const string PaymentIdKey = "payment_return_payment_id";
    private const string RequestIdKey = "payment_return_request_id";
    private const string FromBookingFlowKey = "payment_return_from_booking_flow";

    public static void RememberCheckoutContext(int paymentId, int requestId, bool fromBookingFlow)
    {
        Preferences.Default.Set(PaymentIdKey, paymentId);
        Preferences.Default.Set(RequestIdKey, requestId);
        Preferences.Default.Set(FromBookingFlowKey, fromBookingFlow);
    }

    public static bool CaptureReturn(string? dataString)
    {
        if (string.IsNullOrWhiteSpace(dataString) ||
            !Uri.TryCreate(dataString, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, "bikemate", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var status = uri.Host.ToLowerInvariant() switch
        {
            "payment-success" => "success",
            "payment-cancelled" => "cancelled",
            _ => null
        };

        if (status is null)
        {
            return false;
        }

        Preferences.Default.Set(ReturnStatusKey, status);
        Preferences.Default.Set(ReturnUrlKey, dataString);
        _ = MainThread.InvokeOnMainThreadAsync(TryNavigateToCheckoutAsync);
        return true;
    }

    public static PaymentReturnInfo? PeekReturn()
    {
        var status = Preferences.Default.Get(ReturnStatusKey, string.Empty);
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return new PaymentReturnInfo(
            status,
            Preferences.Default.Get(PaymentIdKey, 0),
            Preferences.Default.Get(RequestIdKey, 0),
            Preferences.Default.Get(FromBookingFlowKey, false),
            Preferences.Default.Get(ReturnUrlKey, string.Empty));
    }

    public static PaymentReturnInfo? ConsumeReturn()
    {
        var info = PeekReturn();
        if (info is null)
        {
            return null;
        }

        Preferences.Default.Remove(ReturnStatusKey);
        Preferences.Default.Remove(ReturnUrlKey);
        return info;
    }

    public static string FormatBanner(PaymentReturnInfo info)
    {
        return string.Equals(info.Status, "success", StringComparison.OrdinalIgnoreCase)
            ? "Checkout completed. BikeMate is verifying the payment with PayMongo now."
            : "Checkout was cancelled. No confirmed payment was recorded, and you can try again when ready.";
    }

    public static async Task TryNavigateToCheckoutAsync()
    {
        var info = PeekReturn();
        if (info is null || Shell.Current is null)
        {
            return;
        }

        var route = info.PaymentId > 0
            ? $"{nameof(PaymentCheckoutPage)}?paymentId={info.PaymentId}"
            : info.RequestId > 0
                ? $"{nameof(PaymentCheckoutPage)}?requestId={info.RequestId}"
                : nameof(PaymentCheckoutPage);

        try
        {
            await Shell.Current.GoToAsync(route);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Payment return navigation failed: {ex}");
        }
    }
}
