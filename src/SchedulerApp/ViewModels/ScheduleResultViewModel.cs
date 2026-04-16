using CommunityToolkit.Mvvm.ComponentModel;
using SchedulerApp.Models;

namespace SchedulerApp.ViewModels;

/// <summary>Presentation model for the AI confirmation card. Title is observable so the user can rename before accepting.</summary>
public partial class ScheduleResultViewModel : ObservableObject
{
    public ScheduleResult Model { get; }

    [ObservableProperty]
    private string _title;

    public ScheduleResultViewModel(ScheduleResult model)
    {
        Model = model;
        _title = model.Title;
        Alternatives = model.Alternatives
            .Select(a => new AlternativeSlotViewModel(a))
            .ToList();
    }

    public string Reasoning => Model.Reasoning;
    public bool HasConflict => Model.ConflictDetected;
    public bool HasNoConflict => !Model.ConflictDetected;
    public string TimeRange => $"{Model.StartTime:ddd, MMM d  h:mm tt} – {Model.EndTime:h:mm tt}";
    public List<AlternativeSlotViewModel> Alternatives { get; }
}

public class AlternativeSlotViewModel
{
    public AlternativeSlot Model { get; }

    public AlternativeSlotViewModel(AlternativeSlot model)
    {
        Model = model;
    }

    public string Label => Model.Label;
    public string TimeRange => $"{Model.StartTime:h:mm tt} – {Model.EndTime:h:mm tt}";
}
