# SchedulerApp

An AI-powered day-scheduler mobile app built as a portfolio project. Users type natural-language requests ("block two hours for deep work tomorrow morning") and the app uses Google Gemini 2.5 Flash to find an optimal slot, detect conflicts, and present a confirmation card before committing the event to a local SQLite calendar.

---

## Architecture Overview

```
┌──────────────────────────────────────────────────────┐
│                  .NET MAUI App                       │
│  (iOS / Android / Mac Catalyst)                      │
│                                                      │
│  DayView ──► DayViewModel ──► SchedulerApiService    │
│                  │                    │              │
│                  │              HTTP POST            │
│                  │           /api/schedule           │
│                  │                    │              │
│              ICalendarStore           ▼              │
│           (SqliteCalendarStore)  ┌────────────────┐  │
│                  │               │ ASP.NET Core   │  │
│              scheduler.db        │ Minimal API    │  │
│              (AppDataDirectory)  │                │  │
│                                  │ GeminiService  │  │
│                                  │      │         │  │
│                                  │  HTTPS POST    │  │
│                                  │  Gemini REST   │  │
│                                  │      │         │  │
│                                  │  Gemini 2.5    │  │
│                                  │  Flash         │  │
│                                  └────────────────┘  │
└──────────────────────────────────────────────────────┘
```

The API key never leaves the backend. The MAUI app talks only to `localhost:5000`; the API proxies to Gemini.

---

## Repo Structure

```
scheduler-app/
├── src/
│   ├── SchedulerApp/                   .NET MAUI app (iOS + Android)
│   │   ├── Data/
│   │   │   └── SchedulerDbContext.cs   EF Core context + CalendarEvent entity config
│   │   ├── Models/
│   │   │   ├── CalendarEvent.cs        Core domain model + EventType enum
│   │   │   ├── ExcludedSlot.cs         Value type for rejected time slots (reschedule flow)
│   │   │   └── ScheduleResult.cs       Deserialized API response + AlternativeSlot
│   │   ├── Services/
│   │   │   ├── ICalendarStore.cs       Async CRUD interface for calendar events
│   │   │   ├── SqliteCalendarStore.cs  EF Core + SQLite implementation (Singleton)
│   │   │   ├── ISchedulerApiService.cs HTTP client interface to the backend
│   │   │   ├── SchedulerApiService.cs  POST /api/schedule; maps CalendarEvent → DTO
│   │   │   ├── IUserPreferencesService.cs  Work hours / work days interface
│   │   │   └── UserPreferencesService.cs   MAUI Preferences-backed implementation
│   │   ├── ViewModels/
│   │   │   ├── DayViewModel.cs         Primary VM — AI input, confirmation, edit/delete
│   │   │   ├── CalendarEventViewModel.cs   Timeline layout math (TopOffset, Height)
│   │   │   ├── ScheduleResultViewModel.cs  Wraps ScheduleResult for the confirmation card
│   │   │   └── SettingsViewModel.cs    Work-hours/days settings page
│   │   ├── Views/
│   │   │   ├── DayView.xaml/.cs        Main screen — timeline, AI input bar, overlays
│   │   │   ├── EventBlockView.xaml     Reusable coloured event block
│   │   │   ├── ConfirmationCardView.xaml   AI suggestion card (Accept / Dismiss / Reschedule)
│   │   │   └── SettingsPage.xaml       Work hours + work days configuration
│   │   ├── Resources/Styles/
│   │   │   └── Colors.xaml             18 semantic color resources (AppAccent, AppError, …)
│   │   └── MauiProgram.cs              DI setup, SQLite connection, EnsureCreated()
│   │
│   └── SchedulerApp.Api/               ASP.NET Core 10 Minimal API
│       ├── Configuration/
│       │   └── GeminiOptions.cs        Strongly-typed IOptions<T> (ApiKey, MaxOutputTokens)
│       ├── Endpoints/
│       │   ├── ScheduleEndpoints.cs    POST /api/schedule — validates input, calls Gemini
│       │   └── HealthEndpoints.cs      GET /api/health — liveness probe
│       ├── Middleware/
│       │   └── GlobalExceptionMiddleware.cs  Catches unhandled exceptions → 500 JSON
│       ├── Models/
│       │   ├── ScheduleRequest.cs      Request body record (UserInput, CurrentDate, Events, …)
│       │   ├── ScheduleResultDto.cs    Response record (Title, StartTime, Reasoning, EventType, …)
│       │   ├── CalendarEventDto.cs     Calendar event projection sent with each request
│       │   ├── ExcludedSlotDto.cs      Rejected slot sent during reschedule
│       │   ├── WorkPreferencesDto.cs   User work-hours/days forwarded from MAUI to the prompt
│       │   └── ApiResponse.cs          Generic envelope { success, data, error }
│       ├── Services/
│       │   ├── GeminiService.cs        Builds HTTP request, calls Gemini REST, parses JSON
│       │   └── PromptBuilder.cs        Assembles the system-prompt + calendar context
│       └── Program.cs                  DI registration, middleware, endpoint mapping
│
└── tests/
    └── SchedulerApp.Api.Tests/         MSTest project (27 tests)
        ├── Services/
        │   ├── PromptBuilderTests.cs   13 tests — all prompt-assembly branches
        │   └── GeminiServiceTests.cs   6 tests — FakeHttpMessageHandler error paths
        └── Endpoints/
            └── ScheduleEndpointsTests.cs  2 integration tests — WebApplicationFactory
```

