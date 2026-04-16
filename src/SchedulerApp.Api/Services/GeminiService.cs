using Microsoft.Extensions.Options;
using SchedulerApp.Api.Configuration;
using SchedulerApp.Api.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SchedulerApp.Api.Services;

public interface IGeminiService
{
    Task<ScheduleResultDto?> ScheduleAsync(ScheduleRequest request, CancellationToken ct = default);
}

public class GeminiService(
    IHttpClientFactory httpClientFactory,
    IPromptBuilder promptBuilder,
    IOptions<GeminiOptions> options,
    ILogger<GeminiService> logger) : IGeminiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<ScheduleResultDto?> ScheduleAsync(ScheduleRequest request, CancellationToken ct = default)
    {
        var gemini = options.Value;

        if (string.IsNullOrWhiteSpace(gemini.ApiKey))
            throw new InvalidOperationException("Gemini:ApiKey is not configured.");

        var apiKey = gemini.ApiKey;
        var maxOutputTokens = gemini.MaxOutputTokens;

        var prompt = promptBuilder.BuildSchedulePrompt(request);

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = 0.2,
                maxOutputTokens,
                thinkingConfig = new { thinkingBudget = 0 }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var client = httpClientFactory.CreateClient("gemini");
        var url = $"v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(url, content, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HTTP call to Gemini failed");
            return null;
        }

        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Gemini returned {StatusCode}: {Body}", response.StatusCode, responseBody);
            return null;
        }

        try
        {
            var geminiResponse = JsonNode.Parse(responseBody);
            var text = geminiResponse?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogWarning("Gemini response had no text content. Raw: {Body}", responseBody);
                return null;
            }

            return JsonSerializer.Deserialize<ScheduleResultDto>(text, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse Gemini response. Raw: {Body}", responseBody);
            return null;
        }
    }
}
