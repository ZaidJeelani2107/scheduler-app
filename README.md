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

## Screenshots

- **IOS**

- Home screen:

<img width="1470" height="956" alt="home-screen" src="https://github.com/user-attachments/assets/507415bb-5620-49ad-b333-4e6b5f23aaac" />

---

- Schedule meeting screen:

<img width="1470" height="956" alt="schedule-meetings-tab" src="https://github.com/user-attachments/assets/aeafeede-9efd-490c-9fac-f895d8d09d19" />

---

- Loading screen:

<img width="1470" height="956" alt="ai-loading-screen" src="https://github.com/user-attachments/assets/d32cb127-edd9-4a60-a5b3-eacc01ac7b75" />

---

- Edit meetings screen:

<img width="1470" height="956" alt="edit-meetings-tab" src="https://github.com/user-attachments/assets/f2fdaf73-1a6b-4eb9-93b2-53716ec8aa6d" />

---

- Work preferences screen:

<img width="1470" height="956" alt="work-preferences-tab" src="https://github.com/user-attachments/assets/6094a686-2387-431e-b509-d2bbcd4b0b46" />

---

- **Android**

- Home screen:

  <img width="1080" height="2424" alt="home-screen" src="https://github.com/user-attachments/assets/c121831d-1e8d-4d5d-bb0f-25ba511ef308" />

---

- Schedule meetings screen:

  <img width="1080" height="2424" alt="android-schedule-meetings" src="https://github.com/user-attachments/assets/b0054e4b-42d8-414f-82a3-68c346405d1a" />

---

- Edit meetings screen:

   <img width="1080" height="2424" alt="android-edit-meetings" src="https://github.com/user-attachments/assets/e83dd5f3-9148-4981-924c-680225b0baf5" />

---

- Work preferences screen:

  <img width="1080" height="2424" alt="work-preferences" src="https://github.com/user-attachments/assets/59b4f338-9bf0-4837-bc06-e1403db07e4a" />

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
