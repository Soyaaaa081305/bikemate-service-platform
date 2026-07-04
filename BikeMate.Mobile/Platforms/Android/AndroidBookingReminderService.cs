using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using BikeMate.Core.DTOs;
using BikeMate.Services;
using Microsoft.Maui.ApplicationModel;
using System.Runtime.Versioning;

#pragma warning disable CA1416

namespace BikeMate.Platforms.Android;

public sealed class AndroidBookingReminderService : IBookingReminderService
{
    internal const string ChannelId = "bikemate_booking_reminders";
    internal const string ChannelName = "Booking reminders";
    internal const string ExtraNotificationId = "notification_id";
    internal const string ExtraRequestId = "request_id";
    internal const string ExtraTitle = "title";
    internal const string ExtraMessage = "message";
    private const string PostNotificationsPermissionName = "android.permission.POST_NOTIFICATIONS";

    public async Task ScheduleUpcomingBookingRemindersAsync(IEnumerable<ServiceRequestDto> requests, CancellationToken cancellationToken = default)
    {
        var context = Platform.AppContext;
        if (context is null)
        {
            return;
        }

        EnsureNotificationChannel(context);
        await RequestNotificationPermissionAsync();

        var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
        if (alarmManager is null)
        {
            return;
        }

        foreach (var request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ScheduleRequestReminders(context, alarmManager, request);
        }
    }

    private static void ScheduleRequestReminders(Context context, AlarmManager alarmManager, ServiceRequestDto request)
    {
        if (request.ScheduledAt is null || IsClosed(request.CurrentStatus))
        {
            CancelReminder(context, alarmManager, request.RequestId, 60);
            CancelReminder(context, alarmManager, request.RequestId, 0);
            return;
        }

        var scheduledLocal = request.ScheduledAt.Value.Kind == DateTimeKind.Utc
            ? request.ScheduledAt.Value.ToLocalTime()
            : request.ScheduledAt.Value;

        ScheduleReminder(
            context,
            alarmManager,
            request,
            scheduledLocal.AddHours(-1),
            60,
            "Booking soon",
            $"{DisplayServiceName(request)} is scheduled at {scheduledLocal:h:mm tt}. Check your shop, address, and payment status.");

        ScheduleReminder(
            context,
            alarmManager,
            request,
            scheduledLocal,
            0,
            "Booking time",
            $"{DisplayServiceName(request)} is scheduled now. Open BikeMate for the latest status.");
    }

    private static void ScheduleReminder(
        Context context,
        AlarmManager alarmManager,
        ServiceRequestDto request,
        DateTime fireAtLocal,
        int minutesBefore,
        string title,
        string message)
    {
        if (fireAtLocal <= DateTime.Now.AddMinutes(1))
        {
            CancelReminder(context, alarmManager, request.RequestId, minutesBefore);
            return;
        }

        var intent = new Intent(context, typeof(BookingReminderReceiver));
        intent.PutExtra(ExtraNotificationId, NotificationId(request.RequestId, minutesBefore));
        intent.PutExtra(ExtraRequestId, request.RequestId);
        intent.PutExtra(ExtraTitle, title);
        intent.PutExtra(ExtraMessage, message);

        var pendingIntent = PendingIntent.GetBroadcast(
            context,
            NotificationId(request.RequestId, minutesBefore),
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        if (pendingIntent is null)
        {
            return;
        }

        var triggerAt = new DateTimeOffset(fireAtLocal).ToUnixTimeMilliseconds();
        alarmManager.Set(AlarmType.RtcWakeup, triggerAt, pendingIntent);
    }

    private static void CancelReminder(Context context, AlarmManager alarmManager, int requestId, int minutesBefore)
    {
        var intent = new Intent(context, typeof(BookingReminderReceiver));
        var pendingIntent = PendingIntent.GetBroadcast(
            context,
            NotificationId(requestId, minutesBefore),
            intent,
            PendingIntentFlags.NoCreate | PendingIntentFlags.Immutable);

        if (pendingIntent is not null)
        {
            alarmManager.Cancel(pendingIntent);
            pendingIntent.Cancel();
        }
    }

    internal static void EnsureNotificationChannel(Context context)
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        CreateNotificationChannel(context);
    }

    [SupportedOSPlatform("android26.0")]
    private static void CreateNotificationChannel(Context context)
    {
        var manager = context.GetSystemService(Context.NotificationService) as NotificationManager;
        var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Default)
        {
            Description = "Reminders for upcoming BikeMate bookings."
        };
        manager?.CreateNotificationChannel(channel);
    }

    private static async Task RequestNotificationPermissionAsync()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu ||
            ContextCompat.CheckSelfPermission(Platform.AppContext, PostNotificationsPermissionName) == Permission.Granted)
        {
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Permissions.RequestAsync<PostNotificationsPermission>();
        });
    }

    private static int NotificationId(int requestId, int minutesBefore)
    {
        unchecked
        {
            return (requestId * 100) + minutesBefore + 7000;
        }
    }

    private static string DisplayServiceName(ServiceRequestDto request)
    {
        return string.IsNullOrWhiteSpace(request.ServiceName) ? $"Booking BM-{request.RequestId:000000}" : request.ServiceName;
    }

    private static bool IsClosed(string status)
    {
        return status.Equals("completed", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("cancelled", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("rejected", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class PostNotificationsPermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu
            ? [("android.permission.POST_NOTIFICATIONS", true)]
            : [];
}

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class BookingReminderReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null)
        {
            return;
        }

        AndroidBookingReminderService.EnsureNotificationChannel(context);

        var notificationId = intent.GetIntExtra(AndroidBookingReminderService.ExtraNotificationId, 7000);
        var requestId = intent.GetIntExtra(AndroidBookingReminderService.ExtraRequestId, 0);
        var title = intent.GetStringExtra(AndroidBookingReminderService.ExtraTitle) ?? "BikeMate reminder";
        var message = intent.GetStringExtra(AndroidBookingReminderService.ExtraMessage) ?? "Open BikeMate for your latest booking status.";

        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? string.Empty)
            ?? new Intent(context, typeof(global::BikeMate.MainActivity));
        launchIntent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        if (requestId > 0)
        {
            launchIntent.PutExtra("request_id", requestId);
        }

        var contentIntent = PendingIntent.GetActivity(
            context,
            notificationId,
            launchIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
        if (contentIntent is null)
        {
            return;
        }

        var smallIcon = context.ApplicationInfo?.Icon ?? global::Android.Resource.Drawable.IcDialogInfo;
        var builder = new NotificationCompat.Builder(context, AndroidBookingReminderService.ChannelId);
        builder.SetSmallIcon(smallIcon);
        builder.SetContentTitle(title);
        builder.SetContentText(message);
        builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(message));
        builder.SetContentIntent(contentIntent);
        builder.SetAutoCancel(true);
        builder.SetPriority((int)NotificationPriority.Default);

        var notification = builder.Build();
        if (notification is null)
        {
            return;
        }

        var notificationManager = NotificationManagerCompat.From(context);
        if (notificationManager is null)
        {
            return;
        }

        notificationManager.Notify(notificationId, notification);
    }
}
