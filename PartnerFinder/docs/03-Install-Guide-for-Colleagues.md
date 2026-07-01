# 03 · Install Guide (for colleagues · non-technical friendly)

> 中文版：`03-安裝步驟-給同事.md`
> All steps from zero to usable, in one place. Just follow along.

---

## A. Prerequisite (once per PC)

1. Install **.NET 8 SDK** (free, from Microsoft):
   <https://dotnet.microsoft.com/download/dotnet/8.0>
   - Download the **SDK** (not just the Runtime), Windows or macOS, install with defaults.
2. Verify: open Command Prompt / Terminal, run `dotnet --version`, see `8.x.x`.

---

## B. Get the code

**Option 1 (easiest):** unzip the ZIP you received; it contains the `PartnerFinder` folder.

**Option 2 (GitHub):** on the branch page, click green **Code → Download ZIP**:
```
https://github.com/WalkeratGH/reteck-radar-site/tree/claude/partner-finder-mvp-dotnet-dbe7mj
```

---

## C. Start the system

- **Windows:** open the `PartnerFinder` folder, **double-click `run-on-lan.bat`**.
- **macOS:** in Terminal, `chmod +x run-on-lan.sh` (first time), then `./run-on-lan.sh`.

Success = `Now listening on: http://0.0.0.0:5080`. **Keep the window open** — closing it
stops the system.

> Manual command (same effect): from `PartnerFinder`, run
> `dotnet run --urls "http://0.0.0.0:5080"`

First launch auto-creates the database and seeds 3 US sample records.

---

## D. Open it on THIS computer

Browser: `http://localhost:5080` → the Dashboard appears.

---

## E. Open it on a PHONE (same Wi-Fi)

1. Find the PC IP: Windows `ipconfig` → **IPv4 Address** under **Wireless LAN adapter
   Wi-Fi** (or Ethernet), like `192.168.x.x`.
   - ⚠️ Ignore VMware / VirtualBox / WSL / VPN adapter IPs — those won't work.
2. Put the phone on the **same Wi-Fi** as the PC (turn off mobile data).
3. Phone browser: `http://<PC-IP>:5080` (e.g. `http://192.168.1.23:5080`).

**If the phone can't connect (timeout):** almost always the Windows firewall. Open
PowerShell **as Administrator** (Start → type powershell → right-click → *Run as
administrator*; title bar must read "Administrator"), paste and Enter:
```powershell
New-NetFirewallRule -DisplayName "PartnerFinder 5080" -Direction Inbound -Protocol TCP -LocalPort 5080 -Action Allow
```
Still failing → check "same Wi-Fi", "no AP isolation (try a home Wi-Fi or phone
hotspot)", and "right NIC IP". See `Using-on-Phone.md`.

---

## F. Enable "Automatic Web Search" (optional, needs a free key)

To let the system search the web for companies, get a free **Brave Search API** key:
1. Sign up at <https://brave.com/search/api/>, pick the **Free** plan, copy the key (like `BSAxxxx…`).
2. Open `PartnerFinder/appsettings.json` in a text editor and paste:
   ```json
   "Search": { "Brave": { "ApiKey": "paste-here" } }
   ```
3. Restart the system. The **Web Search** page can now search and file with one click.
   (Full guide: `Web-Search-Setup.md`)

> 🔒 **Do not** upload the filled-in `appsettings.json` to GitHub or share it — the key is
> like a password.

---

## G. Where's the data / backup / reset

- Data lives in `PartnerFinder/partnerfinder.db` (this PC, not the cloud).
- **Back up:** copy `partnerfinder.db`.
- **Reset:** stop the app, delete `partnerfinder.db`; next launch rebuilds it + sample data.

---

## Quick troubleshooting

| Symptom | Fix |
|---|---|
| Double-click window flashes and closes | .NET 8 SDK not installed; or red error at the end — screenshot for an engineer |
| `localhost:5080` won't open | System not running; confirm the console shows `Now listening` |
| Phone connection times out | Firewall (see E), different Wi-Fi, or wrong NIC IP |
| Auto search shows Not configured | Key not pasted right or not restarted (see F) |
| Want off-site access | Needs cloud deployment; see `04-Developer-and-AI-Handoff.md` |