---

## Prerequisites

| Requirement | Version |
|-------------|---------|
| .NET SDK | 10.0 |
| .NET MAUI workload | `dotnet workload install maui` |
| Google Gemini API key | [aistudio.google.com](https://aistudio.google.com) |
| Xcode (iOS builds) | 16+ |
| Android SDK (Android builds) | API 35+ |

---

## Setup

### 1 — Clone and restore

```bash
git clone <repo-url>
cd scheduler-app
dotnet restore
```

### 2 — Set the Gemini API key

The key is stored in .NET User Secrets and never committed to source control.

```bash
dotnet user-secrets set "Gemini:ApiKey" "YOUR_KEY_HERE" \
  --project src/SchedulerApp.Api
```

`appsettings.Development.json` intentionally holds an empty string for this key — the real value is injected at runtime from user secrets.

### 3 — Run the API

```bash
dotnet run --project src/SchedulerApp.Api
# Listening on http://localhost:5000
```

### 4 — Run the MAUI app

```bash
# iOS Simulator (Mac only)
dotnet run --project src/SchedulerApp/SchedulerApp.csproj -f net10.0-ios

# Android emulator
dotnet run --project src/SchedulerApp/SchedulerApp.csproj -f net10.0-android

# Mac Catalyst (compile check / quick run)
dotnet run --project src/SchedulerApp/SchedulerApp.csproj -f net10.0-maccatalyst
```

> **Android emulator note:** The app uses `10.0.2.2:5000` instead of `localhost:5000` when built for Android because the emulator's `localhost` is the emulator itself, not the host machine. This is handled by an `#if ANDROID` conditional in `SchedulerApiService.cs`.

---

## How the AI Scheduling Flow Works

```
1. User types a natural-language request in the AI input bar
   e.g. "Schedule a 1-hour design review for tomorrow afternoon"

2. DayViewModel.SubmitAiInputCommand fires
   - Fetches the next 7 days of events from SqliteCalendarStore
   - Reads work-hour/day preferences from UserPreferencesService

3. SchedulerApiService.ScheduleAsync POST /api/schedule
   - Serialises: userInput, currentDate (DateTimeOffset), events[], preferences, excludedSlots[]

4. API: ScheduleEndpoints validates the request (non-empty, ≤ 500 chars)

5. API: GeminiService.ScheduleAsync
   - PromptBuilder assembles a system-prompt containing:
       • Work-hour rules (e.g. "8:00 AM to 7:00 PM, Monday–Friday")
       • Today's date + the next 7 days of calendar events, sorted by start time
       • Any excluded slots from previous reschedule attempts
       • The user's raw input string
   - Sends POST to Gemini 2.5 Flash (generativelanguage.googleapis.com)
     with responseMimeType=application/json and thinkingBudget=0
   - Parses candidates[0].content.parts[0].text as ScheduleResultDto JSON

6. API returns { success, data: ScheduleResultDto } or 502 on Gemini failure

7. MAUI: DayViewModel sets PendingConfirmation = new ScheduleResultViewModel(result)

8. DayView.xaml.cs detects the PropertyChanged event and animates the
   ConfirmationCardView slide-up overlay (TranslateToAsync, 300ms CubicOut)

9. User sees: event title, suggested time, Gemini's reasoning, and (if conflict) 3 alternatives

10. Accept → CalendarEvent written to SQLite; timeline refreshes; overlay slides down
    Reschedule → rejected slot added to excludedSlots[]; flow repeats from step 3
    Dismiss → PendingConfirmation = null; overlay slides down; excludedSlots cleared
```

---

## Known Gotchas

| Gotcha | Detail |
|--------|--------|
| Android emulator URL | Use `10.0.2.2:5000` not `localhost:5000`. The `#if ANDROID` block in `SchedulerApiService.cs` handles this automatically. |
| `thinkingConfig` placement | Must be **inside** `generationConfig`, not at the request root. Placing it at the root produces a 400 "Unknown name" error from Gemini. |
| `thinkingBudget: 0` is required | Gemini 2.5 Flash uses thinking tokens by default; without `thinkingBudget: 0` the response may be truncated by `MAX_TOKENS`. |
| `TranslateToAsync` not `TranslateTo` | `TranslateTo` is obsolete in .NET 10 MAUI. Always use `TranslateToAsync` (identical signature). |
| Overlay `IsVisible` is not data-bound | Confirmation and edit overlays are toggled entirely from `DayView.xaml.cs` via `PropertyChanged`. Adding `IsVisible` bindings to XAML will break the slide-up animation. |
| `CanSaveEdit` guards both change detection and time ordering | The Save button stays disabled if nothing changed **or** if `EndTime ≤ StartTime`. There is no redundant validation in `SaveEdit` itself. |
| `DividerDark` and `InputBgDark` are both `#2A2A2A` | Intentionally separate semantic resources. Do not merge them — they may diverge in future design iterations. |
| `TimelineDividerCount` is derived | Do not hardcode `12`. `DayView.xaml.cs` computes `_timelineDividerCount` from `CalendarEventViewModel.PixelsPerMinute` and the VM's `TimelineStartHour`/`TimelineEndHour`. Update only the VM constants if the visible range changes. |
| JDK validation warning on CLI builds | `dotnet build -f net10.0-android` may emit an XA5300 error on some machines. Use `-f net10.0-maccatalyst` to verify C# compiles cleanly without the Android toolchain. |
| `EnsureCreated()` is synchronous | MAUI's `CreateMauiApp()` is synchronous; the DB schema is initialised before the app runs. This is accepted tech debt — async init would require restructuring startup into page `OnAppearing`. |

---

## Running the Tests

```bash
dotnet test tests/SchedulerApp.Api.Tests
```

The suite uses **MSTest** (not xUnit or NUnit). 27 tests across three files:

| File | Tests | What's covered |
|------|-------|----------------|
| `PromptBuilderTests.cs` | 13 | Empty calendar message, event sorting, excluded-slot section, preference defaults/custom values, `FormatHour` edge cases (midnight, noon, PM hours), date formatting |
| `GeminiServiceTests.cs` | 6 | Missing API key exception, HTTP network failure, non-2xx status code, empty `candidates` text, missing `candidates` key, happy-path deserialization |
| `ScheduleEndpointsTests.cs` | 2 | 200 OK with success body, 502 Bad Gateway with error body (uses `WebApplicationFactory<Program>` with shared factory lifetime) |

All 27 tests pass. `SqliteCalendarStore` and `DayViewModel` are not yet covered (in-memory SQLite and MAUI VM tests are listed as future work).
