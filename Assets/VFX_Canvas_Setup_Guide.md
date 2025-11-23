# VFX 在 Canvas 上顯示 - 完整設置教學

這個文件說明如何在 Unity Canvas 上顯示 Visual Effect Graph (VFX)。

## 目錄
1. [方法概述](#方法概述)
2. [方法 1: Render Texture（推薦）](#方法-1-render-texture推薦)
3. [方法 2: World Space Canvas](#方法-2-world-space-canvas)
4. [方法 3: 整合到節奏遊戲](#方法-3-整合到節奏遊戲)
5. [創建 VFX](#創建-vfx)
6. [常見問題](#常見問題)

---

## 方法概述

### 為什麼 VFX 不能直接放在 Canvas 上？
- VFX (Visual Effect Graph) 使用 **世界空間渲染**
- UI Canvas (Screen Space) 使用 **螢幕空間渲染**
- 兩者不在同一個渲染空間，所以需要特殊處理

### 三種解決方案：

| 方法 | 優點 | 缺點 | 適用情況 |
|------|------|------|----------|
| **Render Texture** | 完美分離、性能好 | 設置複雜 | Screen Space Canvas |
| **World Space Canvas** | 簡單直接 | Canvas 需改成 World Space | 3D 場景 |
| **整合節奏遊戲** | 自動化、易用 | 需要管理器 | 遊戲內特效 |

---

## 方法 1: Render Texture（推薦）

### 步驟 1: 創建 VFX Camera

1. **在場景中創建新 Camera**:
   ```
   右鍵 → Camera
   命名為 "VFX Camera"
   ```

2. **設置 VFX Camera**:
   - Position: `(0, 0, -10)` （遠離主畫面）
   - Projection: `Orthographic`
   - Size: `5`
   - Clear Flags: `Solid Color`
   - Background: `R:0 G:0 B:0 A:0` （完全透明）
   - Culling Mask: **只勾選 "VFX Layer"**

3. **創建新 Layer**:
   ```
   Edit → Project Settings → Tags and Layers
   在 User Layer 8 加入 "VFX"
   ```

### 步驟 2: 設置 Canvas

1. **在 Canvas 下創建 RawImage**:
   ```
   Canvas → 右鍵 → UI → Raw Image
   命名為 "VFX Display"
   ```

2. **調整 RawImage 大小**:
   - Width: `512`
   - Height: `512`
   - 位置：根據需求調整

### 步驟 3: 掛載腳本

1. **創建空物件**:
   ```
   右鍵 → Create Empty
   命名為 "VFX Canvas Controller"
   ```

2. **掛載 VFXToCanvas 腳本**:
   - 將 `VFXToCanvas.cs` 拖到物件上

3. **設置引用**:
   - VFX Effect: 你的 VFX GameObject
   - VFX Camera: 剛創建的 VFX Camera
   - Canvas Image: RawImage 組件
   - Resolution: `512 x 512`
   - Transparent Background: ✓ 勾選

### 步驟 4: 設置 VFX

1. **將 VFX 放在 VFX Camera 前方**:
   - Position: `(0, 0, 0)`

2. **設置 VFX Layer**:
   ```
   選擇 VFX GameObject
   Layer → VFX
   ```

3. **測試運行**:
   - 按 Play，應該會在 Canvas 的 RawImage 上看到 VFX

---

## 方法 2: World Space Canvas

### 步驟 1: 修改 Canvas 設置

```
選擇 Canvas
Render Mode → World Space
Event Camera → Main Camera
```

### 步驟 2: 調整 Canvas 位置

```
Position: (0, 0, 5) // 在鏡頭前方
Rotation: (0, 0, 0)
Scale: (0.01, 0.01, 0.01) // 縮小到合適大小
```

### 步驟 3: 使用腳本

1. **掛載 VFXWorldSpaceCanvas 腳本**

2. **設置**:
   - VFX Prefab: 你的 VFX Prefab
   - Canvas Transform: Canvas 的 Transform
   - Local Position: `(0, 0, 0)`
   - Scale: `1`

---

## 方法 3: 整合到節奏遊戲

### 步驟 1: 創建 VFX Manager

1. **創建空物件**:
   ```
   右鍵 → Create Empty
   命名為 "VFX Manager"
   ```

2. **掛載 RhythmGameVFXManager 腳本**

### 步驟 2: 創建 VFX Prefabs

你需要創建以下 VFX：

#### 2.1 Perfect Hit VFX（完美擊中）
- 顏色：金色、白色
- 效果：爆炸、光芒
- 大小：中等

#### 2.2 Great Hit VFX（很好）
- 顏色：藍色、青色
- 效果：閃光
- 大小：稍小

#### 2.3 Good Hit VFX（良好）
- 顏色：綠色
- 效果：粒子
- 大小：小

#### 2.4 Miss VFX（錯過）
- 顏色：紅色、灰色
- 效果：煙霧、破碎
- 大小：中等

#### 2.5 Combo VFX（連擊）
- 顏色：彩虹、星星
- 效果：環形擴散
- 大小：大

### 步驟 3: 設置 Manager

1. **設置 VFX Prefabs**:
   - Perfect Hit VFX: 拖入 Prefab
   - Great Hit VFX: 拖入 Prefab
   - Good Hit VFX: 拖入 Prefab
   - Miss VFX: 拖入 Prefab
   - Combo VFX: 拖入 Prefab

2. **設置生成選項**:
   - VFX Parent: Canvas 或專門的 VFX Layer
   - VFX Lifetime: `2` 秒
   - Use World Space: 根據需求勾選

### 步驟 4: 連接到遊戲

1. **在 ScratchRhythmGame 上設置**:
   ```
   選擇遊戲物件
   VFX Manager → 拖入 VFX Manager 物件
   ```

2. **測試**:
   - 擊中目標應該會播放對應的 VFX
   - 每 10 連擊會播放特殊 VFX

---

## 創建 VFX

### 使用 Visual Effect Graph

1. **創建 VFX Asset**:
   ```
   右鍵 → Create → Visual Effects → Visual Effect Graph
   命名為 "HitEffect"
   ```

2. **編輯 VFX**:
   - 雙擊打開 VFX Graph Editor
   - 使用節點創建特效（粒子、光線等）

3. **創建 Prefab**:
   ```
   場景中創建空物件 → Add Component → Visual Effect
   設置 Asset Template → 剛創建的 VFX
   拖到 Project 視窗創建 Prefab
   ```

### 推薦設置（擊中特效範例）

```
Initialize Particle
├─ Capacity: 100
├─ Lifetime: Random (0.5, 1.0)
└─ Color: Gradient (白→金→透明)

Update Particle
├─ Velocity: Random Direction Cone
├─ Drag: 5
└─ Size over Life: Curve (大→小)

Output Particle
└─ Blend Mode: Additive
```

---

## 常見問題

### Q1: VFX 在 Canvas 上不顯示？
**A**: 檢查：
- VFX Camera 的 Culling Mask 是否正確
- VFX GameObject 的 Layer 是否設置為 VFX
- RawImage 的 Texture 是否已設置

### Q2: VFX 顯示但位置不對？
**A**: 確認：
- VFX 是在 VFX Camera 的視野範圍內
- 使用 `useWorldSpace` 設置正確的座標系統

### Q3: VFX 會影響主畫面性能？
**A**: 
- 使用 Render Texture 可以控制解析度（如 512x512）
- 限制 VFX 的粒子數量
- 設置合適的 `vfxLifetime` 自動銷毀

### Q4: 如何讓 VFX 跟隨目標移動？
**A**: 
```csharp
// 在 Update 中
vfxInstance.transform.position = target.transform.position;
```

### Q5: 如何調整 VFX 大小？
**A**: 
```csharp
vfxInstance.transform.localScale = Vector3.one * scaleFactor;
```

---

## 性能優化建議

1. **使用對象池（Object Pooling）**:
   ```csharp
   // 預先創建一些 VFX，重複使用而不是每次 Instantiate
   ```

2. **限制同時存在的 VFX 數量**:
   ```csharp
   if (activeVFXCount > maxVFXCount)
   {
       // 銷毀最舊的 VFX
   }
   ```

3. **降低 Render Texture 解析度**:
   - 512x512 通常已經足夠
   - 遊戲運行時可以根據設備性能動態調整

4. **使用簡單的 VFX**:
   - 減少粒子數量
   - 避免複雜的計算節點

---

## 下一步

1. **創建您的第一個 VFX**（擊中特效）
2. **測試 Render Texture 方法**
3. **整合到遊戲中**
4. **根據遊戲風格調整特效**

如需更多幫助，請參考：
- Unity Visual Effect Graph 官方文檔
- 範例專案中的 VFX 設置
