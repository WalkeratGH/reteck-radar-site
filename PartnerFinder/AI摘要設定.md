# 開啟「AI Summary 自動評估摘要」(Claude API)

> English version: `AI-Summary-Setup.md`

在 **Partner Detail(夥伴詳細頁)** 按一顆按鈕,AI 就會讀取這家公司的資料(含公司官網),
自動寫出:**評估摘要、AI 基礎設施摘要、建議補勾的能力、建議的下一步行動**。
需要一把 **Anthropic(Claude)API 金鑰**,約 5 分鐘設定一次即可。

---

## 費用概念(先安心)

- 新帳號通常有**一次性免費試用額度**;之後採預付儲值(最低約 US$5)。
- 每按一次約 **US$0.01~0.02**(預設 Claude Opus 4.8);換最省的 Haiku 模型約 $0.003。
- 一個月評估幾十家,成本不到一杯咖啡。

## 第 1 步:申請金鑰

1. 到 <https://console.anthropic.com> 用 Email 註冊、登入。
2. 完成手機驗證(通常此時發放試用額度;若要求儲值,最低 $5)。
3. 左側 **API Keys** → **Create Key** → 複製金鑰(`sk-ant-...` 開頭,**只顯示一次**,馬上存好)。

## 第 2 步:貼進系統

1. 用記事本打開 `PartnerFinder/appsettings.json`,找到:
   ```json
   "Ai": {
     "Anthropic": {
       "ApiKey": "",
       "Model": "claude-opus-4-8"
     }
   }
   ```
2. 把金鑰貼進 `ApiKey` 的**雙引號中間**,存檔。
3. (可選)想省成本,把 `Model` 改成 `"claude-haiku-4-5"`。

> 🔒 金鑰等於密碼:**不要**把填了金鑰的 `appsettings.json` 上傳 GitHub 或外傳。

## 第 3 步:重新啟動 → 使用

1. 關掉黑底視窗,重跑 `run-on-lan.bat`。
2. **Settings** 頁的「AI Summary generator」應變成綠色 **Configured**。
3. 打開任何一家公司的 **Detail** 頁 → 右側「**AI Summary**」卡片 → 按
   **Generate AI Summary**(約 10~30 秒)。
4. 產生後請**人工看過**:摘要存入 AI Summary 欄位;「Suggested capabilities to verify」
   是 AI 建議你確認後**手動**補勾的能力(系統不會自動勾選,避免影響評分)。

---

## 常見問題

| 狀況 | 解法 |
|---|---|
| 卡片顯示「Not set up yet」 | 金鑰沒貼對或沒重啟 |
| 「Invalid Anthropic API key (401)」 | 金鑰錯誤/已撤銷,回 Console 確認 |
| 「Rate limited (429)」 | 呼叫太頻繁,等一分鐘再按 |
| 想換模型 | 改 `Model` 後重啟(可用 `claude-haiku-4-5`、`claude-sonnet-5` 等) |
