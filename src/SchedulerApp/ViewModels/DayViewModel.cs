using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SchedulerApp.Infrastructure;
using SchedulerApp.Models;
using SchedulerApp.Services;
using System.Collections.ObjectModel;

namespace SchedulerApp.ViewModels;

public partial class DayViewModel : ObservableObject
{
    private readonly ICalendarStore _store;
    private readonly ISchedulerApiService _apiService;
    private readonly ILogger<DayViewModel> _logger;
    private readonly IUserPreferencesService _prefs;
    private readonly IUiDispatcher _dispatcher;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private string _aiInputText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Auto-dismisses after 4 seconds. Setting a new value cancels any pending dismiss.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private ScheduleResultViewModel? _pendingConfirmation;

    [ObservableProperty]
    private CalendarEventViewModel? _editingEvent;

    [ObservableProperty]
    private string _editTitle = string.Empty;

    [ObservableProperty]
    private TimeSpan _editStartTime;

    [ObservableProperty]
    private TimeSpan _editEndTime;

    [ObservableProperty]
    private string? _editError;

    // Snapshot of values when the edit overlay opens — compared against current values
    // by CanSaveEdit to ensure the Save button is disabled until at least one field changes.
    private string _originalEditTitle = string.Empty;
    private TimeSpan _originalEditStartTime;
    private TimeSpan _originalEditEndTime;

    // Accumulates slots rejected by the user during the current reschedule sequence.
    // Passed to the API on each Reschedule so Gemini never re-suggests the same window.
    // Cleared on Accept or Dismiss to reset for the next scheduling request.
    private readonly List<ExcludedSlot> _excludedSlots = [];
    private string _lastAiInput = string.Empty;

    // Cancellation token source for the auto-dismiss timer. Cancelled when a new error
    // replaces the current one so we don't dismiss the new message early.
    private CancellationTokenSource? _errorDismissCts;

    public ObservableCollection<CalendarEventViewModel> Events { get; } = [];
    public bool HasNoEvents => Events.Count == 0;

    /// <summary>Raised after Events is repopulated so DayView can recalculate scroll position post-layout.</summary>
    public event EventHandler? TimelineChanged;

    public string DateLabel => SelectedDate.ToString("dddd, MMMM d");
    public int TimelineStartHour => _prefs.WorkStartTime.Hours;

    // +1 so the last work-hour slot is fully visible on the timeline.
    public int TimelineEndHour => Math.Max(_prefs.WorkEndTime.Hours + 1, TimelineStartHour + 1);

    public DayViewModel(ICalendarStore store, ISchedulerApiService apiService, ILogger<DayViewModel> logger, IUserPreferencesService prefs, IUiDispatcher dispatcher)
    {
        _store = store;
        _apiService = apiService;
        _logger = logger;
        _prefs = prefs;
        _dispatcher = dispatcher;
        _store.CalendarChanged += (_, _) => FireAndForget(LoadEventsAsync());
        FireAndForget(LoadEventsAsync());
    }

    // Runs a fire-and-forget task, logging any unhandled exception rather than crashing the app.
    private void FireAndForget(Task task) =>
        task.ContinueWith(
            t => _logger.LogError(t.Exception!.GetBaseException(), "Background operation failed"),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);

