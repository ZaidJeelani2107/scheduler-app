using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SchedulerApp.Api.Configuration;
using SchedulerApp.Api.Models;
using SchedulerApp.Api.Services;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SchedulerApp.Api.Tests.Services;

[TestClass]
public class GeminiServiceTests
{
    private static readonly ScheduleRequest Request = new(
        "Schedule a meeting",
        new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero),
        []);

    // ── Helpers ─────────────────────────────────────────────────────────────

    private sealed class FakeHttpMessageHandler(HttpResponseMessage? response = null, bool throws = false) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (throws) throw new HttpRequestException("Simulated network failure");
            return Task.FromResult(response!);
        }
    }

    private static GeminiService MakeService(
        HttpResponseMessage? httpResponse = null,
        bool httpThrows = false,
        string apiKey = "test-key")
    {
        var handler = new FakeHttpMessageHandler(httpResponse, httpThrows);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://generativelanguage.googleapis.com/") };

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient("gemini")).Returns(client);

        var promptBuilder = new Mock<IPromptBuilder>();
        promptBuilder.Setup(x => x.BuildSchedulePrompt(It.IsAny<ScheduleRequest>())).Returns("test prompt");

        var options = Options.Create(new GeminiOptions { ApiKey = apiKey, MaxOutputTokens = 1000 });

        return new GeminiService(factory.Object, promptBuilder.Object, options, NullLogger<GeminiService>.Instance);
    }

    private static HttpResponseMessage OkJson(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    // Wraps innerText as the value of candidates[0].content.parts[0].text
    private static string GeminiEnvelope(string innerText)
    {
        var escapedText = JsonSerializer.Serialize(innerText);
        return $$"""
            {
              "candidates": [{
                "content": {
                  "parts": [{ "text": {{escapedText}} }]
                }
              }]
            }
            """;
    }

    private const string ValidResultJson =
        """{"title":"Team meeting","startTime":"2026-04-14T10:00:00","endTime":"2026-04-14T11:00:00","reasoning":"Best slot","conflictDetected":false,"alternatives":[],"eventType":"Task"}""";

    // ── Tests ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task MissingApiKey_ThrowsInvalidOperationException()
    {
        var sut = MakeService(apiKey: "");
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => sut.ScheduleAsync(Request));
    }

    [TestMethod]
    public async Task HttpException_ReturnsNull()
    {
        var sut = MakeService(httpThrows: true);
        var result = await sut.ScheduleAsync(Request);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task NonSuccessStatusCode_ReturnsNull()
    {
        var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal error", Encoding.UTF8, "text/plain")
        };
        var sut = MakeService(httpResponse: errorResponse);
        var result = await sut.ScheduleAsync(Request);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task EmptyTextInCandidates_ReturnsNull()
    {
        var sut = MakeService(httpResponse: OkJson(GeminiEnvelope("")));
        var result = await sut.ScheduleAsync(Request);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task MissingCandidatesKey_ReturnsNull()
    {
        var sut = MakeService(httpResponse: OkJson("{}"));
        var result = await sut.ScheduleAsync(Request);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task EmptyCandidatesArray_ReturnsNull()
    {
        // {"candidates": []} — index [0] resolves to null via null-safe navigation
        var sut = MakeService(httpResponse: OkJson("""{"candidates": []}"""));
        var result = await sut.ScheduleAsync(Request);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task MalformedResultJson_ReturnsNull()
    {
        // The Gemini wrapper is valid but the inner text is not a ScheduleResultDto —
        // exercises the deserialization catch block.
        var sut = MakeService(httpResponse: OkJson(GeminiEnvelope("this is not json {")));
        var result = await sut.ScheduleAsync(Request);
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task ValidResponse_ReturnsScheduleResultDto()
    {
        var sut = MakeService(httpResponse: OkJson(GeminiEnvelope(ValidResultJson)));
        var result = await sut.ScheduleAsync(Request);
        Assert.IsNotNull(result);
        Assert.AreEqual("Team meeting", result.Title);
    }

    [TestMethod]
    public async Task CancelledToken_ThrowsTaskCanceledException()
    {
        // FakeHttpMessageHandler calls ThrowIfCancellationRequested before PostAsync completes.
        // GeminiService re-throws the cancellation (catch OperationCanceledException { throw })
        // rather than absorbing it — HttpClient wraps it as TaskCanceledException (a subclass).
        var sut = MakeService(httpResponse: OkJson(GeminiEnvelope(ValidResultJson)));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() =>
            sut.ScheduleAsync(Request, cts.Token));
    }

    [TestMethod]
    public async Task ValidResponse_DeserializesAllFields()
    {
        // Validates the full JSON-to-ScheduleResultDto contract so that schema drift is caught early.
        const string fullJson = """
            {
                "title": "Focus block",
                "startTime": "2026-04-14T14:00:00",
                "endTime":   "2026-04-14T16:00:00",
                "reasoning": "Conflict-free 2-hour gap in the afternoon",
                "conflictDetected": true,
                "alternatives": [
                    { "startTime": "2026-04-14T10:00:00", "endTime": "2026-04-14T12:00:00", "label": "Morning slot" }
                ],
                "eventType": "FocusBlock"
            }
            """;

        var sut = MakeService(httpResponse: OkJson(GeminiEnvelope(fullJson)));
        var result = await sut.ScheduleAsync(Request);

        Assert.IsNotNull(result);
        Assert.AreEqual("Focus block", result.Title);
        Assert.AreEqual(new DateTime(2026, 4, 14, 14, 0, 0), result.StartTime);
        Assert.AreEqual(new DateTime(2026, 4, 14, 16, 0, 0), result.EndTime);
        Assert.AreEqual("Conflict-free 2-hour gap in the afternoon", result.Reasoning);
        Assert.IsTrue(result.ConflictDetected);
        Assert.AreEqual(1, result.Alternatives.Count);
        Assert.AreEqual("Morning slot", result.Alternatives[0].Label);
        Assert.AreEqual(new DateTime(2026, 4, 14, 10, 0, 0), result.Alternatives[0].StartTime);
        Assert.AreEqual("FocusBlock", result.EventType);
    }
}
