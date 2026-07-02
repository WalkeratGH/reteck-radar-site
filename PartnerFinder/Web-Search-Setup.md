# Enable "Automatic Web Search" (Brave Search API)

> 中文版：`自動搜尋設定.md`

The **Web Search** page can search the web automatically and list candidate companies,
each fileable with one click. It needs a **free Brave Search API key**. One-time setup,
about 5 minutes.

---

## Step 1: Get a free key

1. Open <https://brave.com/search/api/> and click **Get started** (goes to the API dashboard).
2. Sign up / log in with email.
3. Choose the **Free** plan (may be called "Free" or "Data for Search – Free"; free quota
   is roughly 2,000 queries/month, 1 query/sec).
   - Some flows ask you to **add a card for verification** — the free plan is **not
     charged**, it's just verification.
4. On the **API Keys** (or "Subscriptions") page, **copy the key** (a long alphanumeric
   string, e.g. `BSAxxxxxxxxxxxxxxxxxxxxxx`).

## Step 2: Paste the key into the system

1. In the `PartnerFinder` folder, open **`appsettings.json`** in a text editor.
2. Find:
   ```json
   "Search": {
     "Brave": {
       "ApiKey": ""
     }
   }
   ```
3. Paste the key **between the quotes** (don't touch other symbols):
   ```json
   "Search": {
     "Brave": {
       "ApiKey": "BSAxxxxxxxxxxxxxxxxxxxxxx"
     }
   }
   ```
4. Save.

> 🔒 Security: this key is like a password. **Do not** upload the filled-in
> `appsettings.json` to GitHub or share it.

## Step 3: Restart the system

- Close the console window, then re-run **`run-on-lan.bat`** (macOS: `run-on-lan.sh`).

## Step 4: Confirm it's on

- Open the **Settings** page: the "Web Search API connector" row should change from grey
  **Not configured** to green **Configured**.
- Open the **Web Search** page, type e.g. `GPU workstation builder Austin Texas`, click
  **Search** — results appear, each with **+ File as partner**.

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| Settings still shows Not configured | Key not pasted correctly or not restarted. Check quotes, restart |
| "Search failed: 401 / 422" | Invalid/inactive key; verify the key and that the plan is active |
| "429" | Free quota used up (2,000/month); resets next month, or upgrade |
| Company names look odd | The system pre-fills the website host as the name; edit it on the Create page |

---

## Suggested workflow

1. In **Keyword Generator**, produce phrases → click **Auto search** on a phrase (jumps
   straight to Web Search).
2. In **Web Search**, click **+ File as partner** on promising companies. The system
   **auto-reads the company's website** and pre-fills what it finds (company name,
   email, phone, LinkedIn, city, service description); a banner lists what was found.
3. On the Create page **verify the auto-filled data**, add contacts, check capability
   boxes → save; the system scores and grades (A/B/C) automatically.

> Auto-fill is best-effort: only details published on the company's site can be found;
> anything missing is left blank for manual entry.
