# SchedulerApp

An AI-powered calendar scheduling app for iOS and Android. Describe what you want to schedule in plain English and Gemini 2.5 Flash finds the best available slot, detects conflicts, and suggests alternatives — all before writing anything to your calendar.

## Stack

- **Frontend** — .NET MAUI 10 (iOS, Android, Mac Catalyst)
- **Backend** — ASP.NET Core 10 Minimal API
- **Database** — SQLite via EF Core
- **AI** — Google Gemini 2.5 Flash

## How It Works

1. User types a natural-language request — *"block 2 hours for deep work tomorrow morning"*
2. The app sends the request + next 7 days of calendar events to the API
3. The API builds a prompt and calls Gemini, which returns a suggested time slot
4. User sees a confirmation card with the suggested time and Gemini's reasoning
5. Accept → event saved to SQLite. Reschedule → rejected slot is excluded and Gemini tries again

## Setup

**1. Set your Gemini API key**
```bash
dotnet user-secrets set "Gemini:ApiKey" "YOUR_KEY" --project src/SchedulerApp.Api
```

**2. Run the API**
```bash
dotnet run --project src/SchedulerApp.Api
```

**3. Run the app**
```bash
dotnet run --project src/SchedulerApp -f net10.0-ios
dotnet run --project src/SchedulerApp -f net10.0-android
```

## Tests

```bash
dotnet test tests/SchedulerApp.Api.Tests
```

36 tests covering `PromptBuilder`, `GeminiService`, and the `/api/schedule` endpoint via `WebApplicationFactory`.
