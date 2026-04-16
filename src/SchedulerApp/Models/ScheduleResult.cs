namespace SchedulerApp.Models;

public class ScheduleResult
{
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public bool ConflictDetected { get; set; }
    public List<AlternativeSlot> Alternatives { get; set; } = [];

    // Parsed to EventType via Enum.TryParse in DayViewModel.AcceptConfirmation; falls back to Task.
    public string EventType { get; set; } = "Task";
}

public class AlternativeSlot
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Label { get; set; } = string.Empty;
}
