using SchedulerApp.Api.Models;
using System.Text;

namespace SchedulerApp.Api.Services;

public interface IPromptBuilder
{
    string BuildSchedulePrompt(ScheduleRequest request);
}

public class PromptBuilder : IPromptBuilder
{
    public string BuildSchedulePrompt(ScheduleRequest request)
    {
        var sb = new StringBuilder();

        sb.AppendLine(BuildSystemRules(request.Preferences));
        sb.AppendLine();
        sb.AppendLine($"Today is {request.CurrentDate:dddd, MMMM d yyyy, h:mm tt}.");
        sb.AppendLine();
        sb.AppendLine("The user's calendar for the next 7 days:");

        if (request.Events.Count == 0)
        {
            sb.AppendLine("(No events scheduled — the calendar is empty.)");
        }
        else
        {
            // Times are in the user's local timezone — same timezone as CurrentDate's offset.
            foreach (var evt in request.Events.OrderBy(e => e.StartTime))
            {
                sb.AppendLine($"- \"{evt.Title}\" {evt.StartTime:ddd MMM d, h:mm tt} – {evt.EndTime:h:mm tt}");
            }
        }

        if (request.ExcludedSlots is { Count: > 0 } excluded)
        {
            sb.AppendLine();
            sb.AppendLine("The user already rejected these suggested times — do NOT suggest them again:");
            foreach (var slot in excluded)
                sb.AppendLine($"- {slot.StartTime:ddd MMM d, h:mm tt} – {slot.EndTime:h:mm tt}");
        }

        sb.AppendLine();
        sb.AppendLine($"User said: \"{request.UserInput}\"");
        sb.AppendLine();
        sb.AppendLine("""
            Return ONLY this JSON — no text outside the braces:
            {
              "title": "string",
              "startTime": "ISO8601 datetime",
              "endTime": "ISO8601 datetime",
              "reasoning": "1-2 sentence explanation of why you chose this slot",
              "conflictDetected": false,
              "eventType": "Task",
              "alternatives": [
                { "startTime": "ISO8601", "endTime": "ISO8601", "label": "human-readable e.g. Mon 10–12 AM" }
              ]
            }

            Rules for alternatives:
            - If conflictDetected is false, alternatives must be an empty array [].
            - If conflictDetected is true, provide exactly 3 alternatives within working hours that avoid all existing events.

            Rules for eventType — set to exactly one of these three values:
            - "Task": appointments, meetings, errands, to-dos with a specific time.
            - "FocusBlock": deep work, study sessions, concentration time, creative work.
            - "Event": social events, celebrations, travel, anything that isn't work or a task.
            """);

        return sb.ToString();
    }

    private static string BuildSystemRules(WorkPreferencesDto? prefs)
    {
        var startLabel = FormatHour(prefs?.WorkStartHour ?? 8);
        var endLabel = FormatHour(prefs?.WorkEndHour ?? 19);
        var days = prefs?.WorkDays is { Count: > 0 } d
            ? string.Join(", ", d)
            : "Monday, Tuesday, Wednesday, Thursday, Friday";

        return $"""
            You are an intelligent scheduling assistant. Your job is to:
            1. Parse the user's natural language input into a calendar event or task.
            2. Find the optimal available time slot in their calendar.
            3. Detect any scheduling conflicts if the user explicitly requests a time that overlaps with an existing event.
            4. Suggest exactly 3 alternative slots when a conflict is detected.

            Rules you must follow:
            - Only schedule within working hours: {startLabel} to {endLabel}. Both the start AND end time of the event must fall within these hours — never let an event run past {endLabel}.
            - Never schedule during existing events.
            - Never shorten the requested duration to fit a partial gap — if the full duration does not fit on a given day, skip that day and find the next available day where the full duration fits as a continuous block.
            - Never suggest a date in the past — all suggested times must be today or in the future.
            - For tasks with a deadline (e.g. "due Friday"), find the earliest available continuous slot before that deadline.
            - Prefer continuous blocks over fragmented time.
            - Working days are: {days}.
            - Always return valid ISO 8601 datetime strings (e.g. 2026-04-13T14:00:00).
            - Return ONLY the JSON object specified. No text outside the JSON braces.
            """;
    }

    private static string FormatHour(int hour) => hour switch
    {
        0 => "12:00 AM",
        < 12 => $"{hour}:00 AM",
        12 => "12:00 PM",
        _ => $"{hour - 12}:00 PM"
    };
}
