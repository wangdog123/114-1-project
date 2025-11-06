# Joy-Con 劃痕節奏遊戲 - 設定指南

## 🎯 核心解決方案

### 問題分析
你遇到的問題是 Joy-Con 的**座標系轉換問題**：
- **加速度計**讀取的是手把**本地座標系**的數值
- 當手把旋轉時，本地座標的 X/Y/Z 軸方向也會跟著改變
- 例如：直直拿著往右揮 vs 斜著拿往右揮，加速度計的數值完全不同

### 解決方法：座標系轉換
使用 **Gyro（陀螺儀）** 來取得手把的旋轉四元數，然後將加速度從本地座標轉換到世界座標：

```csharp
// 1. 取得手把旋轉
Quaternion rotation = SwitchControllerHID.current.deviceRotation.ReadValue();

// 2. 取得本地加速度
Vector3 localAccel = SwitchControllerHID.current.acceleration.ReadValue();

// 3. 轉換到世界座標 ⭐ 關鍵！
Vector3 worldAccel = rotation * localAccel;
```

這樣不管手把怎麼握、怎麼轉向，揮動的**世界方向**都會被正確偵測！

---

## 📦 檔案說明

### 1. `SwingDetector.cs`
核心的揮動偵測器，負責：
- ✅ 將加速度從本地座標轉換到世界座標
- ✅ 自動校準重力偏移
- ✅ 偵測 6 個方向的揮動（上/下/左/右/前/後）
- ✅ 避免重複偵測（冷卻機制）

**關鍵參數：**
- `swingThreshold`: 揮動門檻值（預設 2.5）
- `cooldownTime`: 冷卻時間（預設 0.3 秒）
- `accelerationSmoothing`: 平滑係數（0.3 = 較平滑）

### 2. `SlashRhythmGame.cs`
遊戲邏輯管理器，負責：
- 🎮 生成隨機劃痕序列
- 📊 顯示序列並計時
- ✔️ 檢查玩家輸入是否正確
- 📈 難度漸進（越高級劃痕越多、時間越短）
- 🏆 計分系統

### 3. `SlashIcon.cs`
UI 圖示組件，負責：
- 🎨 顯示方向箭頭
- 🌈 狀態顏色（正常/隱藏/正確/錯誤）

---

## 🛠️ Unity 場景設定

### Step 1: 創建基本物件

1. **創建空物件** `GameManager`
   - 加入 `SwingDetector.cs`
   - 加入 `SlashRhythmGame.cs`

2. **創建 Canvas**（如果還沒有）
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080

### Step 2: 設定 UI

在 Canvas 下創建以下 UI 元素：

```
Canvas
├── Panel_Game
│   ├── Text_Score (顯示分數)
│   ├── Text_Level (顯示關卡)
│   ├── Text_Hint (顯示提示)
│   └── Panel_SlashIcons (水平排列的劃痕圖示容器)
│       └── HorizontalLayoutGroup (自動排列)
```

#### Panel_SlashIcons 設定：
- 加入 `Horizontal Layout Group` 組件
- Spacing: 20
- Child Alignment: Middle Center
- Child Force Expand: Width ✓, Height ✗

### Step 3: 創建劃痕圖示 Prefab

1. 在 `Panel_SlashIcons` 下創建 **Image** 物件
2. 命名為 `SlashIcon`
3. 加入 `SlashIcon.cs` 腳本
4. 加入子物件 **Text**（顯示箭頭符號）
   - Font Size: 60
   - Alignment: Center Middle
5. 將 `SlashIcon` 拖到 Project 視窗創建 **Prefab**
6. 刪除 Hierarchy 中的原始物件

### Step 4: 連接組件

在 `GameManager` 的 `SlashRhythmGame` 組件：
- **Swing Detector**: 拖入同物件上的 SwingDetector
- **Slash Icon Container**: 拖入 `Panel_SlashIcons`
- **Slash Icon Prefab**: 拖入剛創建的 Prefab
- **Score Text**: 拖入 `Text_Score`
- **Level Text**: 拖入 `Text_Level`
- **Hint Text**: 拖入 `Text_Hint`

---

## ⚙️ 參數調整指南

### SwingDetector 參數

