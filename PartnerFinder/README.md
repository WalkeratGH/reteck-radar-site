# Global IT SI / AI Infrastructure Partner Finder (MVP)

A local .NET web application to **search, catalog, and score global IT System
Integrators, Data Center service vendors, and small-AI-computing builders** as
candidate partners for Re-Teck (Microsoft / Dell / Nokia / Data Center / ITAD /
AI Server / Edge AI / GPU Workstation projects).

> This is the first-phase MVP. It runs entirely on your machine with a local
> SQLite database and **does not connect to any paid API**. Current search focus:
> **United States**.

---

## What it does

| Page | Purpose |
|---|---|
| **Dashboard** | Totals, A/B/C level counts, partners by country, AI-capable count, missing-contact count, pending-review count |
| **Partners** | List, search & filter (text / country / category / level / AI-only) |
| **Add / Edit Partner** | Full data-entry form (basic info, IT/DC capabilities, AI capabilities, evaluation) |
| **Partner Detail** | Full profile + automatic **score breakdown** + quick review-status update + one-click **AI Summary** (Claude API, free-tier key; see `AI-Summary-Setup.md`) |
| **Keyword Generator** | Turns Country / City / Service Type / AI-required into copy-paste Google/LinkedIn search phrases (no API) |
| **Web Search** | Live web search (Brave API, free key required); one-click "File as partner" **auto-fills** company name, email, phone, LinkedIn, city and description from the company's website |
| **Qualification Scores** | Ranked scoreboard of every partner (General /70 + AI /30 = /100) |
| **Export** | Download the whole database as **CSV** or **Excel (.xlsx)** |
| **Settings** | Environment info, future-connector status, service-category list |

---

## Tech stack

- **.NET 8** / C#
- **ASP.NET Core MVC**
- **Entity Framework Core** + **SQLite** (file `partnerfinder.db`, created automatically)
- **Bootstrap 5** (bundled locally under `wwwroot/lib`, works offline)
- **ClosedXML** for Excel export

---

## How to run

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
cd PartnerFinder
dotnet run
```

Then open the URL shown in the console (e.g. `http://localhost:5xxx`).

On first launch the app automatically:
1. creates the SQLite database and applies the migration, and
2. seeds a few **sample US partners** so the dashboard is not empty.

The database file (`partnerfinder.db`) lives next to the app and is **not**
committed to git. Delete it to start from a clean slate.

---

## Scoring model (100 points)

**General IT / SI capability — 70**

| Item | Points |
|---|---:|
| Data Center / Enterprise IT experience | 20 |
| Local coverage capability | 10 |
| Brand partnership (Microsoft / Dell / Cisco / HPE) | 10 |
| Certification and compliance (ISO 9001 / 27001 …) | 10 |
| Service scope match | 10 |
| Contact availability | 5 |
| Website / public information credibility | 5 |

**AI Infrastructure capability — 30**

| Item | Points |
|---|---:|
| AI server / GPU workstation deployment | 10 |
| Edge AI / local AI deployment | 8 |
| Linux / Docker / Kubernetes | 6 |
| Cooling / power planning (small AI computing) | 4 |
| NVIDIA / AMD GPU ecosystem | 2 |

**Grading:** `A = 80–100` (contact first) · `B = 60–79` (confirm further) ·
`C = below 60` (keep on hold).

Scores are recalculated automatically every time a partner is saved
(see `Services/ScoringService.cs`).

---

## Project layout

```
PartnerFinder/
  Program.cs                 App startup, DI, auto-migrate + seed
  appsettings.json           SQLite connection string
  Models/
    Partner.cs               The single core entity (all fields)
    Enums.cs                 PartnerStatus / RecommendedLevel / ManualReviewStatus
    ViewModels.cs            Dashboard + Partner-list view models
  Data/
    AppDbContext.cs          EF Core context
    PartnerOptions.cs        Service-category dropdown list (edit here)
    DbSeeder.cs              First-run sample data
  Services/
    ScoringService.cs        100-point qualification model
    KeywordGeneratorService.cs   Search-phrase generator (no API)
    ExportService.cs         CSV + Excel export
    DuplicateDetectionService.cs Name/website duplicate warning
    Connectors.cs            FUTURE connectors (interfaces + no-op impls)
    BraveWebSearchConnector.cs   Live web search (Brave API)
    WebsiteInfoService.cs    Auto-fill partner details from a company website
  Controllers/               Home, Partners, Keyword, Qualification, Export, Settings
  Views/                     Razor + Bootstrap UI
  Migrations/                EF Core migration (InitialCreate)
```

---

## Reserved for later phases

The architecture already defines interfaces (in `Services/Connectors.cs`) so these
can be added without rewrites:

- Web Search API connector
- SerpAPI connector
- Microsoft Partner directory connector
- LinkedIn manual research workflow
- AI Summary generator
- Weekly market radar

Already active in the MVP: **automatic scoring engine** and **duplicate company
detection** (by normalized company name + website host).

To enable a connector, implement its interface and swap the registration in
`Program.cs` (currently pointing at the `Null…` no-op implementations).

---

## For maintainers (non-technical friendly)

- **Add a service category:** edit the list in `Data/PartnerOptions.cs`.
- **Change scoring weights:** edit `Services/ScoringService.cs` (each item is commented).
- **Change sample data:** edit `Data/DbSeeder.cs`.
- **Start fresh:** stop the app and delete `partnerfinder.db`.
