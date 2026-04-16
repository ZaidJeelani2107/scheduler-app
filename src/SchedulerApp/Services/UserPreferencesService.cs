namespace SchedulerApp.Services;

public class UserPreferencesService : IUserPreferencesService
{
    private static readonly IReadOnlyList<DayOfWeek> DefaultWorkDays =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
        DayOfWeek.Thursday, DayOfWeek.Friday
    ];

    private TimeSpan? _cachedWorkStartTime;
    private TimeSpan? _cachedWorkEndTime;
    private IReadOnlyList<DayOfWeek>? _cachedWorkDays;

    /// <inheritdoc/>
    public TimeSpan WorkStartTime
    {
        get
        {
            if (_cachedWorkStartTime.HasValue) return _cachedWorkStartTime.Value;
            var raw = Preferences.Default.Get("WorkStartTime", "08:00:00");
            _cachedWorkStartTime = TimeSpan.TryParse(raw, out var t) ? t : TimeSpan.FromHours(8);
            return _cachedWorkStartTime.Value;
        }
        set
        {
            _cachedWorkStartTime = value;
            Preferences.Default.Set("WorkStartTime", value.ToString());
        }
    }

    /// <inheritdoc/>
    public TimeSpan WorkEndTime
    {
        get
        {
            if (_cachedWorkEndTime.HasValue) return _cachedWorkEndTime.Value;
            var raw = Preferences.Default.Get("WorkEndTime", "19:00:00");
            _cachedWorkEndTime = TimeSpan.TryParse(raw, out var t) ? t : TimeSpan.FromHours(19);
            return _cachedWorkEndTime.Value;
        }
        set
        {
            _cachedWorkEndTime = value;
            Preferences.Default.Set("WorkEndTime", value.ToString());
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<DayOfWeek> WorkDays
    {
        get
        {
            if (_cachedWorkDays is not null) return _cachedWorkDays;
            var raw = Preferences.Default.Get("WorkDays", string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                _cachedWorkDays = DefaultWorkDays;
                return _cachedWorkDays;
            }
            try
            {
                _cachedWorkDays = raw.Split(',')
                    .Select(s => Enum.Parse<DayOfWeek>(s.Trim()))
                    .ToList();
            }
            catch
            {
                _cachedWorkDays = DefaultWorkDays;
            }
            return _cachedWorkDays;
        }
        set
        {
            _cachedWorkDays = value;
            Preferences.Default.Set("WorkDays", string.Join(",", value.Select(d => d.ToString())));
        }
    }
}
