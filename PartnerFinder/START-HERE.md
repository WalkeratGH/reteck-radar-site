# Re-Teck Partner Finder — Handoff Package

> 中文版：`START-HERE-先看我.md`

A **.NET 8 web app** to search, catalog, and score global (US first) **IT System
Integrators / data center service vendors / small-AI-compute builders** as candidate
partners for Re-Teck.

> Status: **runnable MVP** — runs locally, full feature set, includes automatic
> web search (Brave API).

---

## What's in this package

```
PartnerFinder/
  START-HERE.md / START-HERE-先看我.md   ← index (EN / 中文)
  README.md                              ← full feature overview (English)
  docs/
    00-System-Purpose-and-Target-Partners.md / 00-系統用途與目標夥伴.md
    01-Requirements.md / 01-開發需求.md
    02-Development-Log-and-Decisions.md / 02-開發紀錄與決策.md
    03-Install-Guide-for-Colleagues.md / 03-安裝步驟-給同事.md
    04-Developer-and-AI-Handoff.md / 04-AI-接手開發說明.md
  Using-on-Phone.md / 手機使用步驟.md
  Web-Search-Setup.md / 自動搜尋設定.md
  ...(the rest is source code)
```
Every document has both an English and a Chinese version.

---

## Which one should I read first?

| Your role | Start with |
|---|---|
| **Understand what companies we look for & why** | `docs/00-System-Purpose-and-Target-Partners.md` |
| **Just run and use it** | `docs/03-Install-Guide-for-Colleagues.md` |
| **Understand what the system does** | `docs/01-Requirements.md` |
| **See current status & design decisions** | `docs/02-Development-Log-and-Decisions.md` |
| **Engineer / continue the code** | `docs/04-Developer-and-AI-Handoff.md` + `README.md` |
| **Use an AI (e.g. Claude) to continue dev** | `docs/04-Developer-and-AI-Handoff.md` (paste-ready prompt at the end) |

---

## 30-second quick start

1. Install **.NET 8 SDK**: <https://dotnet.microsoft.com/download/dotnet/8.0>
2. In the `PartnerFinder` folder, **double-click `run-on-lan.bat`** (macOS: run `run-on-lan.sh`)
3. Open `http://localhost:5080` in a browser

Details (phone access, web-search setup) in `docs/03-Install-Guide-for-Colleagues.md`.
