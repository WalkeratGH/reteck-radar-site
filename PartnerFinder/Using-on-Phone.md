# Using Partner Finder on your phone (PC runs it · phone on same Wi-Fi)

> 中文版：`手機使用步驟.md`

Plain steps. The idea: **the PC runs the system → the phone joins the same Wi-Fi →
open the PC's address in the phone browser.** Free, no account needed. Downsides: the
PC must stay on, and the phone must be on the same Wi-Fi (won't work off-site).

---

## Step 0: Install .NET 8 on the PC (once)

- Go to <https://dotnet.microsoft.com/download/dotnet/8.0>
- Download **.NET 8 SDK** (Windows or macOS), install with defaults.

## Step 1: Get the code onto the PC

Easiest: on the GitHub branch page, click green **Code → Download ZIP**, then unzip.
```
https://github.com/WalkeratGH/reteck-radar-site/tree/claude/partner-finder-mvp-dotnet-dbe7mj
```
(Or with git: `git clone`, then `git checkout claude/partner-finder-mvp-dotnet-dbe7mj`.)
The app is in the **`PartnerFinder`** folder.

## Step 2: Start the system

- **Windows:** open the `PartnerFinder` folder, **double-click `run-on-lan.bat`**.
- **macOS:** in Terminal, `chmod +x run-on-lan.sh` (first time) then `./run-on-lan.sh`.

Success looks like `Now listening on: http://0.0.0.0:5080`.
**Keep that window open** — closing it stops the system.

> Manual command (same effect): from the `PartnerFinder` folder run
> `dotnet run --urls "http://0.0.0.0:5080"`

## Step 3: Find the PC's IP address

- **Windows:** open Command Prompt, run `ipconfig`, find the **IPv4 Address** (looks
  like `192.168.x.x`).
- **macOS:** System Settings → Wi-Fi → Details → IP Address (or `ipconfig getifaddr en0`).

Say it's `192.168.1.23`.

## Step 4: Open it on the phone

1. Connect the phone to the **same Wi-Fi as the PC**.
2. In the phone browser, enter: `http://192.168.1.23:5080` (use your Step 3 IP; keep `:5080`).
3. The Dashboard appears; you can add partners, generate keywords, export, etc.

💡 App-like feel: use "Add to Home Screen" in the phone browser.

---

## If it won't connect

| Symptom | Fix |
|---|---|
| Phone can't connect | Confirm phone & PC are on the **same Wi-Fi** (not mobile data) |
| Still can't connect (timeout) | PC **firewall** is likely blocking. On first run Windows shows an "allow access" prompt — click **Allow**; or open port 5080 (see below) |
| `https` won't open | Use **`http://`**, not https |
| Forgot the IP | Re-run Step 3; a laptop's IP can change after switching Wi-Fi |

**Open port 5080 in Windows Firewall** (run PowerShell **as Administrator** — Start →
type powershell → right-click → *Run as administrator*; the title bar must say
"Administrator"):
```powershell
New-NetFirewallRule -DisplayName "PartnerFinder 5080" -Direction Inbound -Protocol TCP -LocalPort 5080 -Action Allow
```
Still stuck? The IP may belong to a virtual adapter (VMware/VirtualBox/WSL/VPN) — pick
the real Wi-Fi/Ethernet IPv4 from `ipconfig`. Some corporate/public Wi-Fi has "AP
isolation" that blocks device-to-device — test with a phone hotspot (PC joins the
hotspot too) or use a home Wi-Fi.

---

## Where is the data?

Data lives in a file on the PC: `PartnerFinder/partnerfinder.db`. It stays with **this
PC**, not the phone or cloud.
- Back up: copy `partnerfinder.db`.
- Start fresh: stop the app, delete `partnerfinder.db`; it rebuilds with sample data
  next launch.

> Want off-site access without keeping a PC on? That requires a cloud deployment —
> see `docs/04-Developer-and-AI-Handoff.md`.
