# JobSniffing AI ⚡

Full-stack job search platform built with **ASP.NET Core Web API**, **Entity Framework Core (SQLite)**, **Google Gemini API**, and **Angular** (TypeScript + Tailwind CSS).

---

## 🏛️ Architecture Overview

```
                            ┌──────────────────────────────────────────────┐
                            │        JobSniffing AI Dashboard              │
                            │   (Tailwind CSS, Standalone Components)      │
                            └──────────────────────┬───────────────────────┘
                                                   │ HTTP REST
                                                   ▼
                            ┌──────────────────────────────────────────────┐
                            │           JobsController (.NET 10)           │
                            └──────────────┬───────────────────────────────┘
                                           │
                    ┌──────────────────────┴──────────────────────┐
                    ▼                                             ▼
        ┌───────────────────────┐                     ┌───────────────────────┐
        │     ApifyService      │                     │   AiMatchingService   │
        │  Multi-Actor Scraper  │                     │   Google Gemini API   │
        │(LinkedIn, Naukri,     │                     │ (Filter <= N hours,   │
        │     Instahyre)        │                     │  Score 0-100, Reason) │
        └───────────────────────┘                     └───────────────────────┘
                    │                                             │
                    └──────────────────────┬──────────────────────┘
                                           ▼
                            ┌──────────────────────────────────────────────┐
                            │        Single-Table SQLite Database          │
                            │             [JobSearchResults]               │
                            │               (pulsejob.db)                  │
                            └──────────────────────────────────────────────┘
```

---

## 🛠️ Tech Stack & Key Components

* **Backend**: ASP.NET Core Web API (C# .NET 10 / .NET 8 compatible)
* **ORM & Database**: Entity Framework Core with SQLite (`pulsejob.db`). Auto-created on startup.
* **Single Table Design**: Single table `JobSearchResults` storing batch timestamp, title, company, platform, direct URL, Gemini match score, match reason, and application toggle.
* **External Scraper Service**: `ApifyService` for parallel querying of LinkedIn, Naukri, and Instahyre actors with built-in resilience.
* **AI Intelligence Service**: `AiMatchingService` connecting to Google Gemini API (`gemini-2.5-flash` / `gemini-1.5-pro`) for candidate-to-job semantic scoring and freshness verification.
* **Frontend**: Modern Angular 19+ standalone application styled with Tailwind CSS in dark Vercel aesthetic with accessible feedback patterns (`:user-invalid` styling and reduced-motion compliant spinners).

---

## 🚀 Getting Started

### 1. Prerequisites
* [.NET 8.0, 9.0, or 10.0 SDK](https://dotnet.microsoft.com/download)
* [Node.js](https://nodejs.org/) (v20+ or v22+) & npm

---

### 2. Configure API Keys in `appsettings.json`

Open `PulseJob.Api/appsettings.json` and set your credentials:

```json
{
  "ConnectionStrings": {
    "PulseJobDb": "Data Source=pulsejob.db"
  },
  "Apify": {
    "ApiToken": "YOUR_APIFY_TOKEN",
    "LinkedInActorId": "valig~linkedin-jobs-scraper",
    "NaukriActorId": "blackfalcondata~naukri-jobs-feed",
    "InstahyreActorId": "anchor~instahyre-jobs-scraper"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "Model": "gemini-2.5-flash"
  }
}
```

> **Note:** If `YOUR_APIFY_TOKEN` or `YOUR_GEMINI_API_KEY` are left as placeholders, Application operates with the dummy data without throwing any error.

---

### 3. Run the Backend API

From the root repository directory:

```bash
cd PulseJob.Api
dotnet run --launch-profile http
```

* API will start on: **`http://localhost:5247`**
* OpenAPI / Swagger documentation endpoint: **`http://localhost:5247/openapi/v1.json`**

---

### 4. Run the Angular Dashboard

In a separate terminal window:

```bash
cd pulsejob-ui
npm start
```

* Open your browser and navigate to: **`http://localhost:4200`**

---

## 📡 API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/jobs/search` | Scrapes jobs via Apify, processes through Gemini AI, persists to `pulsejob.db`, and returns results |
| `GET` | `/api/jobs?date=YYYY-MM-DD&limit=100` | Retrieves saved jobs strictly filtered by the selected date (UTC) |
| `GET` | `/api/jobs/dates` | Retrieves all available distinct dates on which searches/scrapes occurred |
| `PATCH` | `/api/jobs/{id}/applied` | Toggles the application status (`isApplied: true/false`) in SQLite |

---

## 💾 SQLite Single Table Schema (`JobSearchResults`)

| Column | Type | Description |
|---|---|---|
| `Id` | `INTEGER` (PK, AutoIncrement) | Unique record ID |
| `ScrapeTimestamp` | `DATETIME` | Search execution batch timestamp |
| `JobTitle` | `TEXT` | Extracted job title |
| `Company` | `TEXT` | Company name |
| `Platform` | `TEXT` | `LinkedIn`, `Naukri`, or `Instahyre` |
| `DirectLink` | `TEXT` | Direct link to the job posting |
| `MatchScore` | `INTEGER` | AI match score (0 to 100) |
| `MatchReason` | `TEXT` | Gemini match explanation |
| `PostedDate` | `DATETIME` | Job publication timestamp |
| `IsApplied` | `INTEGER` (Boolean) | Application progress status |

