using SchedulerApp.Api.Models;
using SchedulerApp.Api.Services;

namespace SchedulerApp.Api.Endpoints;

public static class ScheduleEndpoints
{
    public static IEndpointRouteBuilder MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/schedule", async (
            ScheduleRequest request,
            IGeminiService geminiService,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger(nameof(ScheduleEndpoints));

            if (string.IsNullOrWhiteSpace(request.UserInput))
                return Results.BadRequest(new ApiResponse<ScheduleResultDto>(false, null, "UserInput is required."));

            if (request.UserInput.Length > 500)
                return Results.BadRequest(new ApiResponse<ScheduleResultDto>(false, null, "UserInput must be 500 characters or fewer."));

            logger.LogInformation("Schedule request: {Input}", request.UserInput);

            var result = await geminiService.ScheduleAsync(request, ct);

            return result is null
                ? Results.Json(
                    new ApiResponse<ScheduleResultDto>(false, null, "AI scheduling failed. Please try again."),
                    statusCode: StatusCodes.Status502BadGateway)
                : Results.Ok(new ApiResponse<ScheduleResultDto>(true, result));
        })
        .WithName("Schedule")
        .Produces<ApiResponse<ScheduleResultDto>>(StatusCodes.Status200OK)
        .Produces<ApiResponse<ScheduleResultDto>>(StatusCodes.Status400BadRequest)
        .Produces<ApiResponse<ScheduleResultDto>>(StatusCodes.Status502BadGateway);

        return app;
    }
}
