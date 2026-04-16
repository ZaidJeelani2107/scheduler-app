using Microsoft.Extensions.Logging;
using SchedulerApp.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SchedulerApp.Services;

public class SchedulerApiService(
    IHttpClientFactory httpClientFactory,
    IUserPreferencesService userPreferences,
    ILogger<SchedulerApiService> logger) : ISchedulerApiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc/>
    public async Task<ScheduleResult?> ScheduleAsync(
        string userInput,
        IReadOnlyList<CalendarEvent> currentEvents,
        IReadOnlyList<ExcludedSlot>? excludedSlots = null,
        CancellationToken ct = default)
    {
        var client = httpClientFactory.CreateClient("scheduler");

        var payload = new
        {
            userInput,
            currentDate = DateTimeOffset.Now, // Now (not UtcNow) — preserves the device's local UTC offset so the API knows the user's timezone
            events = currentEvents.Select(e => new
            {
                id = e.Id,
                title = e.Title,
                startTime = e.StartTime,
                endTime = e.EndTime,
                type = e.Type.ToString()
            }).ToList(),
            preferences = new
            {
                workStartHour = (int)userPreferences.WorkStartTime.TotalHours,
                workEndHour = (int)userPreferences.WorkEndTime.TotalHours,
                workDays = userPreferences.WorkDays.Select(d => d.ToString()).ToList()
            },
            excludedSlots = excludedSlots?.Select(s => new
            {
                startTime = s.StartTime,
                endTime = s.EndTime
            }).ToList()
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync("api/schedule", content, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "HTTP request to scheduler API failed");
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Scheduler API returned {StatusCode}: {Body}", response.StatusCode, errorBody);
            return null;
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        try
        {
            var envelope = JsonNode.Parse(body);
            var data = envelope?["data"]?.ToJsonString();
            if (string.IsNullOrWhiteSpace(data))
            {
                logger.LogWarning("Scheduler API response had no data field. Raw: {Body}", body);
                return null;
            }

            return JsonSerializer.Deserialize<ScheduleResult>(data, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse scheduler API response. Raw: {Body}", body);
            return null;
        }
    }
}
