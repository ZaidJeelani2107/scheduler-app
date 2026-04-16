using SchedulerApp.Api.Configuration;
using SchedulerApp.Api.Endpoints;
using SchedulerApp.Api.Middleware;
using SchedulerApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection(GeminiOptions.SectionName));

builder.Services.AddHttpClient("gemini", client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(25);
});

builder.Services.AddSingleton<IPromptBuilder, PromptBuilder>();
builder.Services.AddScoped<IGeminiService, GeminiService>();

// Permissive CORS is intentional for local development — the API is consumed by a
// native MAUI app which does not use CORS at all (CORS is a browser concept).
// Restrict origins before exposing this API to any web client in production.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseCors();

app.MapHealthEndpoints();
app.MapScheduleEndpoints();

app.Run();

// Expose Program to WebApplicationFactory in tests
public partial class Program { }
