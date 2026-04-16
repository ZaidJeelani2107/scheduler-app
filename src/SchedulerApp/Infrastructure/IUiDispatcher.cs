namespace SchedulerApp.Infrastructure;

/// <summary>Abstracts main-thread dispatch so DayViewModel can be unit-tested without a running MAUI platform.</summary>
public interface IUiDispatcher
{
    Task InvokeOnMainThreadAsync(Action action);
}
