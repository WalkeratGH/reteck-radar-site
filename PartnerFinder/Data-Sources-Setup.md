# Data Sources Setup (Hunter.io contacts / SAM.gov discovery / brand directories)

> 中文版:`資料來源設定.md`

Beyond Brave search and AI research, the system has three more data channels.
All of them **start free**.

---

## 1. Hunter.io — auto-fills Contact Person / Title / Email

**What it does:** during AI research, if Email / Contact Person / Contact Title
are still empty, the system looks up the company domain's **publicly listed
contacts** on Hunter.io and fills them (empty fields only).

**Cost:** free plan ~25 searches/month; paid from ~US$34/month.

**Setup (3 min):**
1. Sign up free at <https://hunter.io>.
2. Copy your API key from the **API** page.
3. Paste into `appsettings.json`:
   ```json
   "Contacts": { "Hunter": { "ApiKey": "YOUR_KEY" } }
   ```
4. Restart. Settings → "Hunter.io contact finder" turns **Configured**.

Nothing else to do — it kicks in automatically during AI research.

---

## 2. SAM.gov — the "Discover" page (official US registry)

**What it does:** a new **Discover** page in the nav: pick a state (e.g. TX)
and an industry code (NAICS dropdown pre-loaded with IT codes) and it lists
companies from the **US federal-contractor registry** (name, city, website).
Hit **⚡** to file + AI-research one in a single click.

**Cost:** completely free (official US government API).

**Setup (5 min):**
1. Register a free account at <https://sam.gov>.
2. Log in → **Account Details** → generate an **API Key**.
3. Paste into `appsettings.json`:
   ```json
   "Discovery": { "SamGov": { "ApiKey": "YOUR_KEY" } }
   ```
4. Restart. Settings → "SAM.gov discovery" turns **Configured**.

**Tip:** state + `541512 (Computer Systems Design)` is the best-matching combo;
`423430 (Computer Equipment Wholesalers)` often includes leasing companies.

---

## 3. Official brand partner directories (no signup needed)

The **Keyword Generator** page now has an "Official Partner Directories" card
linking to the Microsoft / NVIDIA / Dell / Cisco / HPE / Supermicro partner
directories plus CRN's Top-500 lists.

**How to use:** open a directory → filter to United States + your solution
area → copy an interesting company's **website URL** → paste it into
**Web Search** → hit **⚡ File + AI research**. (If an official link moves,
use the "search" fallback button next to it.)

---

## Key summary (appsettings.json)

| Feature | Config key | Where to get it | Cost |
|---|---|---|---|
| Automatic web search | `Search:Brave:ApiKey` | brave.com/search/api | $5 free credit/month (~1000 searches) |
| AI research & auto-fill | `Ai:Anthropic:ApiKey` | console.anthropic.com | ~$0.02–0.1 per company |
| Contact enrichment | `Contacts:Hunter:ApiKey` | hunter.io | free 25/month |
| SAM.gov discovery | `Discovery:SamGov:ApiKey` | sam.gov | free |

> 💡 **Preferred place for keys:** paste them into `appsettings.Local.json`
> (copy it from `appsettings.Local.example.json`) rather than `appsettings.json`.
> That file is Git-ignored, so keys survive `git pull` updates and are never
> uploaded. See `Update-via-GitHub.md` / `用GitHub更新.md`.
>
> 🔒 As always: never upload or share a file that contains real keys.
