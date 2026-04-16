namespace SchedulerApp.Services;

public interface IUserPreferencesService
{
    TimeSpan WorkStartTime { get; set; }
    TimeSpan WorkEndTime { get; set; }
    IReadOnlyList<DayOfWeek> WorkDays { get; set; }
}
