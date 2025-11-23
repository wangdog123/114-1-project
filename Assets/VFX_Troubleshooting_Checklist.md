# VFX 不播放問題排查清單

## 問題：slash 完成時沒有播放 VFX

---

## ✅ 檢查清單（按順序檢查）

### 1. VFX Manager 是否已設置？

**檢查步驟**:
```
選擇遊戲物件（包含 ScratchRhythmGame 腳本）
→ Inspector → VFX Manager 欄位
→ 是否已拖入 VFX Manager 物件？
```

**如果為空**:
```
1. 創建空物件 → 命名 "VFX Manager"
2. Add Component → Rhythm Game VFX Manager
3. 拖入到遊戲物件的 VFX Manager 欄位
```

---

### 2. VFX Prefabs 是否已設置？

**檢查步驟**:
```
選擇 VFX Manager 物件
→ Inspector
→ 檢查以下欄位是否都有 Prefab：
   ├─ Left Slash VFX
   ├─ Right Slash VFX
   ├─ Up Slash VFX
   └─ Down Slash VFX
```

**如果為空**:
- 需要先創建 VFX Prefab（參考 SlashVFX_Creation_Guide.md）
- 至少創建一個測試用的簡單 Particle System

**快速創建測試 VFX**:
```
1. Hierarchy → 右鍵 → Effects → Particle System
2. 命名 "TestSlashVFX"
3. 拖到 Project 視窗創建 Prefab
4. 將 Prefab 拖入所有 4 個方向的欄位（先測試）
```

---

### 3. 檢查 Console 的 Debug 訊息

現在程式碼已添加詳細的 Debug 訊息，Play 遊戲並擊中目標後，應該看到：

**正常情況**:
```
[遊戲] 準備播放 VFX：方向=Left, 位置=(100, 50, 0)
[VFX Manager] SpawnSlashVFX 被調用：方向=Left, 位置=(100, 50, 0)
[VFX Manager] 選擇左劃 VFX，Prefab=True
[VFX Manager] ✓ 開始生成 VFX：TestSlashVFX
[VFX Manager] 實例化 VFX：TestSlashVFX
[VFX Manager] 使用本地座標：(100, 50, 0)
[VFX Manager] ✓ Particle System 已播放：TestSlashVFX
[VFX Manager] VFX 將在 2 秒後銷毀
```

**問題 1: VFX Manager 為 null**
```
[遊戲] VFX Manager 為 null！請在 Inspector 中設置 VFX Manager。
```
→ 回到檢查清單 #1

**問題 2: Prefab 未設置**
```
[VFX Manager] SpawnSlashVFX 被調用：方向=Left, 位置=(100, 50, 0)
[VFX Manager] 選擇左劃 VFX，Prefab=False
[VFX Manager] ✗ Left 方向的劃痕 VFX 未設置！請在 Inspector 中拖入對應的 Prefab。
```
→ 回到檢查清單 #2

**問題 3: Prefab 沒有組件**
```
[VFX Manager] ⚠ 警告：TestSlashVFX 上沒有 VisualEffect 或 ParticleSystem 組件！
```
→ Prefab 需要有 VisualEffect 或 ParticleSystem 組件

---

### 4. 使用測試工具驗證

**步驟 1: 添加測試腳本**
```
創建空物件 → "VFX Tester"
Add Component → VFX Debug Tester
```

**步驟 2: 設置引用**
```
VFX Manager → 拖入 VFX Manager 物件
Test Position → (0, 0, 0)
```

**步驟 3: 測試**
```
Play
按方向鍵：
├─ ← 左箭頭 → 測試左劃 VFX
├─ → 右箭頭 → 測試右劃 VFX
├─ ↑ 上箭頭 → 測試上劃 VFX
└─ ↓ 下箭頭 → 測試下劃 VFX
```

如果按方向鍵能看到 VFX，但遊戲中看不到 → 可能是位置問題

---

### 5. 檢查 VFX 位置

VFX 可能生成了，但在看不到的地方。

**檢查 VFX Manager 設定**:
```
Inspector → VFX Manager:
├─ VFX Parent: 
│   └─ 如果是 Canvas，VFX 會在 Canvas 內
│   └─ 如果為空，VFX 會在 VFX Manager 物件下
├─ Use World Space: 
│   └─ ✓ 勾選 → 使用世界座標
│   └─ ✗ 不勾選 → 使用本地座標（相對於 Parent）
```

**推薦設定（2D Canvas 遊戲）**:
```
VFX Parent → 設為 Canvas
Use World Space → ✗ 不勾選
```

**推薦設定（3D 遊戲）**:
```
VFX Parent → 留空或設專門的 VFX Layer
Use World Space → ✓ 勾選
```

---

### 6. 檢查 Camera 能否看到 VFX

**如果使用 Particle System**:
- 確保 Camera 的 Culling Mask 包含 VFX 的 Layer

**如果使用 Visual Effect Graph**:
- 確保使用 URP 或 HDRP
- VFX Graph 不支援 Built-in Render Pipeline

---

### 7. 檢查 VFX Prefab 本身

**選擇 VFX Prefab → 檢查**:

**Particle System**:
```
├─ Looping: ✗ 不勾選（單次播放）
├─ Play On Awake: ✓ 勾選（自動播放）
├─ Duration: 0.5-2 秒
└─ Start Lifetime: 0.3-1 秒
```

**Visual Effect**:
```
├─ Asset Template: 必須設置
├─ Random Seed: 可選
└─ Initial Event Name: "OnPlay"
```

---

## 🔧 快速修復步驟

### 最簡單的測試方法：

1. **創建測試 Particle System**:
   ```
   Hierarchy → Effects → Particle System
   直接拖到 Project → 創建 Prefab
   ```

2. **拖入所有 4 個方向**:
   ```
   VFX Manager → 所有 4 個欄位都拖入同一個 Prefab
   ```

3. **測試**:
   ```
   Play → 擊中目標
   應該會看到粒子特效（即使都一樣）
   ```

4. **如果還是看不到**:
   ```
   檢查 Console 的 Debug 訊息
   確認是哪一步出問題
   ```

---

## 📊 常見錯誤對照表

| Console 訊息 | 問題 | 解決方法 |
|--------------|------|----------|
| VFX Manager 為 null | 未設置管理器 | 拖入 VFX Manager 到遊戲物件 |
| Prefab=False | Prefab 未設置 | 拖入 VFX Prefab 到對應方向 |
| 沒有組件警告 | Prefab 缺少組件 | 添加 ParticleSystem 或 VisualEffect |
| 沒有任何訊息 | 可能沒觸發 | 確認有擊中目標 |
| VFX 一閃即逝 | Lifetime 太短 | 增加 vfxLifetime 參數 |

---

## 💡 建議

1. **先用最簡單的 Particle System 測試**
2. **確認能正常播放後，再製作精美的 VFX**
3. **使用 VFXDebugTester 獨立測試 VFX**
4. **檢查 Console 的詳細 Debug 訊息**

---

## 需要幫助？

如果以上步驟都檢查過還是不行，請提供：
1. Console 的完整 Debug 訊息
2. VFX Manager Inspector 的截圖
3. VFX Prefab 的結構
