# 開場鏡頭動畫設置指南 (Cinemachine 版本)

## 功能說明
使用 **Cinemachine Virtual Camera** 實現專業的開場鏡頭動畫：
1. 自動**切換**到開場專用的虛擬相機（可設置特寫、景深、追蹤等效果）
2. 播放一個**小動畫**（例如角色特寫、Logo、粒子效果等）
3. 自動**切回**遊戲主相機
4. 啟動視差背景移動
5. 進入技能演出階段
6. 正式開始遊戲

**優勢：**
- ✅ 使用 Cinemachine 的專業混合算法，鏡頭過渡超流暢
- ✅ 可在 Virtual Camera 上添加各種效果（景深、抖動、追蹤等）
- ✅ 所見即所得，在編輯器中直接預覽最終效果
- ✅ 不需要手動計算位置和旋轉，全部由 Cinemachine 處理

---

## Unity 編輯器設置步驟

### 前置：安裝 Cinemachine
1. 打開 Package Manager：`Window → Package Manager`
2. 搜索 `Cinemachine`
3. 點擊 `Install`

### 1. 創建開場虛擬相機
1. 在 Hierarchy 中：`右鍵 → Cinemachine → Virtual Camera`
2. 命名為 `IntroVirtualCamera`
3. 調整相機位置和角度到你想要的開場視角
   - 例如：角色臉部特寫
   - 例如：場景全景俯瞰
   - 例如：Logo 或特殊物品的近景

**調整技巧：**
- 選中 Virtual Camera 後，Scene 視圖會顯示相機視野預覽
- 可以直接在 Scene 中移動 Virtual Camera 來調整構圖
- Inspector 中調整 `Lens → Field of View` 控制視野大小

### 2. 設置虛擬相機效果（可選但推薦）
在 `IntroVirtualCamera` 的 Inspector 中：

**基礎設置：**
- `Priority`: 設為 `0`（初始關閉，由程式控制）
- `Follow`: 如果要追蹤物件，拖入目標 Transform
- `Look At`: 如果要看向物件，拖入目標 Transform

**進階效果（Add Extension）：**
- `CinemachineConfiner`: 限制相機範圍
- `CinemachineImpulseListener`: 添加相機抖動
- 景深效果：在 Main Camera 上添加 Post Processing Volume

### 3. 創建遊戲主相機（可選）
如果遊戲中也想用 Cinemachine 控制：
1. 創建另一個 Virtual Camera：`右鍵 → Cinemachine → Virtual Camera`
2. 命名為 `GameplayVirtualCamera`
3. 設置為遊戲時的視角
4. `Priority`: 設為 `10`（初始啟用）

### 4. 設置引用
在 `ScratchRhythmGame` 組件的「開場鏡頭動畫設置 - Cinemachine」區塊：
- **Intro Virtual Camera**: 拖入 `IntroVirtualCamera`
- **Gameplay Virtual Camera**: 拖入 `GameplayVirtualCamera`（可選）

### 5. 創建小動畫物件（可選）
**方案 A - 使用圖片/UI：**
1. 在 Canvas 下創建 Image 物件
2. 設置你想顯示的圖片（角色特寫、Logo 等）
3. 添加 Animator 組件並創建動畫（淡入淡出、旋轉等）
4. 初始設為不啟用（Disable）

**方案 B - 使用 3D 物件：**
1. 創建 3D 物件（角色模型、粒子效果等）
2. 放置在 IntroVirtualCamera 視野中
3. 添加動畫控制器
4. 初始設為不啟用（Disable）

**方案 C - 不使用動畫：**
- 留空 `Intro Animation Object`，系統會自動跳過

### 6. 設置動畫物件引用
在 `ScratchRhythmGame` 組件上：
- **Intro Animation Object**: 拖入動畫物件
- **Intro Animation Animator**: 拖入動畫的 Animator 組件（可選）
- **Intro Animation Trigger**: 設置觸發器名稱（預設 "Play"）

### 7. 調整時間參數
在 `ScratchRhythmGame` 組件的「開場鏡頭動畫設置 - Cinemachine」區塊：

| 參數 | 說明 | 建議值 |
|------|------|--------|
| **Intro Camera Blend Time** | 切換到開場相機的混合時間（秒） | 2.0 |
| **Intro Animation Duration** | 小動畫播放時間（秒） | 3.0 |
| **Return Camera Blend Time** | 切回主相機的混合時間（秒） | 1.5 |

**總時長 = 進入混合 + 動畫時間 + 退出混合**（預設 6.5 秒）

---

## 進階設置

### Cinemachine Brain 設置
確保 Main Camera 上有 `Cinemachine Brain` 組件：
1. 選中 Main Camera
2. 如果沒有，點擊 `Add Component → Cinemachine → Cinemachine Brain`
3. 設置 `Default Blend`:
   - `Style`: `EaseInOut`（平滑過渡）
   - `Time`: `1.0`（預設混合時間）

### 自定義混合曲線
創建自定義混合配置：
1. `Assets → Create → Cinemachine → Blend Settings Asset`
2. 在 Cinemachine Brain 中引用
3. 針對不同相機切換設置不同的混合曲線

### 添加景深效果
在 IntroVirtualCamera 上：
1. `Add Component → Cinemachine → CinemachineVolumeSettings`
2. 創建 Post Processing Profile
3. 啟用 `Depth of Field`
4. 調整 `Focus Distance` 和 `Aperture`