#### `swingThreshold` (揮動門檻)
- **太小**：輕微移動就會誤判 → 調高
- **太大**：用力揮也偵測不到 → 調低
- **建議值**：2.0 ~ 4.0
- **測試方法**：
  1. 開啟 `showDebugInfo`
  2. 揮動手把觀察 Console
  3. 看到 `強度: X.XX` 的數值
  4. 調整 threshold 略低於你用力揮的數值

#### `cooldownTime` (冷卻時間)
- **太短**：同一次揮動會被偵測多次
- **太長**：連續揮動會漏掉
- **建議值**：0.2 ~ 0.5 秒

#### `accelerationSmoothing` (平滑係數)
- **越小**：越平滑，但反應慢
- **越大**：反應快，但容易抖動
- **建議值**：0.2 ~ 0.5

### SlashRhythmGame 參數

#### 難度曲線
```csharp
currentLevel = 1;                    // 起始關卡
slashesPerLevel = 3;                 // 第 1 關有 3 個劃痕
slashIncreasePerLevel = 1;           // 每關增加 1 個劃痕
displayDuration = 2f;                // 第 1 關顯示 2 秒
timeDecreasePerLevel = 0.1f;         // 每關減少 0.1 秒
minDisplayDuration = 0.5f;           // 最短 0.5 秒
```

**難度範例：**
- 第 1 關：3 個劃痕，顯示 2.0 秒
- 第 2 關：4 個劃痕，顯示 1.9 秒
- 第 3 關：5 個劃痕，顯示 1.8 秒
- ...
- 第 16 關：18 個劃痕，顯示 0.5 秒（最低）

---

## 🧪 測試流程

### 1. 校準測試
1. 執行遊戲
2. 保持 Joy-Con **完全靜止** 3 秒
3. Console 應該顯示：`校準完成！`
4. 按下 **Y 鍵**可以重新校準

### 2. 方向測試
執行以下測試，確保每個方向都能正確偵測：

| 測試 | 手把握法 | 揮動方向 | 預期結果 |
|------|---------|---------|---------|
| 1 | 直直拿 | 向上揮 | 偵測到 Up |
| 2 | 直直拿 | 向下揮 | 偵測到 Down |
| 3 | 直直拿 | 向左揮 | 偵測到 Left |
| 4 | 直直拿 | 向右揮 | 偵測到 Right |
| 5 | **斜 45° 拿** | 向上揮 | 偵測到 Up ⭐ |
| 6 | **橫著拿** | 向右揮 | 偵測到 Right ⭐ |
| 7 | **倒著拿** | 向上揮 | 偵測到 Up ⭐ |

✅ 如果測試 5~7 都能正確偵測，代表座標轉換成功！

### 3. 遊戲流程測試
1. 遊戲開始 → 顯示 3 個方向的劃痕
2. 2 秒後劃痕變半透明
3. 按照順序揮動 Joy-Con
4. 正確 → 圖示變綠色，繼續下一個
5. 錯誤 → 圖示變紅色，重新開始本關
6. 全部正確 → 進入下一關（劃痕增加，時間減少）

---

## 🐛 常見問題排解

### Q1: 揮動沒有反應
**原因：**
- 門檻值太高
- 沒有校準

**解決：**
1. 開啟 `showDebugInfo` 查看實際數值
2. 調低 `swingThreshold`
3. 按 Y 鍵重新校準

### Q2: 輕微移動就觸發
**原因：**
- 門檻值太低
- 校準時手把在晃動

**解決：**
1. 調高 `swingThreshold`
2. 重新校準（保持完全靜止）

### Q3: 方向判斷錯誤
**原因：**
- 沒有使用世界座標
- 校準偏移不正確

**檢查：**
1. 確認使用的是 `SwingDetector.cs`（有座標轉換）
2. 查看 `calibrationOffset` 的數值是否接近 `(0, -9.8, 0)`

### Q4: 同一次揮動偵測多次
**原因：**
- 冷卻時間太短

**解決：**
- 增加 `cooldownTime`（建議 0.3 ~ 0.5）

### Q5: 斜著拿會影響判定
**原因：**
- 可能沒有正確使用 `deviceRotation`

**檢查：**
```csharp
// 確保有這行關鍵代碼：
Vector3 worldAccel = rotation * localAccel;
```

