# Enable "AI Summary" (Claude API)

> 中文版:`AI摘要設定.md`

One button on the **Partner Detail** page makes the AI read the company's record
(plus its website) and write an **evaluation summary, AI-infrastructure summary,
suggested capabilities to verify, and a suggested next action**.
Requires an **Anthropic (Claude) API key** — a one-time ~5-minute setup.

---

## Cost expectations

- New accounts usually get a **one-time free trial credit**; afterwards it's
  prepaid credit (minimum ~US$5).
- Each click costs roughly **US$0.01–0.02** (default model Claude Opus 4.8), or
  ~$0.003 on the cheapest model (Haiku).

## Step 1: Get a key

1. Sign up / log in at <https://console.anthropic.com>.
2. Complete phone verification (trial credit is usually granted here; if asked
   to top up, the minimum is $5).
3. **API Keys** → **Create Key** → copy the key (starts with `sk-ant-...`,
   **shown only once** — save it immediately).

## Step 2: Paste it into the system

1. Open `PartnerFinder/appsettings.json` and find:
   ```json
   "Ai": {
     "Anthropic": {
       "ApiKey": "",
       "Model": "claude-opus-4-8"
     }
   }
   ```
2. Paste the key **between the quotes** of `ApiKey`, save.
3. (Optional) Set `Model` to `"claude-haiku-4-5"` for the lowest cost.

> 🔒 The key is a password: **never** upload the filled-in `appsettings.json`
> to GitHub or share it.

## Step 3: Restart → use

1. Close the console window and re-run `run-on-lan.bat`.
2. **Settings** → "AI Summary generator" should turn green **Configured**.
3. Open any partner's **Detail** page → right-hand "**AI Summary**" card →
   **Generate AI Summary** (takes ~10–30 seconds).
4. Review the output: the summary is stored in the AI Summary field;
   "Suggested capabilities to verify" are for a **human** to confirm and check
   manually — the AI never checks capability boxes itself, so it cannot
   silently change scoring inputs.

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| Card shows "Not set up yet" | Key not pasted correctly, or app not restarted |
| "Invalid Anthropic API key (401)" | Wrong/revoked key — check the Console |
| "Rate limited (429)" | Too many calls — wait a minute and retry |
| Want a different model | Change `Model` and restart (`claude-haiku-4-5`, `claude-sonnet-5`, …) |
