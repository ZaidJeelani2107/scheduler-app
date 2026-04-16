namespace SchedulerApp.Api.Models;

/// <summary>Consistent JSON envelope returned by every endpoint.</summary>
public record ApiResponse<T>(bool Success, T? Data, string? Error = null);
