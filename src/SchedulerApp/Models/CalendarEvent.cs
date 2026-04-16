namespace SchedulerApp.Models;

public class CalendarEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public EventType Type { get; set; } = EventType.Event;
    public string? Description { get; set; }

    /// <summary>True when created via the AI flow — renders the orange accent stripe in EventBlockView.</summary>
    public bool IsAiScheduled { get; set; }
}

public enum EventType
{
    /// <summary>Social events, celebrations, travel.</summary>
    Event,

    /// <summary>Appointments, meetings, errands, to-dos with a specific time.</summary>
    Task,

    /// <summary>Deep work, study sessions, creative work requiring focus.</summary>
    FocusBlock
}
