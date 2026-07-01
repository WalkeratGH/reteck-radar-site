# 04 · Developer & AI Handoff

> 中文版：`04-AI-接手開發說明.md`

## Overview

- **Purpose:** a search-and-scoring database for IT / AI Infrastructure system
  integrators / data center vendors / small-AI-compute builders.
- **Stack:** .NET 8, ASP.NET Core MVC, EF Core, SQLite, Bootstrap 5, ClosedXML.
- **Status:** runnable MVP; runs locally / on LAN. See `02-Development-Log-and-Decisions.md`.

## Project structure

```
PartnerFinder/
  Program.cs                 Startup, DI registration, auto-migrate + seed on boot
  appsettings.json           SQLite connection string, Brave key (Search:Brave:ApiKey)
  Models/
    Partner.cs               The single core entity (all fields)
    Enums.cs                 PartnerStatus / RecommendedLevel / ManualReviewStatus
    ViewModels.cs            Dashboard & partner-list view models
  Data/
    AppDbContext.cs          EF Core DbContext
    PartnerOptions.cs        Service-category dropdown list (edit here)
    DbSeeder.cs              First-run sample data
  Services/
    ScoringService.cs        100-pt scoring model (each item commented)
    KeywordGeneratorService.cs   Keyword generation (no API)
    ExportService.cs         CSV + Excel export (single column source)
    DuplicateDetectionService.cs Name + host duplicate detection
    Connectors.cs            Future connector INTERFACES + no-op impls
    BraveWebSearchConnector.cs   Brave implementation of IWebSearchConnector
  Controllers/               Home, Partners, Keyword, WebSearch, Qualification, Export, Settings
  Views/                     Razor + Bootstrap
  Migrations/                EF Core migration (InitialCreate)
```

## Build / run / test

```bash
cd PartnerFinder
dotnet build
dotnet run --urls "http://0.0.0.0:5080"     # LAN-reachable; local: http://localhost:5080
```
- On startup `Program.cs` runs `db.Database.Migrate()` (auto-creates/upgrades the DB) and
  seeds sample data when the DB is empty.
- The DB file `partnerfinder.db` is gitignored (not versioned).

## Changing the EF schema (when adding fields)

```bash
dotnet tool install --global dotnet-ef --version 8.0.11   # once
dotnet ef migrations add <Name>
# applied automatically next start; or: dotnet ef database update
```

## Conventions / design notes

- **Scoring** is centralized in `ScoringService.Score()`, returning per-item `ScoreLine`s
  for the detail page; `Apply()` writes Total / AiScore / Level back onto the entity.
  Always call `Apply()` before saving.
- **Export columns** are defined once in `ExportService.Columns`, shared by CSV and Excel.
- **Enums stored as strings** via `HasConversion<string>()` in `AppDbContext`, so the raw
  `.db` is human-readable.
- **Config-driven connectors:** `IsConfigured` is true only when a key is present; the UI
  reflects enabled/disabled from that.

## How to add a "future connector" (e.g. SerpAPI)

1. The interface already exists in `Services/Connectors.cs` (e.g. `ISerpApiConnector`).
2. Add an implementation class (mirror `BraveWebSearchConnector`), reading the key from
   `IConfiguration`.
3. In `Program.cs`, replace the `Null...` registration with your implementation
   (if it needs HttpClient: `builder.Services.AddHttpClient<Interface, Impl>()`).
4. The `Settings` page reflects `IsConfigured` automatically.

## Current backlog (suggested priority)

1. **Phone/LAN access:** environment issue (firewall / IP / Wi-Fi isolation), not code.
   See doc `02`.
2. **Cloud deployment** (for off-site access): add a `Dockerfile`, deploy to Render /
   Railway / Azure App Service; for persistent, multi-user data switch to PostgreSQL
   (EF `UseNpgsql`).
3. Implement remaining connectors: SerpAPI, Microsoft Partner directory, AI Summary
   generator, Weekly market radar.
4. AI Summary: could call the Claude API (Anthropic) to generate `AiSummary` /
   `AiInfrastructureSummary`.

---

## 📋 Paste-ready prompt for an AI (e.g. Claude / Claude Code)

> Copy the whole block below and give it to an AI to take over development:

```
You are taking over an existing .NET 8 project, "Re-Teck Partner Finder".

Background: an ASP.NET Core MVC + EF Core + SQLite + Bootstrap web app to search,
record, and score global (US first) IT system integrators / data center vendors /
small-AI-compute builders as a partner database. It is a runnable MVP.

The project is in the PartnerFinder/ folder. Read these before making changes:
- PartnerFinder/docs/01-Requirements.md
- PartnerFinder/docs/02-Development-Log-and-Decisions.md
- PartnerFinder/docs/04-Developer-and-AI-Handoff.md
- PartnerFinder/README.md

Key facts and conventions:
- Core entity is Models/Partner.cs (single table); scoring is in
  Services/ScoringService.cs (100-pt model; Apply() writes score/level back onto the
  entity — always call it before saving).
- Export columns are defined once in Services/ExportService.cs (Columns).
- Connectors are config-driven: IsConfigured is true only with a key. Brave is
  implemented (BraveWebSearchConnector); the rest (SerpAPI/MS Partner/AI Summary/
  Weekly radar) have interfaces in Services/Connectors.cs but are no-op.
- DB uses EF Core migrations; the app auto-migrates + seeds on startup (Program.cs).
  Schema changes require a new migration.

Build & run:
  cd PartnerFinder && dotnet build && dotnet run --urls "http://0.0.0.0:5080"

Please follow: keep the code simple and readable for non-technical maintainers;
match existing style and naming; add a migration for any EF schema change; never
commit API keys or partnerfinder.db.

What I want next: <write your request here, e.g. implement the SerpAPI connector /
add a cloud-deploy Dockerfile / auto-generate AI Summary via the Claude API /
convert to a multi-user PostgreSQL version>. Give me a short plan before implementing.
```
