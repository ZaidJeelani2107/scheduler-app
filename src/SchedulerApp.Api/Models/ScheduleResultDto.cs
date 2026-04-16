namespace SchedulerApp.Api.Models;

// DateTime (not DateTimeOffset) is intentional — Gemini returns ISO 8601 strings with no UTC offset,
// so attaching an offset would silently misrepresent the intended local time.
public record ScheduleResultDto(
    string Title,
    DateTime StartTime,
    DateTime EndTime,
    string Reasoning,
    bool ConflictDetected,
    List<AlternativeSlotDto> Alternatives,
    string EventType = "Task");

public record AlternativeSlotDto(
    DateTime StartTime,
    DateTime EndTime,
    string Label);
