using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SchedulerApp.Api.Models;
using SchedulerApp.Api.Services;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace SchedulerApp.Api.Tests.Endpoints;

[TestClass]
public class ScheduleEndpointsTests
{
    private static WebApplicationFactory<Program> _factory = null!;
    private static Mock<IGeminiService> _mockGemini = null!;
    private HttpClient _client = null!;

    private static readonly ScheduleResultDto SampleResult = new(
        Title: "Team meeting",
        StartTime: new DateTime(2026, 4, 14, 10, 0, 0),
        EndTime: new DateTime(2026, 4, 14, 11, 0, 0),
        Reasoning: "Best available slot",
        ConflictDetected: false,
        Alternatives: [],
        EventType: "Task");

    private static readonly ScheduleRequest SampleRequest = new(
        UserInput: "Schedule a team meeting",
        CurrentDate: new DateTimeOffset(2026, 4, 14, 9, 0, 0, TimeSpan.Zero),
        Events: []);

    // ── Lifetime ─────────────────────────────────────────────────────────────

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _mockGemini = new Mock<IGeminiService>();
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services =>
                    services.AddSingleton(_mockGemini.Object)));
    }

    [ClassCleanup]
    public static void ClassCleanup() => _factory.Dispose();

    [TestInitialize]
    public void TestInit()
    {
        _client = _factory.CreateClient();
        _mockGemini.Invocations.Clear();
    }

    [TestCleanup]
    public void TestCleanup() => _client.Dispose();

    // ── Tests ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task GeminiSucceeds_Returns200WithSuccessBody()
    {
        _mockGemini.Setup(x => x.ScheduleAsync(It.IsAny<ScheduleRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(SampleResult);

        var response = await _client.PostAsJsonAsync("/api/schedule", SampleRequest);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsNotNull(body);
        Assert.IsTrue(body["success"]!.GetValue<bool>(), "success should be true");
        Assert.AreEqual("Team meeting", body["data"]!["title"]!.GetValue<string>());
    }

    [TestMethod]
    public async Task GeminiFails_Returns502WithErrorBody()
    {
        _mockGemini.Setup(x => x.ScheduleAsync(It.IsAny<ScheduleRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync((ScheduleResultDto?)null);

        var response = await _client.PostAsJsonAsync("/api/schedule", SampleRequest);

        Assert.AreEqual(HttpStatusCode.BadGateway, response.StatusCode);

        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsNotNull(body);
        Assert.IsFalse(body["success"]!.GetValue<bool>(), "success should be false");
        Assert.IsFalse(string.IsNullOrEmpty(body["error"]?.GetValue<string>()), "error message should be present");
    }

    // ── Input validation ─────────────────────────────────────────────────────

    [TestMethod]
    public async Task EmptyUserInput_Returns400WithErrorBody()
    {
        var request = SampleRequest with { UserInput = "" };
        var response = await _client.PostAsJsonAsync("/api/schedule", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsNotNull(body);
        Assert.IsFalse(body["success"]!.GetValue<bool>());
        Assert.IsFalse(string.IsNullOrEmpty(body["error"]?.GetValue<string>()));
    }

    [TestMethod]
    public async Task WhitespaceOnlyUserInput_Returns400()
    {
        var request = SampleRequest with { UserInput = "   " };
        var response = await _client.PostAsJsonAsync("/api/schedule", request);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task UserInputExceedsMaxLength_Returns400WithErrorBody()
    {
        var request = SampleRequest with { UserInput = new string('x', 501) };
        var response = await _client.PostAsJsonAsync("/api/schedule", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.IsFalse(body!["success"]!.GetValue<bool>());
        Assert.IsFalse(string.IsNullOrEmpty(body["error"]?.GetValue<string>()));
    }

    // ── Health endpoint ──────────────────────────────────────────────────────

    [TestMethod]
    public async Task HealthEndpoint_Returns200WithStatusOk()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual("ok", body!["status"]!.GetValue<string>());
    }

    [TestMethod]
    public async Task UserInputAtExactMaxLength_Returns200()
    {
        // Boundary test: 500 chars is valid, 501 is not
        _mockGemini.Setup(x => x.ScheduleAsync(It.IsAny<ScheduleRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(SampleResult);

        var request = SampleRequest with { UserInput = new string('x', 500) };
        var response = await _client.PostAsJsonAsync("/api/schedule", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
