namespace SchedulerApp.Api.Configuration;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Raise if the JSON response is being truncated on large calendars.</summary>
    public int MaxOutputTokens { get; set; } = 8192;
}
