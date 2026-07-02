# 02 · Development Log & Decisions

> 中文版：`02-開發紀錄與決策.md`

## One-line summary

Delivered a **runnable .NET 8 MVP**: ASP.NET Core MVC + EF Core + SQLite + Bootstrap,
fully local and offline-capable; all requested features implemented, plus an added
"automatic web search (Brave API)".

---

## Technology choices & rationale

| Item | Decision | Why |
|---|---|---|
| Framework | ASP.NET Core **MVC** (not Blazor) | More intuitive for non-expert maintainers; lots of examples |
| Database | **SQLite** (single `.db` file) | No DB server to install; copy the file to back up |
| ORM | **EF Core** + Migrations | Auto-creates tables; schema changes are versioned |
| UI | **Bootstrap 5 (bundled in `wwwroot/lib`)** | Works offline, no CDN dependency |
| Excel export | **ClosedXML** | Produces native `.xlsx`, reliable |
| Paid APIs | **None** at first; interfaces reserved | Follows "local MVP first" requirement |
| Auto search | Added **Brave Search API** (user's choice) | Free quota is enough; needs only one key |

---

## Completed features

- **Data model:** single `Partner` entity with all required fields (basic / IT-DC / AI / eval).
- **CRUD:** create, edit, delete, detail; list page with search + filters (text / country /
  category / level / AI-only).
- **Scoring engine** (`Services/ScoringService.cs`): 100-pt model; scores and A/B/C grade
  computed automatically on save; the detail page shows a per-item breakdown.
- **Search Keyword Generator:** produces copyable search phrases from
  Country/City/Service Type/AI (no API).
- **Automatic Web Search (Web Search page):** Brave API; type a query → list results →
  one-click "File as partner" pre-fills the Create form.
- **AI Research & Auto-fill (Claude API)** (`ClaudeAiSummaryService`, official Anthropic C# SDK):
  deep-reads the company website (multiple pages) plus Brave search snippets, then **auto-fills
  the record** — capabilities auto-checked (on only, never off), brand partnerships
  (None→Registered), certifications/city/country/email/phone/main services (empty fields only),
  leasing/SME signals — and re-scores automatically. The AI Summary lists everything auto-applied
  for human verification. Entry points: Detail-page button, batch button on the Partner List
  (max 5 per click), and "⚡ File + AI research" on Web Search (one click from search hit to a
  filled, scored record). Key at `Ai:Anthropic:ApiKey`; model configurable (default
  claude-opus-4-8). See `AI-Summary-Setup.md`.
- **Targeting fields**: new booleans `EquipmentLeasingSignal` (equipment leasing / HaaS) and
  `SmeFocusSignal` (SME customer base) — migration `AddTargetingSignals`; AI research sets them;
  included in CSV/Excel export.
- **Company-info auto-enrichment** (`WebsiteInfoService`): clicking "File as partner"
  fetches the company's website (homepage + contact page, max 2 requests) and extracts
  company name, email, phone, LinkedIn, US city/state, and a service description to
  pre-fill the form; missing fields stay blank for manual entry. Best-effort — only
  publicly visible details can be found.
- **Dashboard:** totals, A/B/C counts, by-country, AI-capable count, missing-contact count,
  pending-review count.
- **Export:** CSV (UTF-8 BOM so Chinese opens cleanly in Excel) and Excel `.xlsx`.
- **Duplicate detection** (`DuplicateDetectionService`): normalized company name + website
  host; warns of likely duplicates on create.
- **Future-connector interfaces** (`Services/Connectors.cs`): Web Search / SerpAPI /
  MS Partner / AI Summary / Weekly market radar all have interfaces; Web Search is
  implemented (Brave).
- **DB init:** EF Core `InitialCreate` migration; on startup the app auto-migrates and
  seeds 3 US sample records (`Data/DbSeeder.cs`).
- **Verification:** actual build (0 warnings / 0 errors), actual run; all 8 pages + exports
  return HTTP 200; scoring correct (sample "Summit" = 86 → Level A); Brave path verified
  with a dummy key (request/headers correct).

---

## Enhancements added during user interaction

1. **Phone viewing:** provided phone-view screenshots so the user could see the result first.
2. **LAN run:** added `run-on-lan.bat` / `run-on-lan.sh` binding the app to `0.0.0.0:5080`
   so a phone on the same Wi-Fi can connect (default `dotnet run` binds localhost only).
3. **HTTPS tweak:** `Program.cs` forces HTTPS only in Production, so local http on a LAN
   isn't redirected and the console stays clean.
4. **Auto search:** implemented **Brave Search API** connector + Web Search page + setup
   guide, per the user's choice.

---

## ⚠️ Known / open items

- **Phone can't reach the local app (in progress):** the user's PC opens `localhost:5080`
  fine, but the LAN IP `192.168.100.209:5080` doesn't connect. Steps tried:
  1. Confirm startup shows `Now listening on: http://0.0.0.0:5080`.
  2. Allow port 5080 in Windows Firewall (must run PowerShell **as Administrator**, else
     "Access denied"):
     `New-NetFirewallRule -DisplayName "PartnerFinder 5080" -Direction Inbound -Protocol TCP -LocalPort 5080 -Action Allow`
  3. **Suspicion:** `192.168.100.209` may not be the real NIC's IP (VMware/VirtualBox/WSL/VPN
     create fake 192.168.x.x). Use `ipconfig` and take the IPv4 under the actual Wi-Fi /
     Ethernet adapter.
  4. Other causes: phone on a different Wi-Fi, or Wi-Fi "AP isolation".
  → This is an **environment issue, not a code issue** (the app is fully fine locally).

- **Brave key not yet obtained:** code is ready; the user just needs to get a free key and
  paste it into `appsettings.json` (see `Web-Search-Setup.md`).

---

## Not yet implemented (reserved for next phase)

- SerpAPI / Microsoft Partner directory / AI Summary generator / Weekly market radar
  (interfaces exist, currently no-op).
- Off-site access without keeping a PC on → needs cloud deployment (Azure App Service /
  Render / Railway, etc.).
- Data is currently local SQLite; multi-user concurrent use needs a central DB
  (e.g. PostgreSQL) and deployment.

---

## Git / source location

- Repo: `WalkeratGH/reteck-radar-site`
- Branch: `claude/partner-finder-mvp-dotnet-dbe7mj`
- Code lives in the `PartnerFinder/` folder.
