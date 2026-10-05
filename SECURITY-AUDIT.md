# GeZi v2.0.0 安全與品質審查報告

審查日期：2026-10-05  
審查基準提交：`b3e5a65692e192f7170c69feca80f39da73a61d8`

## 審查範圍

- WPF 主程式與核心下載器原始碼
- Cookie／帳號／設定／下載歷史的本機儲存
- Python 登入橋接與內嵌 CPython/OpenSSL 執行環境
- 外部程序及 Windows Shell 啟動
- HTTP 下載、續傳 metadata 與分片清理
- 倉庫內固化的 EXE、DLL、PYD、ZIP
- Windows Release 建置與啟動煙霧測試

## 已修正問題

### 1. DPAPI 失敗時可能將 Cookie 明文寫入磁碟（高）

原本 `SecretProtector.Protect()` 在 DPAPI 發生例外時直接回傳明文，設定與帳號儲存程式仍會把該值序列化到 XML。

修正後加密失敗會拋出 `CryptographicException`，儲存操作中止，不再以可用性為由靜默落盤明文。

### 2. Cookie／service ticket 暴露於子程序命令列（高）

原本 Python helper 的完整 Cookie 與 `service_ticket` 會出現在 `ProcessStartInfo.Arguments`，同使用者權限的其他程序可能觀察到命令列。

修正後敏感值只透過 redirected stdin 傳遞。helper 已拒絕舊的敏感命令列模式；`--selftest` 等非敏感參數仍可正常使用。

### 3. 設定、帳號與歷史檔不是原子更新（中）

原本採用「刪除舊檔，再移動暫存檔」，兩個操作之間若斷電或程序終止，可能遺失資料。

修正後統一使用同目錄暫存檔、`Flush(true)`、`File.Replace`，並保留 `.bak` 備份；讀取主檔失敗時會嘗試載入備份。

### 4. 帳號檔名正規化可能碰撞（中）

原本帳號 Key 移除非法字元並截短後直接作為檔名，不同 Key 可能對應到同一檔案。

修正後檔名加入原始 Key 的 SHA-256 前 12 個十六進位字元。讀取與刪除時也會核對檔案內容中的 Key，並保留舊格式檔案的遷移相容性。

### 5. Shell URL 未限制 URI scheme（中）

原本 `OpenUrl()` 會把任意字串交給 Windows Shell。修正後只接受絕對的 HTTP／HTTPS URL，並拒絕帶有 user-info 的 URL。

### 6. 內嵌 OpenSSL 版本註解不符合實際檔案（低）

原始註解宣稱 OpenSSL 3.5.7，實際 selftest 回報為 OpenSSL 3.0.21。已移除硬編碼版本宣稱，改以 helper 的 TLS selftest 作為功能判據。

## 驗證結果

- `.NET Framework 4.8` Release 建置：成功
- 編譯警告：0
- 編譯錯誤：0
- DPAPI 加解密 round-trip：成功
- Python helper 兩份副本 SHA-256：一致
- Python `py_compile`：成功
- stdin ticket 測試：成功，伺服器正常回報假 ticket 無效
- 舊敏感命令列模式：已拒絕
- 內嵌 Python TLS selftest：成功
- `GeZi.exe` GUI 啟動 5 秒煙霧測試：成功
- Microsoft Defender 原始碼掃描：未發現威脅
- Microsoft Defender Release 輸出掃描：未發現威脅

## 尚存風險與限制

1. 專案呼叫夸克網盤非公開 API，介面、風控或服務條款變更都可能令功能失效。
2. 倉庫直接固化 CPython、OpenSSL、ZXing 及多個原生 DLL／PYD。多數檔案沒有 Authenticode 簽章；本次 Defender 掃描未發現威脅，但這不能取代可重現建置或完整供應鏈證明。
3. 下載完成主要依賴 HTTP Range、回應範圍及最終大小驗證；若上游未提供可信雜湊，無法做端到端內容完整性驗證。
4. HTTP handler 允許自動重新導向。CookieContainer 會依網域規則處理 Cookie，但仍應持續限制 API 與下載連結的可信主機範圍。
5. 分片清理會依使用者設定的根目錄掃描符合命名規則的舊檔；使用 reparse point／junction 的特殊環境仍需額外小心。
6. 本次未使用真實夸克帳號登入，也未下載受版權保護內容；登入與大型檔案續傳僅能由使用者在合法授權前提下做整合測試。

## 使用提醒

- 僅從可信來源取得建置包，並核對隨附 SHA-256。
- 不要將 `GeZi.config.xml`、`accounts/` 或匯出的直鏈檔分享給他人。
- 若要散布修改版，必須遵守 GPL-3.0 並提供相應完整原始碼。