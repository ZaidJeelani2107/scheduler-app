using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SchedulerApp.Data;
using SchedulerApp.Infrastructure;
using SchedulerApp.Services;
using SchedulerApp.ViewModels;
using SchedulerApp.Views;

namespace SchedulerApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // SQLite database — factory is singleton so SqliteCalendarStore (also singleton) can create
        // per-operation contexts safely without scoped-in-singleton lifetime issues
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "scheduler.db");
        builder.Services.AddDbContextFactory<SchedulerDbContext>(
            options => options.UseSqlite($"Data Source={dbPath}"),
            ServiceLifetime.Singleton);

        // Calendar store backed by SQLite
        builder.Services.AddSingleton<ICalendarStore, SqliteCalendarStore>();

        // UI thread dispatcher — abstracts MainThread so DayViewModel can be unit-tested
        builder.Services.AddSingleton<IUiDispatcher, MauiUiDispatcher>();

        // User preferences — singleton so settings persist across the session
        builder.Services.AddSingleton<IUserPreferencesService, UserPreferencesService>();

        // API service — HTTP client pointing to local backend
        builder.Services.AddHttpClient("scheduler", client =>
        {
            // Android emulator uses 10.0.2.2 to reach host machine localhost
            // iOS simulator can use localhost directly
#if ANDROID
            client.BaseAddress = new Uri("http://10.0.2.2:5000/");
#else
            client.BaseAddress = new Uri("http://localhost:5000/");
#endif
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        builder.Services.AddTransient<ISchedulerApiService, SchedulerApiService>();

        // ViewModels and Views
        builder.Services.AddTransient<DayViewModel>();
        builder.Services.AddTransient<DayView>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Ensure the SQLite schema exists on first launch
        var factory = app.Services.GetRequiredService<IDbContextFactory<SchedulerDbContext>>();
        using var ctx = factory.CreateDbContext();
        ctx.Database.EnsureCreated();

        return app;
    }
}
