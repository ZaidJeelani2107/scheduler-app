using Microsoft.Maui.Layouts;
using SchedulerApp.ViewModels;
using System.ComponentModel;

namespace SchedulerApp.Views;

public partial class DayView : ContentPage
{
    private DayViewModel? _viewModel;

    public DayView(DayViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_viewModel is not null)
        {
            // Unsubscribe first so re-entry (e.g. iOS back-stack reuse) never double-fires.
            _viewModel.TimelineChanged -= OnTimelineChanged;
            _viewModel.TimelineChanged += OnTimelineChanged;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        // Set initial overlay state without animation
        ConfirmationOverlay.IsVisible = _viewModel?.PendingConfirmation is not null;
        EditOverlay.IsVisible = _viewModel?.EditingEvent is not null;

        // Rebuild the timeline grid whenever the page appears — picks up any
        // preference changes the user may have made in Settings.
        BuildTimelineStructure();
        RefreshTimeline();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_viewModel is not null)
        {
            _viewModel.TimelineChanged -= OnTimelineChanged;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnTimelineChanged(object? sender, EventArgs e) => RefreshTimeline();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DayViewModel.PendingConfirmation))
        {
            if (_viewModel!.PendingConfirmation is not null)
                _ = ShowSheetAsync(ConfirmationOverlay, ConfirmationSheet);
            else
                _ = HideSheetAsync(ConfirmationOverlay, ConfirmationSheet);
        }
        else if (e.PropertyName == nameof(DayViewModel.EditingEvent))
        {
            if (_viewModel!.EditingEvent is not null)
                _ = ShowSheetAsync(EditOverlay, EditSheet);
            else
                _ = HideSheetAsync(EditOverlay, EditSheet);
        }
    }

    private static async Task ShowSheetAsync(View overlay, View sheet)
    {
        sheet.TranslationY = 600;
        overlay.IsVisible = true;
        await sheet.TranslateToAsync(0, 0, 300, Easing.CubicOut);
    }

    private static async Task HideSheetAsync(View overlay, View sheet)
    {
        await sheet.TranslateToAsync(0, 600, 240, Easing.CubicIn);
        overlay.IsVisible = false;
        sheet.TranslationY = 0;
    }

    // Tracks how many hour-divider BoxViews are currently at the start of TimelineLayout.
    // Set by BuildTimelineStructure() and used by RefreshTimeline() to know where event blocks begin.
    private int _timelineDividerCount;

    /// <summary>
    /// Regenerates the hour-label column and the AbsoluteLayout divider lines to match the
    /// user's current WorkStartTime / WorkEndTime preferences.  Call this in OnAppearing so
    /// any preference changes made in Settings are reflected immediately.
    /// </summary>
    private void BuildTimelineStructure()
    {
        if (_viewModel is null) return;

        int startHour  = _viewModel.TimelineStartHour;
        int endHour    = _viewModel.TimelineEndHour;
        int hourCount  = endHour - startHour;
        double pxPerHour   = CalendarEventViewModel.PixelsPerMinute * 60; // 72 px
        double totalHeight = hourCount * pxPerHour;

        var resources    = Application.Current!.Resources;
        var dividerLight = (Color)resources["DividerLight"];
        var dividerDark  = (Color)resources["DividerDark"];
        var labelGray    = (Color)resources["HourLabelGray"];

        // ── Hour labels ────────────────────────────────────────────────────────
        HourLabelsLayout.Children.Clear();
        for (int h = startHour; h < endHour; h++)
        {
            HourLabelsLayout.Children.Add(new Label
            {
                Text              = FormatHour(h),
                FontSize          = 11,
                TextColor         = labelGray,
                HeightRequest     = pxPerHour,
                VerticalOptions   = LayoutOptions.Start,
                HorizontalOptions = LayoutOptions.End,
                Margin            = new Thickness(0, 0, 6, 0)
            });
        }

        // ── Divider lines (clear all children; event blocks are re-added by RefreshTimeline) ──
        TimelineLayout.Children.Clear();
        for (int i = 0; i < hourCount; i++)
        {
            var divider = new BoxView { HeightRequest = 1 };
            divider.SetAppThemeColor(BoxView.ColorProperty, dividerLight, dividerDark);
            AbsoluteLayout.SetLayoutFlags(divider, AbsoluteLayoutFlags.WidthProportional);
            AbsoluteLayout.SetLayoutBounds(divider, new Rect(0, i * pxPerHour, 1, 1));
            TimelineLayout.Children.Add(divider);
        }

        TimelineLayout.HeightRequest = totalHeight;
        _timelineDividerCount = hourCount;
    }

    private static string FormatHour(int hour)
    {
        if (hour == 0)  return "12 AM";
        if (hour < 12)  return $"{hour} AM";
        if (hour == 12) return "12 PM";
        return $"{hour - 12} PM";
    }

    private void RefreshTimeline()
    {
        if (_viewModel is null) return;

        // Remove event blocks added in a previous refresh (keep the divider lines at the front).
        while (TimelineLayout.Children.Count > _timelineDividerCount)
            TimelineLayout.Children.RemoveAt(TimelineLayout.Children.Count - 1);

        foreach (var evt in _viewModel.Events)
        {
            var block = new EventBlockView(evt);
            block.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = _viewModel.EditEventCommand,
                CommandParameter = evt
            });
            AbsoluteLayout.SetLayoutBounds(block, new Rect(4, evt.TopOffset, 0.97, evt.Height));
            AbsoluteLayout.SetLayoutFlags(block, AbsoluteLayoutFlags.WidthProportional);
            TimelineLayout.Children.Add(block);
        }
    }
}
