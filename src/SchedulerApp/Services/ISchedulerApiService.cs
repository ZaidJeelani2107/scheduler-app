using SchedulerApp.Models;

namespace SchedulerApp.Services;

public interface ISchedulerApiService
{
    Task<ScheduleResult?> ScheduleAsync(
        string userInput,
        IReadOnlyList<CalendarEvent> currentEvents,
        IReadOnlyList<ExcludedSlot>? excludedSlots = null,
        CancellationToken ct = default);
}
