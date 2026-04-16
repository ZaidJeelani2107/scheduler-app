namespace SchedulerApp.Api.Models;

public record CalendarEventDto(
    string Id,
    string Title,
    DateTime StartTime,
    DateTime EndTime,
    string Type);
