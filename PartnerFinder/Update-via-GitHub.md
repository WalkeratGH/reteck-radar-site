# Download & update via GitHub (instead of ZIP downloads)

> 中文版:`用GitHub更新.md`

No more ZIP downloads. Link the app to GitHub with Git; updating is then a
single **Pull**, and you paste your API keys **once** — updates never overwrite
them.

Recommended: **GitHub Desktop** (a graphical app, no commands) — best for
non-developers.

---

## A. One-time setup (~10 minutes, done once)

### 1. Install GitHub Desktop
Download from <https://desktop.github.com>, install, and sign in with your
GitHub account.

### 2. Clone this project
- Menu: **File → Clone repository**
- Pick **WalkeratGH/reteck-radar-site**
  (if it's not listed, open the **URL** tab and paste
  `https://github.com/WalkeratGH/reteck-radar-site`)
- Set **Local path** to a folder you'll remember, e.g. `C:\Re-Teck`
- Click **Clone**

### 3. Switch to the development branch ⚠️ Important
The code lives on a dedicated branch, not the default one.
- Use the **Current branch** dropdown (top center)
- Select **`claude/partner-finder-mvp-dotnet-dbe7mj`**

The full app is now in
`C:\Re-Teck\reteck-radar-site\PartnerFinder\`.

### 4. Paste your keys (once)
1. Open the `...\reteck-radar-site\PartnerFinder\` folder.
2. Find **`appsettings.Local.example.json`**, make a copy, and rename the copy
   to **`appsettings.Local.json`** (drop the `.example`).
3. Open `appsettings.Local.json` in Notepad and paste your 4 keys between the
   quotes of each `"ApiKey": ""`:

   ```json
   {
     "Search":    { "Brave":     { "ApiKey": "YOUR_BRAVE_KEY" } },
     "Ai":        { "Anthropic": { "ApiKey": "YOUR_ANTHROPIC_KEY", "Model": "claude-opus-4-8" } },
     "Contacts":  { "Hunter":    { "ApiKey": "YOUR_HUNTER_KEY" } },
     "Discovery": { "SamGov":    { "ApiKey": "YOUR_SAMGOV_KEY" } }
   }
   ```
4. Save.

> 🔒 `appsettings.Local.json` is ignored by Git — **never uploaded, never
> overwritten by updates**. So you paste the keys **only this once**.

### 5. Run
Open the `PartnerFinder\` folder, double-click **`run-on-lan.bat`**, and browse
to <http://localhost:5080>.

---

## B. Updating later (30 seconds each time)

After I push updates to GitHub, get the latest like this:

1. Open **GitHub Desktop**
2. Confirm **Current branch** is still `claude/partner-finder-mvp-dotnet-dbe7mj`
3. Click **Pull origin** (if it says **Fetch origin**, click that first, then Pull)
4. Close the old console window and double-click **`run-on-lan.bat`** again

Done. Keys stay put; your filed companies (the database) stay put.

---

## FAQ

**Do I re-paste keys after updating?** No — they live in
`appsettings.Local.json`, which Git never touches.

**Will my filed companies be overwritten?** No — data is in
`partnerfinder.db`, which is also Git-ignored.

**A "conflict" appears on Pull — what now?** It shouldn't happen anymore (keys
and DB are isolated). If it does, don't hand-edit — screenshot it and send it to
me.

**Prefer the command line?**
```bash
git clone https://github.com/WalkeratGH/reteck-radar-site
cd reteck-radar-site
git checkout claude/partner-finder-mvp-dotnet-dbe7mj
# later, to update:
git pull
```

---

## ZIP vs Git

| | ZIP download (old) | Git (new) |
|---|---|---|
| Update | re-download, unzip, move files | one Pull click |
| Keys | re-paste every time | paste once |
| Filed data | easy to forget/lose | stays in the same folder |
