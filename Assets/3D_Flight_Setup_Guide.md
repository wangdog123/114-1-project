# 3D 飛行物件系統設置指南

## ✅ 已完成的修改：

### 1. 創建了 `SlashTarget3D.cs`
- 新的 3D 版本目標物件腳本
- 支援從發射點飛向目標點
- 自動追蹤移動的目標點（跟隨鏡頭）
- 使用平滑的飛行曲線（SmoothStep）

### 2. 修改了 `ScratchRhythmGame.cs`
- 加入 3D 模式開關 `use3DMode`
- 加入發射點和目標點引用
- 修改生成邏輯支援 2D/3D 雙模式

## 📋 Unity 設置步驟：

### 1. 等待 Unity 編譯完成
- Unity 會自動檢測到新的 `SlashTarget3D.cs` 文件
- 等待編譯完成（Console 不再顯示錯誤）

### 2. 創建空物件
在 Hierarchy 中創建兩個空物件：

**發射點（SpawnPoint）：**
- 右鍵 → Create Empty
- 命名為 `SpawnPoint`
- 設置位置：例如 `(0, 0, -20)`（在鏡頭後方遠處）

**目標點（TargetPoint）：**
- 右鍵 → Create Empty
- 命名為 `TargetPoint`
- 設置位置：例如 `(0, 1, 3)`（在鏡頭前上方）
- **重要：設置為 Camera 的子物件**，讓它跟隨鏡頭移動

### 3. 創建 3D 目標物件預製體

**創建基礎物件：**
```
右鍵 → 3D Object → Cube（或其他形狀）
命名為 "SlashTarget3D_Prefab"
```

**添加組件：**
1. 選中物件
2. Add Component → `SlashTarget3D`（編譯完成後會出現）
3. Add Component → TextMeshPro - Text（3D 文字）

**調整大小：**
- Scale: (0.5, 0.5, 0.5) 或更小
- 文字位置調整到物件中心

**創建預製體：**
- 將物件拖到 Project 視窗的 Assets 資料夾
- 刪除場景中的物件

### 4. 設置 ScratchRhythmGame

選中掛載 `ScratchRhythmGame` 的物件：

1. **3D 飛行設置：**
   - `Use 3D Mode`: ✅ 打勾
   - `Spawn Point`: 拖入 SpawnPoint 物件
   - `Target Point`: 拖入 TargetPoint 物件

2. **UI 引用：**
   - `Slash Target Prefab`: 拖入 3D 預製體
   - `Slash Targets Parent`: 可以設為場景根或空物件

3. **難度設置：**
   - `Flying Duration`: 8（秒）
   - 根據需要調整

## 🎮 運作原理：

### 飛行流程：
```
1. 遊戲開始 → 提示階段
2. 物件在 SpawnPoint 生成
3. 物件開始飛行 (flyingStartTime)
4. 每幀更新：Lerp(startPos, targetPoint.position, progress)
5. targetPoint 跟隨鏡頭移動
6. 物件自動追蹤移動的目標
7. 飛行時間到 → 玩家必須擊中或 Miss
```

### 關鍵公式：
```csharp
// 飛行進度 (0-1)
float progress = (Time.time - flyingStartTime) / flyingDuration;

// 平滑進度
float smoothProgress = Mathf.SmoothStep(0, 1, progress);

// 位置更新（自動追蹤移動的目標）
transform.position = Vector3.Lerp(startPosition, targetPoint.position, smoothProgress);
```

## ⚠️ 重要注意事項：

1. **TargetPoint 必須是 Camera 的子物件**
   - 這樣鏡頭移動時，目標點也會跟著移動
   - 物件會自動調整軌跡追蹤新位置

2. **飛行時間計算**
   - 現在使用固定 8 秒
   - 如果鏡頭移動太快，可能需要增加飛行時間
   - 或者減少 SpawnPoint 到 TargetPoint 的距離

3. **性能優化**
   - 物件數量較多時，考慮使用物件池
   - 擊中/Miss 後會自動銷毀（0.5秒延遲）

## 🔧 測試步驟：

1. Play 遊戲
2. 觀察 Console：
   - `[飛行] 物件 #0 開始飛行` → 確認飛行開始
   - 物件應該從 SpawnPoint 飛向 TargetPoint
3. 移動鏡頭（如果有移動邏輯）
   - 觀察物件是否追蹤移動的 TargetPoint
4. 測試擊中邏輯（WASD 或 Joy-Con）

## 🎨 進階優化（可選）：

### 1. 飛行軌跡曲線
可以修改 `UpdateFlying()` 使用拋物線：
```csharp
// 加入高度曲線
float height = Mathf.Sin(progress * Mathf.PI) * arcHeight;
Vector3 targetPos = Vector3.Lerp(startPosition, targetPoint.position, smoothProgress);
targetPos.y += height;
transform.position = targetPos;
```

### 2. 旋轉動畫
```csharp
// 飛行時旋轉
transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
```

### 3. 尾跡效果
- 添加 Trail Renderer 組件
- 設置 Material 和顏色

## 📝 下一步：

1. 等待 Unity 編譯完成
2. 按照上述步驟設置場景
3. 測試 3D 飛行
4. 調整飛行參數（速度、距離、高度）
5. 添加視覺效果（粒子、光暈等）

如有問題，請告訴我！
