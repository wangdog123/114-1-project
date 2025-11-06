# 🎯 瞄準鏡系統設置指南

## 📋 系統選擇

根據你的需求，我提供了三個不同的瞄準鏡系統：

### 1. **ScopeWithSmooth.cs** (推薦)
- ✅ **不依賴Cinemachine** - 直接使用Unity Camera
- ✅ **內建平滑效果** - 使用SmoothDamp和Lerp
- ✅ **易於調整** - 所有參數都有Range滑桿
- ✅ **完整功能** - 包含縮放、晃動、心率影響

### 2. **scope.cs** (Cinemachine版本)
- ✅ **專業級平滑** - 使用Cinemachine的平滑系統
- ✅ **更多控制選項** - 支援Transposer和FramingTransposer
- ✅ **噪聲系統** - 使用Cinemachine的噪聲組件
- ❌ **需要Cinemachine** - 必須安裝Cinemachine包

### 3. **SimpleScope.cs** (簡化版)
- ✅ **快速設置** - 最簡單的配置
- ✅ **Cinemachine優化** - 專為Cinemachine設計
- ❌ **功能較少** - 簡化版功能

## 🚀 快速開始

### 方法1：使用ScopeWithSmooth (推薦)

1. **創建相機**
   ```
   GameObject → Camera
   命名為 "ScopeCamera"
   ```

2. **添加腳本**
   ```
   將ScopeWithSmooth.cs附加到相機上
   ```

3. **設置參數**
   - Sensitivity: 2.0 (滑鼠靈敏度)
   - Max Zoom: 3.0 (最大縮放)
   - Min Zoom: 1.0 (最小縮放)
   - Shake Intensity: 0.1 (晃動強度)

4. **設置目標**
   ```
   Target: 拖拽玩家物件到Target欄位
   ```

### 方法2：使用Cinemachine版本

1. **安裝Cinemachine**
   ```
   Window → Package Manager → Cinemachine
   安裝Cinemachine包
   ```

2. **創建虛擬相機**
   ```
   右鍵 Hierarchy → Cinemachine → Virtual Camera
   命名為 "ScopeCamera"
   ```

3. **添加組件**
   ```
   - Transposer (跟隨和偏移)
   - Basic Multi Channel Perlin (晃動)
   ```

4. **添加腳本**
   ```
   將scope.cs附加到相機上
   設置vcam引用
   ```

## ⚙️ 參數調整指南

### 基本設置
- **Sensitivity**: 滑鼠靈敏度 (1-3為佳)
- **Max Zoom**: 最大縮放倍數 (2-4為佳)
- **Min Zoom**: 最小縮放倍數 (0.5-1.5為佳)

### 平滑設置
- **Position Smooth Time**: 位置平滑時間 (0.05-0.2為佳)
- **Zoom Smooth Time**: 縮放平滑時間 (0.1-0.5為佳)
- **Shake Smooth Time**: 晃動平滑時間 (0.01-0.1為佳)

### 晃動設置
- **Shake Intensity**: 晃動強度 (0.05-0.3為佳)
- **Shake Frequency**: 晃動頻率 (1-3為佳)
- **Zoom Shake Multiplier**: 縮放晃動倍數 (1-3為佳)

### 心率影響
- **Heart Rate**: 心率 (60-120 BPM)
- **Heart Rate Effect**: 心率影響強度 (0.3-0.8為佳)

## 🎮 控制方式

### 滑鼠控制
- **移動**: 滑鼠移動控制瞄準鏡位置
- **縮放**: 滑鼠滾輪控制縮放
- **射擊**: 左鍵射擊

### 手把控制
- **移動**: 右搖桿控制瞄準鏡位置
- **縮放**: 肩部按鈕控制縮放
- **射擊**: RT按鈕射擊

## 🔧 進階設置

### 自定義晃動
```csharp
// 設置晃動參數
scopeScript.SetShake(0.2f, 2.5f);
```

### 自定義平滑
```csharp
// 設置平滑參數
scopeScript.SetSmoothness(0.1f, 0.3f, 0.05f);
```

### 心率監測
```csharp
// 設置心率
scopeScript.SetHeartRate(85f);
```

## 🐛 常見問題

### Q: 相機不跟隨玩家？
A: 檢查Target是否設置正確

### Q: 縮放效果不明顯？
A: 調整Max Zoom和Min Zoom數值

### Q: 晃動太強/太弱？
A: 調整Shake Intensity和Zoom Shake Multiplier

### Q: 移動不夠平滑？
A: 降低Position Smooth Time數值

## 📝 最佳實踐

1. **開始時使用較低的參數值**
2. **逐步調整到滿意效果**
3. **測試不同縮放級別的晃動效果**
4. **根據遊戲難度調整心率影響**
5. **使用Range滑桿進行實時調整**

## 🎯 遊戲整合

將瞄準鏡系統與其他組件整合：

1. **ShootingSystem.cs** - 射擊系統
2. **HeartRateMonitor.cs** - 心率監測
3. **GameManager.cs** - 遊戲管理
4. **PestController.cs** - 害蟲控制

這樣就能創造出完整的"家政阿姨"射擊遊戲體驗！
