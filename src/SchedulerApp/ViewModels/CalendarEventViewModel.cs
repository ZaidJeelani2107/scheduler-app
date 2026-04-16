using CommunityToolkit.Mvvm.ComponentModel;
using SchedulerApp.Models;

namespace SchedulerApp.ViewModels;

/// <summary>Wraps a CalendarEvent with layout calculations for the scrollable timeline (1.2 px/min = 72 px/hr).</summary>
public partial class CalendarEventViewModel : ObservableObject
{
    // Changing this affects all timeline measurements — TimelineView hour-label spacing must be updated in sync.
    public const double PixelsPerMinute = 1.2;

    private readonly int _timelineStartHour;
    private readonly double _timelineHeightPx;

    public CalendarEvent Model { get; }

    public CalendarEventViewModel(CalendarEvent model, int timelineStartHour, int timelineEndHour)
    {
        Model = model;
        _timelineStartHour = timelineStartHour;
        _timelineHeightPx = (timelineEndHour - timelineStartHour) * 60 * PixelsPerMinute;
    }

    public string Title => Model.Title;
    public DateTime StartTime => Model.StartTime;
    public DateTime EndTime => Model.EndTime;
    public bool IsAiScheduled => Model.IsAiScheduled;
    public string TimeLabel => $"{Model.StartTime:h:mm tt} – {Model.EndTime:h:mm tt}";
    public double DurationMinutes => (Model.EndTime - Model.StartTime).TotalMinutes;

    // Clamped so events outside the visible window pin to the boundary instead of overflowing.
    public double TopOffset =>
        Math.Clamp(
            ((Model.StartTime.Hour - _timelineStartHour) * 60 + Model.StartTime.Minute) * PixelsPerMinute,
            0,
            _timelineHeightPx);

    // Min 24 px so very short events remain tappable.
    public double Height =>
        Math.Max(
            Math.Min(DurationMinutes * PixelsPerMinute, _timelineHeightPx - TopOffset),
            24);

    private static readonly Color TaskColor       = Color.FromArgb("#4A90D9");
    private static readonly Color FocusBlockColor = Color.FromArgb("#7B68EE");
    private static readonly Color EventColor      = Color.FromArgb("#5B9BD5");
    private static readonly Color AiAccentColorValue = Color.FromArgb("#FF8C00");

    public Color BackgroundColor => Model.Type switch
    {
        EventType.Task       => TaskColor,
        EventType.FocusBlock => FocusBlockColor,
        _                    => EventColor
    };

    // Orange for AI-scheduled events, blue for manual — single binding replaces two conditional BoxViews.
    public Color AccentStripColor => Model.IsAiScheduled ? AiAccentColorValue : TaskColor;
}
