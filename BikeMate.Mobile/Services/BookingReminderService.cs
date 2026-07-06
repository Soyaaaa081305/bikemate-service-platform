using BikeMate.Core.DTOs;

namespace BikeMate.Services;

public interface IBookingReminderService
{
    Task<NotificationPermissionState> GetNotificationPermissionStateAsync(CancellationToken cancellationToken = default);
    Task<bool> EnsureNotificationsEnabledAsync(CancellationToken cancellationToken = default);
    Task OpenNotificationSettingsAsync(CancellationToken cancellationToken = default);
    Task ScheduleUpcomingBookingRemindersAsync(IEnumerable<ServiceRequestDto> requests, CancellationToken cancellationToken = default);
}

public enum NotificationPermissionState
{
    NotRequired,
    Enabled,
    Disabled
}

public sealed class NoOpBookingReminderService : IBookingReminderService
{
    public Task<NotificationPermissionState> GetNotificationPermissionStateAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(NotificationPermissionState.NotRequired);
    }

    public Task<bool> EnsureNotificationsEnabledAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task OpenNotificationSettingsAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ScheduleUpcomingBookingRemindersAsync(IEnumerable<ServiceRequestDto> requests, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
