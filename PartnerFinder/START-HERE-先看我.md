# Re-Teck Partner Finder — 交接包 / Handoff Package

> English index: `START-HERE.md`。本包每份文件都有中英兩版（英文版檔名為英文）。

這是一套 **.NET 8 網頁系統**，用來搜尋、整理、評分全球（目前先做美國）的
**IT 系統整合商 / 資料中心服務商 / 小型 AI 運算建置商**，做為 Re-Teck 的合作夥伴資料庫。

> 目前狀態：**MVP 可執行**，本機跑得起來、功能齊全。已含「自動網路搜尋（Brave API）」。

---

## 📂 這包裡面有什麼

```
PartnerFinder/
  START-HERE-先看我.md      ← 你正在看的這份（總覽 / 索引）
  README.md                 ← 系統功能總說明（英文，GitHub 會排版）
  docs/
    00-系統用途與目標夥伴.md ← 【先讀】這系統要找什麼公司、為什麼（業務導向）
    00-System-Purpose-and-Target-Partners.md ← same as above, English version
    01-開發需求.md          ← 原始開發需求規格（老闆／發起人給的）
    02-開發紀錄與決策.md    ← 做了什麼、為什麼這樣選、目前進度、待辦
    03-安裝步驟-給同事.md   ← 【非技術也能照做】怎麼裝、怎麼跑、手機怎麼連
    04-AI-接手開發說明.md   ← 【給工程師 / 給 AI】架構、慣例、如何繼續開發
  手機使用步驟.md           ← 電腦跑、手機同 Wi-Fi 連的白話步驟
  自動搜尋設定.md           ← 開啟自動網路搜尋（申請免費 Brave 金鑰）
  ...（其餘為程式原始碼）
```

---

## 👉 你該先看哪一份？

| 你的角色 | 先看 |
|---|---|
| **想懂這系統要找什麼公司、為什麼** | `docs/00-系統用途與目標夥伴.md` |
| **只想把它跑起來用** | `docs/03-安裝步驟-給同事.md` |
| **想了解這系統要做什麼** | `docs/01-開發需求.md` |
| **想知道目前做到哪、怎麼設計的** | `docs/02-開發紀錄與決策.md` |
| **工程師 / 要接手改程式** | `docs/04-AI-接手開發說明.md` + `README.md` |
| **要用 AI（如 Claude）繼續開發** | `docs/04-AI-接手開發說明.md`（最後有可直接貼給 AI 的說明） |

---

## ⚡ 30 秒快速跑起來

1. 電腦裝 **.NET 8 SDK**：<https://dotnet.microsoft.com/download/dotnet/8.0>
2. 進 `PartnerFinder` 資料夾，**Windows 雙擊 `run-on-lan.bat`**（Mac 執行 `run-on-lan.sh`）
3. 瀏覽器開 `http://localhost:5080`

詳細（含手機連線、自動搜尋設定）看 `docs/03-安裝步驟-給同事.md`。