### 添加相機抖動
1. 創建 Impulse Source：`GameObject → Cinemachine → Impulse Source`
2. 在 IntroVirtualCamera 上添加 `CinemachineImpulseListener`
3. 在動畫中呼叫 Impulse Source 觸發抖動

### 路徑動畫（進階）
使用 Dolly Track 實現軌道運鏡：
1. 創建 `Cinemachine Dolly Cart with Track`
2. 設置路徑點
3. 讓 IntroVirtualCamera 跟隨 Dolly Cart

---

## 測試流程

### 快速測試
1. 進入 Play Mode
2. 按 **Space** 鍵開始遊戲
3. 觀察鏡頭動畫效果

### 調試模式
在 Console 中會看到以下 Log：
```
[遊戲] Start 按鈕觸發，開始開場鏡頭動畫！
[開場動畫] 開始鏡頭動畫序列
[開場動畫] 鏡頭放大中... 從 (x,y,z) 到 (x,y,z)
[開場動畫] 播放小動畫...
[開場動畫] 鏡頭拉回中... 從 (x,y,z) 到 (x,y,z)
[開場動畫] 動畫序列完成，開始遊戲流程
[ParallaxManager] 視差背景已啟動
```

---

## 常見問題

### Q: 鏡頭沒有切換？
**A:** 檢查：
- Main Camera 上是否有 `Cinemachine Brain` 組件
- `Intro Virtual Camera` 引用是否正確設置
- Virtual Camera 的位置是否與 Main Camera 不同
- Console 中是否有錯誤訊息

### Q: 鏡頭切換很生硬？
**A:** 調整：
- 增加 `Intro Camera Blend Time` 和 `Return Camera Blend Time`
- 在 Cinemachine Brain 中設置 `Default Blend` 為 `EaseInOut`
- 檢查是否有其他高優先級的 Virtual Camera 干擾

### Q: 小動畫沒有播放？
**A:** 檢查：
- `Intro Animation Object` 是否正確設置
- 動畫物件的 Animator 是否正確配置
- `Intro Animation Trigger` 名稱是否與 Animator 中的 Trigger 匹配
- 動畫持續時間是否足夠長

### Q: 想跳過開場動畫？
**A:** 兩種方法：
1. **臨時跳過**：不設置 `Intro Virtual Camera`，系統會自動跳過
2. **完全移除**：在按 Start 的邏輯中直接調用 `StartSkillCutscene()`

### Q: 想同時使用多個相機效果？
**A:** 使用 State-Driven Camera 或 Timeline：
- 創建 `Cinemachine State Driven Camera` 控制多個子相機
- 或使用 Unity Timeline 編排整個序列

### Q: 如何預覽開場效果？
**A:** 
1. 在編輯器中選中 `IntroVirtualCamera`
2. 將 Priority 臨時設為 100
3. 在 Scene 和 Game 視圖中查看效果
4. 記得改回 0

---

## 示例配置

### 配置 1：角色特寫 + 景深
**Virtual Camera 設置：**
- Position: 角色臉部前方 1.5 米
- Look At: 角色眼睛位置
- Lens FOV: 40（輕微特寫）
- Add Extension: Volume Settings → Depth of Field

**小動畫：**
- UI Text 淡入：「準備好了嗎？」
- 粒子效果：光暈閃爍

**時間：**
- Blend In: 2.0s
- Animation: 2.5s
- Blend Out: 1.5s

### 配置 2：場景全景掃視
**Virtual Camera 設置：**
- Position: 高空俯瞰位置
- Follow: Dolly Cart（沿著路徑移動）
- Lens FOV: 60（廣角視野）
- Body: Tracked Dolly

**小動畫：**
- Logo 在天空中旋轉
- 標題文字依序淡入

**時間：**
- Blend In: 2.5s
- Animation: 4.0s
- Blend Out: 2.0s

### 配置 3：快速切入 + 抖動
**Virtual Camera 設置：**
- Position: 極近特寫
- Lens FOV: 30（放大）
- Add Extension: Impulse Listener
- Noise: Basic Multi Channel Perlin

**小動畫：**
- 閃電效果
- 音效：「轟隆！」

**時間：**
- Blend In: 0.5s（快速切入）
- Animation: 1.5s
- Blend Out: 1.0s

---

## 代碼流程圖

```
玩家按 Start
    ↓
進入 IntroAnimation 狀態
    ↓
【第一階段】鏡頭放大到焦點
    ↓
【第二階段】播放小動畫
    ↓
【第三階段】鏡頭拉回原位
    ↓
啟動視差背景移動
    ↓
進入 SkillCutscene 狀態
    ↓
技能演出播放
    ↓
開始遊戲 Round
```

---

## 擴展建議

### 添加音效
在各階段添加音效：
```csharp
// 鏡頭放大時
audioSource.PlayOneShot(zoomInSound);

// 動畫播放時
audioSource.PlayOneShot(characterVoice);

// 鏡頭拉回時
audioSource.PlayOneShot(zoomOutSound);
```

### 添加畫面特效
使用 Post-Processing 效果：
```csharp
// 放大時增加景深效果
depthOfField.focusDistance.value = focusDistance;

// 動畫時增加暈影效果
vignette.intensity.value = 0.5f;
```

### 多個焦點序列
創建焦點陣列，依序放大到不同位置：
```csharp
public Transform[] cameraFocusPoints;
```
