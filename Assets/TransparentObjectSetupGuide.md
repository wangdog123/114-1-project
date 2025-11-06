# 半透明Joy-Con陀螺儀控制物體設置指南

## 功能說明
這個腳本可以創建一個半透明的3D模型，並使用Joy-Con的**陀螺儀（重力感應）**來控制它在平面上移動。

## 設置步驟

### 1. 創建或選擇3D物體
1. 在Unity場景中創建一個3D物體（例如：Cube, Sphere, Capsule等）
   - 右鍵點擊 Hierarchy → 3D Object → 選擇你想要的形狀
2. 或者導入你自己的3D模型

### 2. 添加腳本
1. 選擇你的3D物體
2. 在Inspector視窗中，點擊 "Add Component"
3. 搜索 "claws" 並添加

### 3. ⚠️ 重要：啟用Joy-Con陀螺儀
在Windows上使用Joy-Con陀螺儀需要：

1. **使用第三方驅動**：
   - 下載並安裝 [BetterJoy](https://github.com/Davidobot/BetterJoy) 或 [JoyShockMapper](https://github.com/Electronicks/JoyShockMapper)
   - 這些工具可以讓Windows識別Joy-Con的陀螺儀

2. **或者使用Unity的Input System**：
   - 確保已安裝 Input System package
   - Joy-Con透過藍牙連接到電腦

### 4. 調整參數
在 `claws` 組件中，你可以調整：

#### 移動設定
- **Move Speed**: 移動速度（預設5）
  - 數值越大，移動越快
  - 建議範圍：1-10

- **Gyro Sensitivity**: 陀螺儀靈敏度（預設2）
  - 控制傾斜對移動的影響程度
  - 數值越大，輕微傾斜就會快速移動
  - 建議範圍：0.5-5.0

- **Tilt Threshold**: 傾斜閾值（預設0.1）
  - 忽略微小的晃動
  - 避免手部微顫造成不必要的移動
  - 建議範圍：0.05-0.3

#### 材質設定
- **Transparent Material**: （選填）自訂的半透明材質
  - 如果留空，腳本會自動創建半透明材質
- **Transparency**: 透明度（0-1）
  - 0 = 完全透明（看不見）
  - 1 = 完全不透明
  - 0.5 = 半透明（預設值）

### 5. 連接Joy-Con
1. 確保你的Joy-Con已經透過藍牙連接到電腦
2. 如果使用BetterJoy，確保軟體正在運行
3. Unity會自動讀取加速度計數據

### 6. 使用陀螺儀控制
1. 按下Unity的Play按鈕
2. 拿起Joy-Con並傾斜它：
   - **左右傾斜** = X軸移動（左右）
   - **前後傾斜** = Z軸移動（前後）
   - Y軸（高度）保持不變
3. 畫面左上角會顯示：
   - 當前加速度數據
   - 物體位置
   - 陀螺儀靈敏度

### 7. 調整靈敏度
如果物體移動：
- **太靈敏**：降低 Gyro Sensitivity 或提高 Tilt Threshold
- **不夠靈敏**：提高 Gyro Sensitivity 或降低 Tilt Threshold
- **一直在動**：提高 Tilt Threshold 來忽略微小晃動

## 進階設定

### 自訂半透明材質
如果你想使用自己的材質：

1. 在Project視窗中創建新材質：
   - 右鍵 → Create → Material
2. 在材質的Inspector中：
   - **Surface Type**: Transparent
   - **Rendering Mode**: Transparent 或 Fade
   - 調整 **Base Map** 的顏色和Alpha值
3. 將這個材質拖曳到 `claws` 組件的 "Transparent Material" 欄位

### HDRP設定（如果使用HDRP）
如果你的專案使用HDRP：

1. 材質需要使用 HDRP/Lit shader
2. 設定：
   - **Surface Type**: Transparent
   - **Blending Mode**: Alpha
   - **Preserve Specular Lighting**: 勾選（可選）
   - **Sorting Priority**: 0
3. 調整 **Base Map** 的顏色和Alpha通道

## 常見問題

### Q: 物體不是半透明的？
A: 
1. 確認材質的Surface Type設為Transparent
2. 檢查Transparency參數不是1
3. 嘗試手動創建材質並設定

### Q: Joy-Con陀螺儀無法控制？
A: 
1. **Windows用戶**：必須使用BetterJoy或類似工具
2. 確認Joy-Con已透過藍牙連接
3. 檢查Unity Console是否顯示"Joy-Con/手把已連接"
4. 嘗試在遊戲模式下查看左上角的加速度數值是否變化

### Q: 物體一直在動或不穩定？
A: 
1. 提高 **Tilt Threshold** 參數（例如0.15或0.2）
2. 降低 **Gyro Sensitivity** 參數
3. 將Joy-Con放在平面上測試是否還會移動

### Q: 傾斜Joy-Con但物體不動？
A: 
1. 提高 **Gyro Sensitivity** 參數
2. 降低 **Tilt Threshold** 參數
3. 確認加速度數值有在變化（看左上角顯示）

### Q: 移動方向跟傾斜方向相反？
A: 在代碼中調整符號：
```csharp
float moveX = -acceleration.x;  // 加上負號反轉方向
float moveZ = -acceleration.y;
```

### Q: 想要使用手機的陀螺儀？
A: 
1. 使用Unity Remote或類似工具
2. 確保在Build Settings中啟用加速度計
3. 代碼已經支援 Input.acceleration，會自動讀取

### Q: 想要限制移動範圍？
A: 可以在腳本的Update()方法中添加範圍限制：
```csharp
// 在 transform.position += movement * moveSpeed * Time.deltaTime; 之後添加：
float maxX = 10f;
float maxZ = 10f;
transform.position = new Vector3(
    Mathf.Clamp(transform.position.x, -maxX, maxX),
    transform.position.y,
    Mathf.Clamp(transform.position.z, -maxZ, maxZ)
);
```

## 技術說明

### 控制方式
- 使用Unity的 `Input.acceleration` 讀取加速度計數據
- 陀螺儀傾斜會改變加速度向量
- X軸傾斜 → 左右移動
- Y軸傾斜（手把的Y） → 前後移動（世界的Z軸）

### Joy-Con陀螺儀工作原理
- Joy-Con內建6軸陀螺儀（加速度計+陀螺儀）
- Windows需要第三方驅動才能讀取完整數據
- 傾斜角度 → 加速度變化 → 移動向量

### BetterJoy設置（推薦）
1. 下載：https://github.com/Davidobot/BetterJoy/releases
2. 解壓並執行 BetterJoyForCemu.exe
3. 連接Joy-Con（透過藍牙）
4. 軟體會將Joy-Con模擬為Xbox手把
5. Unity會自動讀取加速度數據

### 渲染方式
- 使用Standard或HDRP shader
- 透過修改材質的alpha通道實現透明
- 設定正確的渲染佇列確保正確顯示

### 移動系統
- 使用Time.deltaTime確保幀率獨立的移動
- 僅在XZ平面上移動（保持Y軸不變）
- 平滑的方向性移動
