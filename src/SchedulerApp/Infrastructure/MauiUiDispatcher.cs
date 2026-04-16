namespace SchedulerApp.Infrastructure;

public class MauiUiDispatcher : IUiDispatcher
{
    /// <inheritdoc/>
    public Task InvokeOnMainThreadAsync(Action action) =>
        MainThread.InvokeOnMainThreadAsync(action);
}