---

## 🎨 視覺優化建議

### 1. 劃痕動畫
```csharp
// 在 SlashIcon.cs 中加入動畫
public void Show()
{
    // 從小到大彈出
    LeanTween.scale(gameObject, Vector3.one, 0.3f)
             .setEaseOutBack();
}

public void MarkAsCorrect()
{
    // 閃爍效果
    LeanTween.color(gameObject, correctColor, 0.2f)
             .setEaseInOutQuad();
}
```

### 2. 時間凍結效果
在接到物體時：
```csharp
// 使用 Time.timeScale
Time.timeScale = 0.1f;  // 慢動作
yield return new WaitForSecondsRealtime(0.5f);
Time.timeScale = 1f;    // 恢復正常
```

### 3. 粒子特效
- 正確揮動：綠色粒子爆炸
- 錯誤揮動：紅色粒子
- 完成關卡：金色煙火

---

## 📊 進階功能擴展

### 1. 連擊系統
```csharp
private int combo = 0;
private float comboTimer = 0f;

void OnCorrectSwing()
{
    combo++;
    comboTimer = 3f;  // 3 秒內要繼續
    score += 10 * currentLevel * combo;  // 連擊加成
}

void Update()
{
    if (comboTimer > 0)
    {
        comboTimer -= Time.deltaTime;
        if (comboTimer <= 0) combo = 0;
    }
}
```

### 2. 特殊劃痕
```csharp
public enum SlashType
{
    Normal,      // 普通劃痕
    Heavy,       // 需要用力揮（門檻 x2）
    Quick,       // 快速劃痕（冷卻減半）
    Perfect      // 完美劃痕（時間窗口很小）
}
```

### 3. 音樂節奏同步
```csharp
// 配合 BPMListener
void OnBeat()
{
    // 在節拍時顯示劃痕
    ShowNextSlash();
}
```

---

## 📝 程式碼重點說明

### 為什麼要用四元數旋轉？
```csharp
// ❌ 錯誤做法：直接用本地加速度
Vector3 localAccel = controller.acceleration.ReadValue();
if (localAccel.x > threshold) // 往右揮？

// 問題：如果手把轉了 90°，
// 原本的 X 軸變成了 Y 軸方向！
```

```csharp
// ✅ 正確做法：轉換到世界座標
Quaternion rotation = controller.deviceRotation.ReadValue();
Vector3 localAccel = controller.acceleration.ReadValue();
Vector3 worldAccel = rotation * localAccel;  // 四元數旋轉向量

if (worldAccel.x > threshold) // 無論手把怎麼轉，
// worldAccel.x 永遠代表「真正的右方」
```

### 校準的重要性
```csharp
// 靜止時的加速度 ≠ (0, 0, 0)
// 因為有重力！大約是 (0, -9.8, 0)

// 但如果手把傾斜，重力會分散到各軸
// 例如 45° 傾斜：(0, -6.9, -6.9)

// 所以要先記錄「靜止時的值」
calibrationOffset = 平均靜止加速度;

// 實際揮動時，減去這個偏移
Vector3 swing = worldAccel - calibrationOffset;
```

---

## 🚀 快速開始檢查清單

- [ ] 已將 `SwingDetector.cs`、`SlashRhythmGame.cs`、`SlashIcon.cs` 加入專案
- [ ] 已創建 GameManager 物件並掛載腳本
- [ ] 已創建 UI（Canvas、Text、Panel）
- [ ] 已創建 SlashIcon Prefab
- [ ] 已在 Inspector 中連接所有參考
- [ ] 已測試校準功能（靜止 3 秒）
- [ ] 已測試各方向揮動（直拿 + 斜拿）
- [ ] 已調整門檻值到合適範圍

---

## 💡 關鍵提醒

1. **一定要校準**：每次開始遊戲都要讓手把靜止 3 秒
2. **使用世界座標**：這是解決旋轉問題的核心
3. **平滑 vs 反應速度**：根據遊戲節奏調整 `accelerationSmoothing`
4. **門檻值測試**：不同人握法、力度不同，要多測試
5. **冷卻時間**：避免一次揮動被偵測多次

---

祝你遊戲開發順利！🎮✨
