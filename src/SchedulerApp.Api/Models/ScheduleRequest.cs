namespace SchedulerApp.Api.Models;

public record ScheduleRequest(
    string UserInput,
    DateTimeOffset CurrentDate,
    List<CalendarEventDto> Events,
    WorkPreferencesDto? Preferences = null,
    List<ExcludedSlotDto>? ExcludedSlots = null);
