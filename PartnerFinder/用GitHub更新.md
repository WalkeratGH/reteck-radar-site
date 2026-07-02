# 用 GitHub 下載與更新系統(取代下載 ZIP)

> English version: `Update-via-GitHub.md`

以後不用再下載 ZIP。用 Git 把程式「連結」到 GitHub,更新只要按一次
**Pull**,金鑰也只要貼一次、永遠不會被更新覆蓋。

推薦用 **GitHub Desktop**(圖形介面,不用打指令),最適合非工程背景。

---

## A. 第一次設定(只做一次,約 10 分鐘)

### 1. 安裝 GitHub Desktop
到 <https://desktop.github.com> 下載安裝 → 用你的 GitHub 帳號登入。

### 2. 下載(Clone)這個專案
- GitHub Desktop 上方選單:**File → Clone repository**
- 選 **WalkeratGH/reteck-radar-site**
  (若沒看到,點 **URL** 分頁,貼上
  `https://github.com/WalkeratGH/reteck-radar-site`)
- **Local path** 選一個你記得的資料夾,例如 `C:\Re-Teck`
- 按 **Clone**

### 3. 切換到開發分支 ⚠️ 重要
程式在一條專用分支上,不是預設分支。
- GitHub Desktop 上方中間的 **Current branch** 下拉
- 選 **`claude/partner-finder-mvp-dotnet-dbe7mj`**

現在你電腦上 `C:\Re-Teck\reteck-radar-site\PartnerFinder\` 就是完整程式。

### 4. 貼金鑰(只做一次)
1. 進到 `...\reteck-radar-site\PartnerFinder\` 資料夾
2. 找到 **`appsettings.Local.example.json`**,複製一份、
   把檔名改成 **`appsettings.Local.json`**(去掉 `.example`)
3. 用記事本打開 `appsettings.Local.json`,把 4 把金鑰貼進對應的
   `"ApiKey": ""` 引號中間:

   ```json
   {
     "Search":    { "Brave":     { "ApiKey": "貼 Brave 金鑰" } },
     "Ai":        { "Anthropic": { "ApiKey": "貼 Anthropic 金鑰", "Model": "claude-opus-4-8" } },
     "Contacts":  { "Hunter":    { "ApiKey": "貼 Hunter 金鑰" } },
     "Discovery": { "SamGov":    { "ApiKey": "貼 SAM.gov 金鑰" } }
   }
   ```
4. 存檔。

> 🔒 這個 `appsettings.Local.json` 被 Git 忽略,**不會上傳 GitHub、更新也不會覆蓋它**。
> 所以金鑰你**只要貼這一次**,以後都不用再重貼(這正是你要的效果)。

### 5. 啟動
進 `PartnerFinder\` 資料夾,雙擊 **`run-on-lan.bat`**,
瀏覽器開 <http://localhost:5080>。

---

## B. 以後要更新(每次只要 30 秒)

我在 GitHub 上更新程式後,你這樣拿到最新版:

1. 打開 **GitHub Desktop**
2. 確認 **Current branch** 還是 `claude/partner-finder-mvp-dotnet-dbe7mj`
3. 按上方的 **Pull origin**(若顯示 **Fetch origin**,先按它,再按 Pull)
4. 關掉舊的黑底視窗,重新雙擊 **`run-on-lan.bat`**

完成。金鑰不用動、資料庫(你已建檔的公司)也不會不見。

---

## 常見問題

**Q：更新後金鑰要重貼嗎?**
不用。金鑰在 `appsettings.Local.json`,Git 不會碰它。

**Q：我已建檔的公司資料會不會被更新蓋掉?**
不會。資料存在 `partnerfinder.db`,也被 Git 忽略,更新不影響。

**Q：Pull 時跳出 conflict / 衝突怎麼辦?**
理論上不會了(金鑰和資料庫都已隔離)。若真的發生,先別自己亂改,
把畫面截圖給我,我幫你處理。

**Q：一定要用 GitHub Desktop 嗎?會打指令的話呢?**
不一定。會用終端機的話:
```bash
git clone https://github.com/WalkeratGH/reteck-radar-site
cd reteck-radar-site
git checkout claude/partner-finder-mvp-dotnet-dbe7mj
# 之後更新:
git pull
```

---

## 用 ZIP vs 用 Git 差在哪?

| | 下載 ZIP(舊方式) | 用 Git(新方式) |
|---|---|---|
| 更新 | 每次重新下載、解壓、搬檔 | 按一下 Pull |
| 金鑰 | 每次都要重貼 | 只貼一次 |
| 已建檔資料 | 容易忘記搬、弄丟 | 一直留在原資料夾 |
