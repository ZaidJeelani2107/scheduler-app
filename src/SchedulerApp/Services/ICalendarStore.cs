using SchedulerApp.Models;

namespace SchedulerApp.Services;

public interface ICalendarStore
{
    Task<IReadOnlyList<CalendarEvent>> GetEventsForDayAsync(DateOnly date);
    Task<IReadOnlyList<CalendarEvent>> GetEventsForRangeAsync(DateOnly from, DateOnly to);
    Task AddEventAsync(CalendarEvent evt);
    Task UpdateEventAsync(string id, string title, DateTime startTime, DateTime endTime);
    Task RemoveEventAsync(string id);

    /// <summary>Raised after any write so subscribers can reload without polling.</summary>
    event EventHandler CalendarChanged;
}
