namespace SchedulerApp.Api.Models;

public record WorkPreferencesDto(
    int WorkStartHour,
    int WorkEndHour,
    List<string> WorkDays);