    partial void OnSelectedDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(DateLabel));
        FireAndForget(LoadEventsAsync());
    }

    partial void OnAiInputTextChanged(string value)
    {
        SubmitAiInputCommand.NotifyCanExecuteChanged();
    }

    partial void OnEditTitleChanged(string value) => SaveEditCommand.NotifyCanExecuteChanged();
    partial void OnEditStartTimeChanged(TimeSpan value) => SaveEditCommand.NotifyCanExecuteChanged();
    partial void OnEditEndTimeChanged(TimeSpan value) => SaveEditCommand.NotifyCanExecuteChanged();

    partial void OnErrorMessageChanged(string? value)
    {
        _errorDismissCts?.Cancel();
        _errorDismissCts = null;
        if (value is not null)
        {
            var cts = new CancellationTokenSource();
            _errorDismissCts = cts;
            _ = AutoDismissErrorAsync(cts.Token);
        }
    }

    private async Task AutoDismissErrorAsync(CancellationToken ct)
    {
        try { await Task.Delay(4000, ct); ErrorMessage = null; }
        catch (OperationCanceledException) { }
    }

    // Fetches events for SelectedDate from the store and rebuilds the Events collection
    // on the main thread, then fires TimelineChanged for scroll-position recalculation.
    private async Task LoadEventsAsync()
    {
        var date = DateOnly.FromDateTime(SelectedDate);
        var events = await _store.GetEventsForDayAsync(date);

        await _dispatcher.InvokeOnMainThreadAsync(() =>
        {
            Events.Clear();
            int startHour = TimelineStartHour;
            int endHour   = TimelineEndHour;
            foreach (var e in events)
                Events.Add(new CalendarEventViewModel(e, startHour, endHour));

            OnPropertyChanged(nameof(HasNoEvents));
            TimelineChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    /// <summary>
    /// Moves <see cref="SelectedDate"/> forward or backward by <paramref name="offset"/> days.
    /// Bound to the prev/next day navigation arrows.
    /// </summary>
    [RelayCommand]
    private void NavigateDay(int offset)
    {
        SelectedDate = SelectedDate.AddDays(offset);
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        await Shell.Current.GoToAsync(AppShell.SettingsRoute);
    }

    private bool CanSubmitAiInput() => !string.IsNullOrWhiteSpace(AiInputText);

    // Always uses today's date as context window start — the AI needs future availability, not the viewed day.
    [RelayCommand(CanExecute = nameof(CanSubmitAiInput))]
    private async Task SubmitAiInput(CancellationToken ct)
    {
        var input = AiInputText.Trim();
        if (string.IsNullOrEmpty(input))
            return;

        _lastAiInput = input;
        _excludedSlots.Clear();

        if (await CallScheduleApiAsync(input, [], ct))
            AiInputText = string.Empty;
    }

    [RelayCommand]
    private async Task AcceptConfirmation(AlternativeSlotViewModel? chosen)
    {
        if (PendingConfirmation is null)
            return;

        DateTime start, end;
        if (chosen is not null)
        {
            start = chosen.Model.StartTime;
            end = chosen.Model.EndTime;
        }
        else
        {
            start = PendingConfirmation.Model.StartTime;
            end = PendingConfirmation.Model.EndTime;
        }

        var newEvent = new CalendarEvent
        {
            Title = PendingConfirmation.Title,
            StartTime = start,
            EndTime = end,
            Type = Enum.TryParse<EventType>(PendingConfirmation.Model.EventType, ignoreCase: true, out var parsedType)
                ? parsedType
                : EventType.Task,
            IsAiScheduled = true
        };

        await _store.AddEventAsync(newEvent);
        _excludedSlots.Clear();
        _lastAiInput = string.Empty;

        SelectedDate = start.Date;
        PendingConfirmation = null;
    }

    [RelayCommand]
    private void DismissConfirmation()
    {
        _excludedSlots.Clear();
        _lastAiInput = string.Empty;
        PendingConfirmation = null;
    }

    [RelayCommand]
    private async Task Reschedule(CancellationToken ct)
    {
        if (PendingConfirmation is null) return;

        _excludedSlots.Add(new ExcludedSlot(
            PendingConfirmation.Model.StartTime,
            PendingConfirmation.Model.EndTime));

        await CallScheduleApiAsync(_lastAiInput, [.. _excludedSlots], ct);
    }

    private async Task<bool> CallScheduleApiAsync(string input, IReadOnlyList<ExcludedSlot> excluded, CancellationToken ct)
    {
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var contextDate = DateOnly.FromDateTime(DateTime.Today);
            var contextEvents = await _store.GetEventsForRangeAsync(contextDate, contextDate.AddDays(6));

            var result = await _apiService.ScheduleAsync(input, contextEvents, excluded, ct);

            if (result is null)
            {
                ErrorMessage = "Couldn't reach the scheduling service. Please try again.";
                return false;
            }

            PendingConfirmation = new ScheduleResultViewModel(result);
            return true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void EditEvent(CalendarEventViewModel evt)
    {
        _originalEditTitle = evt.Title;
        _originalEditStartTime = evt.StartTime.TimeOfDay;
        _originalEditEndTime = evt.EndTime.TimeOfDay;

        EditTitle = evt.Title;
        EditStartTime = evt.StartTime.TimeOfDay;
        EditEndTime = evt.EndTime.TimeOfDay;
        EditError = null;
        EditingEvent = evt;
    }

    // Requires title non-empty, end > start, AND at least one field changed from the snapshot.
    private bool CanSaveEdit() =>
        !string.IsNullOrWhiteSpace(EditTitle) &&
        EditEndTime > EditStartTime &&
        (EditTitle.Trim() != _originalEditTitle ||
         EditStartTime != _originalEditStartTime ||
         EditEndTime != _originalEditEndTime);

    [RelayCommand(CanExecute = nameof(CanSaveEdit))]
    private async Task SaveEdit()
    {
        if (EditingEvent is null) return;
        var date = EditingEvent.StartTime.Date;
        await _store.UpdateEventAsync(EditingEvent.Model.Id, EditTitle.Trim(), date + EditStartTime, date + EditEndTime);
        EditingEvent = null;
    }

    [RelayCommand]
    private async Task DeleteEdit()
    {
        if (EditingEvent is null) return;
        await _store.RemoveEventAsync(EditingEvent.Model.Id);
        EditingEvent = null;
    }

    [RelayCommand]
    private void DismissEdit()
    {
        EditingEvent = null;
    }
}
