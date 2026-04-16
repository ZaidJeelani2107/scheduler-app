using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SchedulerApp.Services;

namespace SchedulerApp.ViewModels;

/// <summary>Flat observable properties for work-hour and work-day preferences — one bool per day for direct checkbox binding.</summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly IUserPreferencesService _prefs;

    [ObservableProperty] private TimeSpan _workStartTime;
    [ObservableProperty] private TimeSpan _workEndTime;

    // One observable bool per day — binds directly to each checkbox without a collection binding.
    [ObservableProperty] private bool _monday;
    [ObservableProperty] private bool _tuesday;
    [ObservableProperty] private bool _wednesday;
    [ObservableProperty] private bool _thursday;
    [ObservableProperty] private bool _friday;
    [ObservableProperty] private bool _saturday;
    [ObservableProperty] private bool _sunday;

    [ObservableProperty] private string? _validationError;

    public SettingsViewModel(IUserPreferencesService prefs)
    {
        _prefs = prefs;
        LoadFromPreferences();
    }

    private void LoadFromPreferences()
    {
        WorkStartTime = _prefs.WorkStartTime;
        WorkEndTime = _prefs.WorkEndTime;

        var days = _prefs.WorkDays;
        Monday    = days.Contains(DayOfWeek.Monday);
        Tuesday   = days.Contains(DayOfWeek.Tuesday);
        Wednesday = days.Contains(DayOfWeek.Wednesday);
        Thursday  = days.Contains(DayOfWeek.Thursday);
        Friday    = days.Contains(DayOfWeek.Friday);
        Saturday  = days.Contains(DayOfWeek.Saturday);
        Sunday    = days.Contains(DayOfWeek.Sunday);
    }

    [RelayCommand]
    private async Task Save()
    {
        if (WorkEndTime <= WorkStartTime)
        {
            ValidationError = "End time must be after start time.";
            return;
        }

        var days = new List<DayOfWeek>();
        if (Monday)    days.Add(DayOfWeek.Monday);
        if (Tuesday)   days.Add(DayOfWeek.Tuesday);
        if (Wednesday) days.Add(DayOfWeek.Wednesday);
        if (Thursday)  days.Add(DayOfWeek.Thursday);
        if (Friday)    days.Add(DayOfWeek.Friday);
        if (Saturday)  days.Add(DayOfWeek.Saturday);
        if (Sunday)    days.Add(DayOfWeek.Sunday);

        if (days.Count == 0)
        {
            ValidationError = "Select at least one working day.";
            return;
        }

        _prefs.WorkStartTime = WorkStartTime;
        _prefs.WorkEndTime   = WorkEndTime;
        _prefs.WorkDays      = days;

        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await Shell.Current.GoToAsync("..");
    }
}
