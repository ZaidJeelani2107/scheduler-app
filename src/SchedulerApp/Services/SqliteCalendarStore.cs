using Microsoft.EntityFrameworkCore;
using SchedulerApp.Data;
using SchedulerApp.Models;

namespace SchedulerApp.Services;

public class SqliteCalendarStore(IDbContextFactory<SchedulerDbContext> dbFactory) : ICalendarStore
{
    /// <inheritdoc/>
    public event EventHandler? CalendarChanged;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CalendarEvent>> GetEventsForDayAsync(DateOnly date)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd   = date.ToDateTime(TimeOnly.MaxValue);

        using var ctx = await dbFactory.CreateDbContextAsync();
        return await ctx.CalendarEvents
            .AsNoTracking()
            .Where(e => e.StartTime >= dayStart && e.StartTime <= dayEnd)
            .OrderBy(e => e.StartTime)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CalendarEvent>> GetEventsForRangeAsync(DateOnly from, DateOnly to)
    {
        var rangeStart = from.ToDateTime(TimeOnly.MinValue);
        var rangeEnd   = to.ToDateTime(TimeOnly.MaxValue);

        using var ctx = await dbFactory.CreateDbContextAsync();
        return await ctx.CalendarEvents
            .AsNoTracking()
            .Where(e => e.StartTime >= rangeStart && e.StartTime <= rangeEnd)
            .OrderBy(e => e.StartTime)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task AddEventAsync(CalendarEvent evt)
    {
        using var ctx = await dbFactory.CreateDbContextAsync();
        ctx.CalendarEvents.Add(evt);
        await ctx.SaveChangesAsync();
        CalendarChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task UpdateEventAsync(string id, string title, DateTime startTime, DateTime endTime)
    {
        using var ctx = await dbFactory.CreateDbContextAsync();
        var evt = await ctx.CalendarEvents.FindAsync(id);
        if (evt is null) return;

        evt.Title     = title;
        evt.StartTime = startTime;
        evt.EndTime   = endTime;
        await ctx.SaveChangesAsync();
        CalendarChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public async Task RemoveEventAsync(string id)
    {
        using var ctx = await dbFactory.CreateDbContextAsync();
        var evt = await ctx.CalendarEvents.FindAsync(id);
        if (evt is null) return;

        ctx.CalendarEvents.Remove(evt);
        await ctx.SaveChangesAsync();
        CalendarChanged?.Invoke(this, EventArgs.Empty);
    }
}
