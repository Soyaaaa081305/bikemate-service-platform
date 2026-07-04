using BikeMate.Core.DTOs;

namespace BikeMate.Services;

public interface IBookingReminderService
{
    Task ScheduleUpcomingBookingRemindersAsync(IEnumerable<ServiceRequestDto> requests, CancellationToken cancellationToken = default);
}

public sealed class NoOpBookingReminderService : IBookingReminderService
{
    public Task ScheduleUpcomingBookingRemindersAsync(IEnumerable<ServiceRequestDto> requests, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
