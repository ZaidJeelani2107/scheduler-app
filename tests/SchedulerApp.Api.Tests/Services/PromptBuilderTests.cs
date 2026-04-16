using Microsoft.VisualStudio.TestTools.UnitTesting;
using SchedulerApp.Api.Models;
using SchedulerApp.Api.Services;

namespace SchedulerApp.Api.Tests.Services;

[TestClass]
public class PromptBuilderTests
{
    private readonly PromptBuilder _sut = new();

    private static ScheduleRequest BaseRequest() => new(
        "Schedule a meeting",
        new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero),
        [],
        null,
        null);

    // ── Calendar events ────────────────────────────────────────────────────

    [TestMethod]
    public void EmptyCalendar_ContainsEmptyCalendarMessage()
    {
        var result = _sut.BuildSchedulePrompt(BaseRequest());
        StringAssert.Contains(result, "No events scheduled");
    }

    [TestMethod]
    public void WithEvents_EventsListedSortedByStartTime()
    {
        var request = BaseRequest() with
        {
            Events =
            [
                new CalendarEventDto("2", "Afternoon review", new DateTime(2026, 4, 14, 14, 0, 0), new DateTime(2026, 4, 14, 15, 0, 0), "meeting"),
                new CalendarEventDto("1", "Morning standup",  new DateTime(2026, 4, 14,  9, 0, 0), new DateTime(2026, 4, 14,  9, 30, 0), "meeting"),
            ]
        };

        var result = _sut.BuildSchedulePrompt(request);

        var morningPos   = result.IndexOf("Morning standup",  StringComparison.Ordinal);
        var afternoonPos = result.IndexOf("Afternoon review", StringComparison.Ordinal);
        Assert.IsTrue(morningPos < afternoonPos, "Earlier event should appear before later event");
    }

    [TestMethod]
    public void WithEvents_EventTitlesAppearInPrompt()
    {
        var request = BaseRequest() with
        {
            Events = [new CalendarEventDto("1", "Doctor appointment", new DateTime(2026, 4, 14, 10, 0, 0), new DateTime(2026, 4, 14, 11, 0, 0), "task")]
        };

        var result = _sut.BuildSchedulePrompt(request);
        StringAssert.Contains(result, "Doctor appointment");
    }

    // ── User input ─────────────────────────────────────────────────────────

    [TestMethod]
    public void UserInputAppearsQuotedInPrompt()
    {
        var request = BaseRequest() with { UserInput = "Book a dentist appointment" };
        var result = _sut.BuildSchedulePrompt(request);
        StringAssert.Contains(result, "\"Book a dentist appointment\"");
    }

    // ── Excluded slots ─────────────────────────────────────────────────────

    [TestMethod]
    public void NoExcludedSlots_ExclusionSectionAbsent()
    {
        var result = _sut.BuildSchedulePrompt(BaseRequest());
        Assert.IsFalse(result.Contains("already rejected"), "Exclusion section should not appear when no slots are excluded");
    }

    [TestMethod]
    public void WithExcludedSlots_ExclusionSectionPresent()
    {
        var request = BaseRequest() with
        {
            ExcludedSlots = [new ExcludedSlotDto(new DateTime(2026, 4, 14, 10, 0, 0), new DateTime(2026, 4, 14, 11, 0, 0))]
        };

        var result = _sut.BuildSchedulePrompt(request);
        StringAssert.Contains(result, "already rejected");
    }

    [TestMethod]
    public void WithMultipleExcludedSlots_AllSlotsAppear()
    {
        var request = BaseRequest() with
        {
            ExcludedSlots =
            [
                new ExcludedSlotDto(new DateTime(2026, 4, 14, 10, 0, 0), new DateTime(2026, 4, 14, 11, 0, 0)),
                new ExcludedSlotDto(new DateTime(2026, 4, 14, 14, 0, 0), new DateTime(2026, 4, 14, 15, 0, 0)),
            ]
        };

        var result = _sut.BuildSchedulePrompt(request);

        var rejectedIndex = result.IndexOf("already rejected", StringComparison.Ordinal);
        var afterRejected = result[rejectedIndex..];
        StringAssert.Contains(afterRejected, "10:00");
        StringAssert.Contains(afterRejected, "2:00");
    }

    // ── Preferences: defaults ──────────────────────────────────────────────

    [TestMethod]
    public void NullPreferences_DefaultsToEightAmSevenPm()
    {
        var result = _sut.BuildSchedulePrompt(BaseRequest());
        StringAssert.Contains(result, "8:00 AM");
        StringAssert.Contains(result, "7:00 PM");
    }

    [TestMethod]
    public void NullPreferences_DefaultsToWeekdays()
    {
        var result = _sut.BuildSchedulePrompt(BaseRequest());
        StringAssert.Contains(result, "Monday");
        StringAssert.Contains(result, "Tuesday");
        StringAssert.Contains(result, "Friday");
    }

    // ── Preferences: custom ────────────────────────────────────────────────

    [TestMethod]
    public void CustomPreferences_WorkHoursAppearInSystemRules()
    {
        var request = BaseRequest() with
        {
            Preferences = new WorkPreferencesDto(9, 17, ["Monday", "Wednesday", "Friday"])
        };

        var result = _sut.BuildSchedulePrompt(request);
        StringAssert.Contains(result, "9:00 AM");
        StringAssert.Contains(result, "5:00 PM");
    }

    [TestMethod]
    public void CustomPreferences_WorkDaysAppearInSystemRules()
    {
        var request = BaseRequest() with
        {
            Preferences = new WorkPreferencesDto(9, 17, ["Monday", "Wednesday", "Friday"])
        };

        var result = _sut.BuildSchedulePrompt(request);
        StringAssert.Contains(result, "Monday, Wednesday, Friday");
    }

    // ── FormatHour edge cases (exercised via preferences) ──────────────────

    [DataTestMethod]
    [DataRow(0,  "12:00 AM")]   // midnight
    [DataRow(1,  "1:00 AM")]
    [DataRow(8,  "8:00 AM")]
    [DataRow(12, "12:00 PM")]   // noon
    [DataRow(13, "1:00 PM")]
    [DataRow(19, "7:00 PM")]
    [DataRow(23, "11:00 PM")]
    public void FormatHour_ProducesCorrectLabel(int hour, string expected)
    {
        var request = BaseRequest() with
        {
            Preferences = new WorkPreferencesDto(hour, hour, ["Monday"])
        };

        var result = _sut.BuildSchedulePrompt(request);
        StringAssert.Contains(result, expected);
    }

    // ── Date formatting ────────────────────────────────────────────────────

    [TestMethod]
    public void CurrentDate_AppearsFormattedInPrompt()
    {
        var request = BaseRequest() with { CurrentDate = new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero) };
        var result = _sut.BuildSchedulePrompt(request);
        StringAssert.Contains(result, "April 14 2026");
    }
}
