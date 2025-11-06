# 划痕节奏游戏 - 设置指南

## 游戏机制

玩家需要**记住**屏幕上显示的划痕序列（例如：左→右→上），然后在限定时间内，用陀螺仪控制虚拟光标按**正确的顺序**划过目标。

### 游戏流程
1. 显示序列：`← → ↑` (2秒)
2. 序列消失，显示目标
3. 玩家用手柄控制光标，按顺序划过目标
4. 正确：进入下一关（更多划痕，更快速度）
5. 错误：重新开始（降低难度）

---

## Unity 设置步骤

### 1. 创建 Canvas
1. Hierarchy → 右键 → UI → Canvas
2. Canvas Scaler → UI Scale Mode → Scale With Screen Size
3. Reference Resolution → 1920 x 1080

### 2. 创建目标预制体（Slash Target Prefab）

**步骤：**
1. Hierarchy → 右键 → UI → Image
2. 命名为 "SlashTarget"
3. 设置属性：
   - Width: 100
   - Height: 100
   - Color: 白色（或任何你喜欢的颜色）
   
4. 添加子对象（显示方向文字）：
   - 右键 SlashTarget → UI → Text
   - 命名为 "DirectionText"
   - 设置：
     - Font Size: 40
     - Alignment: 居中
     - Color: 黑色
     - Text: "←1"（示例）

5. 将 SlashTarget 拖到 Project 面板创建预制体
6. 删除 Hierarchy 中的 SlashTarget（预制体已保存）

### 3. 创建 UI 元素

**在 Canvas 下创建：**

**a) Slash Targets Parent**
```
Empty GameObject
Name: "SlashTargetsParent"
Position: 中心 (0, 0, 0)
```

**b) Sequence Display Text**
```
UI → Text
Name: "SequenceDisplay"
Position: 顶部中央 (0, 400, 0)
Font Size: 50
Alignment: 居中
Text: "准备开始..."
```

**c) Score Text**
```
UI → Text
Name: "ScoreText"
Position: 左上角 (-800, 450, 0)
Font Size: 30
Alignment: 左对齐
Text: "Score: 0
Level: 1
Combo: x0"
```

**d) Feedback Text**
```
UI → Text
Name: "FeedbackText"
Position: 中央 (0, 0, 0)
Font Size: 80
Alignment: 居中
Color: 黄色
Text: "Perfect!"
默认隐藏（Disable GameObject）
```

### 4. 设置游戏管理器

1. 创建空 GameObject：`GameManager`
2. 添加脚本 `ScratchRhythmGame.cs`
3. 在 Inspector 中设置：

```
Cursor: 拖入你的 VirtualCursor 对象
Scratch Detection Radius: 50
Sequence Length: 3
Display Time: 2
Action Time: 3

Current Level: 1
Speed Multiplier: 1

Slash Targets Parent: 拖入 SlashTargetsParent
Slash Target Prefab: 拖入 SlashTarget 预制体
Sequence Display Text: 拖入 SequenceDisplay
Score Text: 拖入 ScoreText
Feedback Text: 拖入 FeedbackText
```

### 5. 确保 VirtualCursor 正常工作

在你的 `test.cs` 中：
```
Enable Mouse Control: ✓ 勾选
Virtual Cursor: 拖入 CursorManager
```

---

## 游戏参数调整

### 难度控制
```csharp
sequenceLength = 3      // 初始序列长度（建议 2-4）
displayTime = 2f        // 显示时间（秒）
actionTime = 3f         // 操作时间（秒）
speedMultiplier = 1f    // 速度倍数（自动增加）
```

### 检测控制
```csharp
scratchDetectionRadius = 50f  // 光标检测半径（像素）
                              // 越大越容易命中，越小越难
```

---

## 游戏逻辑

### 难度递增
- 每完成一轮 → Level +1
- 序列长度 = 基础长度 + (Level - 1)
- 速度倍数 +0.1 (显示/操作时间缩短)

### 难度递减（失败时）
- Level -1 (最低 Level 1)
- 速度倍数 -0.1 (最低 1.0)

### 计分系统
- 基础分: 100 分/目标
- Combo 加成: 分数 × Combo 数
- 例如：Combo x3 → 300 分

---

## 测试

1. 运行游戏
2. 应该看到：`记住这个顺序：← → ↑`
3. 2秒后序列消失，屏幕上出现3个目标
4. 用手柄移动光标，按顺序划过目标
5. 正确 → "Perfect!" → 下一关更难
6. 错误 → "顺序错误！" → 重新开始

---

## 扩展建议

### 视觉效果
- 添加粒子效果（划过目标时）
- 添加轨迹残影（光标移动时）
- 目标呼吸动画（等待击中时）

### 音效
- 显示序列时的音效
- 击中目标的音效（正确/错误不同）
- 背景音乐（随 BPM 变化）

### 游戏模式
- 自由模式：无限模式，看能达到多少分
- 挑战模式：固定关卡，每关特定难度
- 对战模式：双人比拼速度和准确度

---

## 故障排除

**问题：光标不动**
- 检查 `test.cs` 中 `enableMouseControl` 是否开启
- 检查 `virtualCursor` 是否正确引用

**问题：检测不到碰撞**
- 调大 `scratchDetectionRadius`
- 检查 VirtualCursor.GetCursorPosition() 是否返回正确坐标

**问题：目标不显示**
- 检查 `slashTargetPrefab` 是否正确设置
- 检查 Canvas 是否正确设置

**问题：方向反了**
- 回到 `test.cs` 调整轴映射
- 按 B 键重新校准朝向
