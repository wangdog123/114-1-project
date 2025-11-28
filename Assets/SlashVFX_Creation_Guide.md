# 劃痕 VFX 創建指南

## 概述
為節奏遊戲創建 4 個方向的劃痕特效（左、右、上、下）

---

## 方法 1: 使用 Unity Particle System（簡單快速）

### 1. 創建左劃痕特效

```
Hierarchy → 右鍵 → Effects → Particle System
命名為 "LeftSlashVFX"
```

**設置參數**:
```
Main:
├─ Duration: 0.5
├─ Start Lifetime: 0.3
├─ Start Speed: 5
├─ Start Size: 0.5
└─ Start Color: 白色 → 透明（Gradient）

Emission:
└─ Rate over Time: 50

Shape:
├─ Shape: Cone
├─ Angle: 30
├─ Radius: 0.1
└─ Rotation: (0, 0, 0) ← 向右噴射

Velocity over Lifetime:
└─ Linear: (-5, 0, 0) ← 向左移動

Size over Lifetime:
└─ Curve: 大 → 小

Color over Lifetime:
└─ Gradient: 白(255) → 透明(0)

Renderer:
└─ Material: Default-Particle (或自訂材質)
```

### 2. 創建右劃痕特效

```
複製 LeftSlashVFX → 改名 "RightSlashVFX"
```

**修改參數**:
```
Shape:
└─ Rotation: (0, 180, 0) ← 反向

Velocity over Lifetime:
└─ Linear: (5, 0, 0) ← 向右移動
```

### 3. 創建上劃痕特效

```
複製 LeftSlashVFX → 改名 "UpSlashVFX"
```

**修改參數**:
```
Shape:
└─ Rotation: (0, 0, -90) ← 朝上

Velocity over Lifetime:
└─ Linear: (0, 5, 0) ← 向上移動
```

### 4. 創建下劃痕特效

```
複製 LeftSlashVFX → 改名 "DownSlashVFX"
```

**修改參數**:
```
Shape:
└─ Rotation: (0, 0, 90) ← 朝下

Velocity over Lifetime:
└─ Linear: (0, -5, 0) ← 向下移動
```

### 5. 創建 Prefab

```
將 4 個 VFX 從 Hierarchy 拖到 Project 視窗
→ 自動創建 Prefab
```

---

## 方法 2: 使用 Sprite + Animation（2D 風格）

### 1. 創建劃痕 Sprite

在 Photoshop/GIMP 中創建劃痕圖片：
- 尺寸: 256x256
- 白色劃痕，黑色背景
- 保存為 PNG（帶透明度）

### 2. 導入 Unity

```
拖入 Project 視窗
設置:
├─ Texture Type: Sprite (2D and UI)
└─ Alpha Is Transparency: ✓
```

### 3. 創建 VFX GameObject

```
Hierarchy → 右鍵 → 2D Object → Sprite
命名為 "LeftSlashVFX"
```

**設置**:
```
Sprite Renderer:
├─ Sprite: 剛導入的劃痕圖片
├─ Color: 白色
└─ Material: Sprites-Default

Transform:
├─ Rotation: (0, 0, 0) ← 左劃
└─ Scale: (1, 1, 1)
```

### 4. 添加動畫

```
選擇 LeftSlashVFX
→ Add Component → Animation
→ Window → Animation → Animation
→ Create New Clip → "LeftSlash"
```

**動畫關鍵幀**:
```
0.0s:
├─ Color.a = 255 (完全不透明)
└─ Scale = (0.5, 0.5, 1)

0.3s:
├─ Color.a = 0 (完全透明)
└─ Scale = (1.5, 1.5, 1)
```

### 5. 複製並旋轉

```
複製 LeftSlashVFX 3 次
RightSlashVFX → Rotation Z: 180
UpSlashVFX → Rotation Z: 90
DownSlashVFX → Rotation Z: -90
```

---

## 方法 3: 使用 Visual Effect Graph（進階）

### 前置需求
```
Window → Package Manager
安裝: Visual Effects Graph
```

### 1. 創建 VFX Graph

```
Project → 右鍵 → Create → Visual Effects → Visual Effect Graph
命名為 "SlashEffect"
```

### 2. 編輯 VFX Graph

雙擊打開編輯器

**設置節點**:
```
Initialize Particle:
├─ Capacity: 20
├─ Lifetime: 0.3
└─ Size: 0.2

Spawn:
└─ Rate: 50

Update Particle:
├─ Add Velocity: 使用 Direction 參數
└─ Size over Life: 曲線（大→小）

Output:
├─ Blend Mode: Additive
└─ Color over Life: 白→透明
```

### 3. 創建參數

```
Blackboard → + → Vector3
命名: SlashDirection

值:
├─ Left: (-1, 0, 0)
├─ Right: (1, 0, 0)
├─ Up: (0, 1, 0)
└─ Down: (0, -1, 0)
```

### 4. 創建 4 個 Prefab

```
場景中創建 4 個 Visual Effect GameObject
設置不同的 SlashDirection 參數
創建 Prefab
```

---

## 整合到遊戲

### 步驟 1: 設置 VFX Manager

```
Hierarchy → 創建空物件 → "VFX Manager"
Add Component → Rhythm Game VFX Manager
```

### 步驟 2: 拖入 Prefab

```
Inspector:
├─ Left Slash VFX → 拖入 LeftSlashVFX Prefab
├─ Right Slash VFX → 拖入 RightSlashVFX Prefab
├─ Up Slash VFX → 拖入 UpSlashVFX Prefab
└─ Down Slash VFX → 拖入 DownSlashVFX Prefab
```

### 步驟 3: 連接到遊戲

```
選擇 ScratchRhythmGame 物件
VFX Manager → 拖入 VFX Manager 物件
```

### 步驟 4: 測試

```
Play → 擊中目標 → 應該顯示對應方向的劃痕 VFX
```

---

## 推薦設置（簡單版）

**材質顏色建議**:
```
左劃 (Left): 藍色 (#00B0FF)
右劃 (Right): 紅色 (#FF4444)
上劃 (Up): 綠色 (#00FF88)
下劃 (Down): 黃色 (#FFD700)
```

**特效持續時間**:
```
VFX Lifetime: 0.5 秒（快速消失）
```

**位置偏移**（讓劃痕從目標中心延伸）:
```csharp
// 在 VFXManager.SpawnSlashVFX 中可以添加
Vector3 offset = GetDirectionOffset(direction) * 0.5f;
vfxObj.transform.position = position + offset;
```

---

## 常見問題

### Q: VFX 方向不對？
**A**: 檢查 Particle System 的 `Rotation` 和 `Velocity over Lifetime`

### Q: VFX 太快消失？
**A**: 增加 `Duration` 和 `Start Lifetime`

### Q: VFX 看不清楚？
**A**: 
- 增加 `Start Size`
- 提高 `Emission Rate`
- 使用 Additive Blend Mode

### Q: 想要拖尾效果？
**A**: 
```
Trails 模組 → ✓ 啟用
├─ Ratio: 0.5
├─ Lifetime: 0.3
└─ Width over Trail: 寬→窄
```

---

## 下一步

1. ✓ 創建 4 個基本劃痕特效
2. ✓ 調整顏色和大小
3. ✓ 設置 VFX Manager
4. ✓ 測試遊戲效果
5. 根據遊戲風格微調特效

享受製作 VFX 的樂趣！🎨✨
