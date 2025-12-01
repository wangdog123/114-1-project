using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Switch; // 確保你已安裝 Input System 和 Switch HID Support
using UnityEngine.UIElements;
using Unity.Mathematics;

public class test : MonoBehaviour
{
    [Header("控制器設定")]
    public MultiSwitchControllerManager controllerManager; // 控制器管理器
    public int controllerIndex = 0; // 使用哪一個控制器（0 = 第一個，1 = 第二個）

    public Vector3 acclelOffset = new Vector3(0.97f, -0.01f, -0.2f);
    public Vector3 acclelOffsetwhen45 = new Vector3(0.45f, 0.35f, 0.8f);

    [Header("移动设置")]
    public GameObject targetObject; // 要移动的物体
    public float moveDistance = 2f; // 每次移动的距离
    public float moveSpeed = 10f; // 移动速度

    [Header("鼠标控制设置")]
    public bool enableMouseControl = true; // 是否启用鼠标控制
    public VirtualCursor virtualCursor; // 虚拟光标脚本引用
    public float mouseSensitivity = 0.5f; // 鼠标灵敏度（降低以获得更好的控制）
    public Vector3 gyroDeadzone = new Vector3(2f, 2f, 2f); // 每个轴独立的死区（根据测试数据调整）
    
    [Header("陀螺仪校准")]
    [Tooltip("手动设置陀螺仪偏移，或按按钮自动校准")]
    public Vector3 gyroOffset = new Vector3(-0.3f, -1.35f, 0.89f); // 陀螺仪漂移校准偏移（固定值）
    public bool Calibrated = false; // 是否在开始时自动校准
    public bool calibrateOrientation = true; // 是否校准初始朝向（解决倒着拿的问题）
    
    [Header("世界坐标设置")]
    [Tooltip("使用世界坐标系，不受手柄旋转影响")]
    public bool useWorldCoordinates = true; // 开启世界坐标系，这样不管怎么拿手柄方向都对
    
    [Header("调试模式")]
    public bool showDebugInfo = true; // 显示详细调试信息
    public bool drawGyroGizmos = true; // 在 Gizmos 中显示陀螺仪数据


    // 【已修正】orientation 是 Quaternion (四元數)，不是 Vector3
    // private Vector3 orientation; // <- 原始錯誤代碼
    private Quaternion orientation; // <- 已修正

    private Vector3 aRaw;
    private Vector3 aWorld;
    public Vector3 alinear; // 公開給其他腳本使用（線性加速度）
    private Vector3 amag;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private float lastSwingTime = 0f;
    private float swingCooldown = 0.3f; // 挥动冷却时间，避免重复触发

    // 用于追踪陀螺仪的相对位置
    private Vector3 lastGyro;
    private Vector3 gyroPosition; // 陀螺仪积分后的虚拟位置
    
    // 初始朝向校准
    private Quaternion initialOrientation; // 游戏开始时的手柄朝向
    private bool orientationCalibrated = false;
    
    // 调试数据
    private Vector3 currentGyro;
    private Vector3 worldGyro;

    // 當前使用的控制器
    private SwitchControllerHID currentController;


    // Start is called before the first frame update
    void Start()
    {
        // 如果沒有指定管理器，嘗試自動找到
        if (controllerManager == null)
        {
            controllerManager = FindObjectOfType<MultiSwitchControllerManager>();
        }

        if (controllerManager == null)
        {
            Debug.LogError("找不到 MultiSwitchControllerManager！請在場景中添加此組件。");
            return;
        }

        // 等待一幀確保控制器列表已更新
        StartCoroutine(InitializeController());
    }

    IEnumerator InitializeController()
    {
        yield return null; // 等待一幀

        // 獲取指定索引的控制器
        currentController = controllerManager.GetController(controllerIndex);

        if (currentController == null)
        {
            Debug.LogError($"找不到索引 {controllerIndex} 的控制器！當前連接: {controllerManager.ControllerCount} 個");
            yield break;
        }

        Debug.Log($"使用控制器 {controllerIndex}: {currentController.name} (ID: {currentController.deviceId})");

        currentController.SetIMUEnabled(true);
        // 讀取 IMU 校準資料
        currentController.ReadUserIMUCalibrationData();

        // 初始化陀螺仪数据
        lastGyro = currentController.angularVelocity.ReadValue();
        gyroPosition = Vector3.zero;
        
        // 如果启用自动校准，启动校准协程
        // if (!Calibrated)
        // {
        //     StartCoroutine(CalibrateGyroOffset());
        // }
        
        // 记录初始朝向（解决倒着拿的问题）
        // if (calibrateOrientation)
        // {
        //     Vector3 initialOrientationEuler = SwitchControllerHID.current.orientation.ReadValue();
        //     initialOrientation = Quaternion.Euler(initialOrientationEuler);
        //     orientationCalibrated = true;
        //     Debug.Log($"初始朝向已记录: {initialOrientationEuler}");
        // }
    }
    
    // 陀螺仪偏移校准协程（仅在需要时调用）
    IEnumerator CalibrateGyroOffset()
    {
        Debug.Log("开始陀螺仪校准，请将手柄平放在桌上保持静止 3 秒...");
        yield return new WaitForSeconds(0.5f); // 等待 1 秒稳定
        
        if (currentController == null) yield break;

        // 多次采样取平均，减少误差
        Vector3 sum = Vector3.zero;
        int samples = 30;
        
        for (int i = 0; i < samples; i++)
        {
            sum += currentController.angularVelocity.ReadValue();
            yield return new WaitForSeconds(0.1f);
        }
        
        gyroOffset = sum / samples;
        Debug.Log($"陀螺仪校准完成！偏移值: {gyroOffset}");
    }

    // Update is called once per frame
    void Update()
    {
        if (currentController == null)
            return;
        // 按 West 键（Y键）暂停/继续
            if (currentController.buttonWest.wasPressedThisFrame)
            {
                if (Time.timeScale > 0f)
                {
                    Time.timeScale = 0f;
                }
                else
                {
                    Time.timeScale = 1f;
                }
                return;
            }
            
            // 按 North 键（X键）手动校准陀螺仪偏移
            if (currentController.buttonNorth.wasPressedThisFrame)
            {
                StartCoroutine(CalibrateGyroOffset());
                return;
            }
            
            // 按 South 键（B键）重新校准朝向（当你改变握持方式时）
            if (controllerManager.Controllers[controllerIndex].buttonSouth.wasPressedThisFrame)
            {
                Vector3 recalibratedOrientationEuler = currentController.orientation.ReadValue();
                initialOrientation = Quaternion.Euler(recalibratedOrientationEuler);
                orientationCalibrated = true;
                Calibrated = true;
                Debug.Log($"朝向已重新校准: {recalibratedOrientationEuler}");
            }
            if (controllerManager.Controllers[controllerIndex].dpad.down.wasPressedThisFrame)
            {
                Vector3 recalibratedOrientationEuler = currentController.orientation.ReadValue();
                initialOrientation = Quaternion.Euler(recalibratedOrientationEuler);
                orientationCalibrated = true;
                Calibrated = true;
                Debug.Log($"朝向已重新校准: {recalibratedOrientationEuler}");
            }

            if (Time.timeScale <= 0f)
            return; // 如果游戏暂停，则跳过所有逻辑

            if (!Calibrated)
                return;
            // 讀取 IMU 數據
            aRaw = currentController.acceleration.ReadValue();
            Vector3 orientationEuler = currentController.orientation.ReadValue();
            orientation = Quaternion.Euler(orientationEuler); // 將 Vector3 歐拉角轉換為 Quaternion

            // 【已修正】使用 Quaternion 直接計算世界座標加速度
            // aWorld = Quaternion.Euler(orientation) * aRaw; // <- 原始錯誤代碼
            aWorld = orientation * aRaw; // <- 已修正

            // 移除重力/偏移量，得到用戶施加的線性加速度
            alinear = aWorld - acclelOffset;
            amag = alinear - Vector3.up * Vector3.Dot(alinear, Vector3.up);

            // Debug Log (保持你原有的)
            if (showDebugInfo)
            {
                // Debug.Log($"=== IMU 数据 ===");
                // Debug.Log($"aRaw (原始加速度): {aRaw}");
                // Debug.Log($"aWorld (世界加速度): {aWorld}");
                // Debug.Log($"alinear (线性加速度): {alinear}");
                // Debug.Log($"orientation (四元数): {orientation}");
                // Debug.Log($"orientationEuler (欧拉角): {orientationEuler}");
            }


            // 鼠标控制 - 【已修改】调用新的 MoveMouse 逻辑
            if (enableMouseControl)
            {
                MoveMouse();
            }

            // 判断上下左右挥动 (你的原始邏輯，保持不變)
            float threshold = 2.0f;

            if (Mathf.Abs(alinear.x) > threshold || Mathf.Abs(alinear.z) > threshold)
            {
                // 冷却时间检查，避免重复触发
                if (Time.time - lastSwingTime < swingCooldown)
                    return;

                lastSwingTime = Time.time;

                // 比较哪个轴的变化更大
                if (Mathf.Abs(alinear.z) > Mathf.Abs(alinear.x))
                {
                    // 上下挥动
                    if (alinear.z > 0)
                    {
                        Debug.Log("向上挥");
                        MoveObject(Vector3.forward); // 向前移动
                    }
                    else
                    {
                        Debug.Log("向下挥");
                        MoveObject(Vector3.back); // 向后移动
                    }
                }
                else
                {
                    // 左右挥动
                    if (alinear.x > 0)
                    {
                        Debug.Log("向右挥");
                        MoveObject(Vector3.right); // 向右移动
                    }
                    else
                    {
                        Debug.Log("向左挥");
                        MoveObject(Vector3.left); // 向左移动
                    }
                }
            }

            // 平滑移动物体到目标位置 (你的原始邏輯，保持不變)
            if (isMoving && targetObject != null)
            {
                targetObject.transform.position = Vector3.MoveTowards(
                    targetObject.transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );

                // 到达目标位置后停止移动
                if (Vector3.Distance(targetObject.transform.position, targetPosition) < 0.01f)
                {
                    targetObject.transform.position = targetPosition;
                    isMoving = false;
                }
            }
    }

    // 移动物体的方法 (你的原始邏輯，保持不變)
    void MoveObject(Vector3 direction)
    {
        if (targetObject != null)
        {
            targetPosition = targetObject.transform.position + direction * moveDistance;
            isMoving = true;
        }
    }

    // 鼠标移动控制 - 使用陀螺仪追踪现实世界的 XY 平面移动
    void MoveMouse()
    {
        // 1. 读取陀螺仪数据（角速度，单位：弧度/秒）
        currentGyro = currentController.angularVelocity.ReadValue();
        
        // 2. 移除陀螺仪漂移偏移（固定偏移值，不管手柄怎么拿）
        Vector3 calibratedGyro = currentGyro;

        Vector3 processedGyro;
        
        if (useWorldCoordinates && orientationCalibrated)
        {
            // 计算相对于初始朝向的旋转
            Quaternion relativeOrientation = Quaternion.Inverse(initialOrientation) * orientation;
            
            // 将陀螺仪数据转换到相对坐标系
            worldGyro = relativeOrientation * calibratedGyro;
            processedGyro = worldGyro;
        }
        else if (useWorldCoordinates)
        {
            // 将陀螺仪数据转换到世界坐标系（不受手柄旋转影响）
            worldGyro = orientation * calibratedGyro;
            processedGyro = worldGyro;
        }
        else
        {
            // 使用手柄本地坐标系（更直观）
            worldGyro = calibratedGyro;
            processedGyro = calibratedGyro;
        }

        // 3. 根据你的测试数据映射轴
        // 你的反馈：
        // - 往上 → 应该往上，但现在往右下 (需要调整)
        // - 往下 → 应该往下，但现在往左上 (需要调整)
        // - 往左 → 应该往左，但现在往上偏右 (需要调整)
        // - 往右 → 应该往右，但现在往下偏左 (需要调整)
        
        // 重新映射（根据实际测试结果）
        float gyroX = processedGyro.z; // 左右移动 → 鼠标 X 轴（反向）
        float gyroY = processedGyro.y; // 上下移动 → 鼠标 Y 轴（反向）

        // 4. 应用死区（每个轴独立，避免漂移）
        if (Mathf.Abs(gyroX) < gyroDeadzone.x) gyroX = 0f;
        if (Mathf.Abs(gyroY) < gyroDeadzone.y) gyroY = 0f;

        // 5. 计算这一帧的移动量
        float mouseX = gyroX * mouseSensitivity * Time.deltaTime;
        float mouseY = gyroY * mouseSensitivity * Time.deltaTime;

        // Debug 信息
        if (showDebugInfo)
        {
            // Debug.Log($"=== 陀螺仪数据 ===");
            // Debug.Log($"Gyro 原始: {currentGyro}");
            // Debug.Log($"Gyro 校准后: {calibratedGyro}");
            // Debug.Log($"处理后 gyroX(使用X轴): {gyroX:F2}, gyroY(使用Y轴): {gyroY:F2}");
            // Debug.Log($"鼠标移动: mouseX={mouseX:F4}, mouseY={mouseY:F4}");
        }

        // 6. 移动虚拟光标
        if ((Mathf.Abs(mouseX) > 0.0001f || Mathf.Abs(mouseY) > 0.0001f) && virtualCursor != null)
        {
            if(this.name=="left")
                virtualCursor.MoveCursor(-mouseX, -mouseY);
            else
                virtualCursor.MoveCursor(mouseX, mouseY);
        }

        // 更新上一帧的陀螺仪数据
        lastGyro = currentGyro;
    }

    // OnDrawGizmos (你的原始邏輯，保持不變)
    void OnDrawGizmos()
    {
        // 绘制坐标轴参考线
        Vector3 center = transform.position;

        // X轴 (红色 - 左右)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(center - Vector3.right * 2, center + Vector3.right * 2);
        Gizmos.DrawSphere(center + Vector3.right * 2, 0.1f);

        // Y轴 (绿色)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(center - Vector3.up * 2, center + Vector3.up * 2);

        // Z轴 (蓝色 - 上下)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(center - Vector3.forward * 2, center + Vector3.forward * 2);
        Gizmos.DrawSphere(center + Vector3.forward * 2, 0.1f);

        // 绘制加速度向量
        if (Application.isPlaying)
        {
            // alinear 向量 (黄色)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(center, center + alinear);
            Gizmos.DrawSphere(center + alinear, 0.15f);

            // 绘制 X 和 Z 分量
            Gizmos.color = new Color(1f, 0.5f, 0f); // 橙色 - X分量
            Gizmos.DrawLine(center, center + Vector3.right * alinear.x);

            Gizmos.color = Color.cyan; // 青色 - Z分量
            Gizmos.DrawLine(center, center + Vector3.forward * alinear.z);

            // === 新增：绘制陀螺仪数据 ===
            if (drawGyroGizmos)
            {
                Vector3 gyroCenter = center + Vector3.up * 3; // 在物体上方显示

                // 陀螺仪原始数据 (白色)
                Gizmos.color = Color.white;
                Gizmos.DrawLine(gyroCenter, gyroCenter + currentGyro * 2);
                Gizmos.DrawSphere(gyroCenter + currentGyro * 2, 0.1f);

                // 陀螺仪世界坐标 (洋红色)
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(gyroCenter, gyroCenter + worldGyro * 2);
                Gizmos.DrawSphere(gyroCenter + worldGyro * 2, 0.12f);

                // X 轴分量 (红色) - Yaw
                Gizmos.color = Color.red;
                Gizmos.DrawLine(gyroCenter, gyroCenter + Vector3.right * worldGyro.y * 2);

                // Y 轴分量 (蓝色) - Pitch
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(gyroCenter, gyroCenter + Vector3.forward * worldGyro.x * 2);
            }

            // 显示当前主导方向
            float threshold = 2.0f;
            if (Mathf.Abs(alinear.x) > threshold || Mathf.Abs(alinear.z) > threshold)
            {
                if (Mathf.Abs(alinear.z) > Mathf.Abs(alinear.x))
                {
                    // 上下挥动 - 放大显示Z轴
                    Gizmos.color = alinear.z > 0 ? Color.cyan : Color.blue;
                    Gizmos.DrawLine(center, center + Vector3.forward * alinear.z * 1.5f);
                }
                else
                {
                    // 左右挥动 - 放大显示X轴
                    Gizmos.color = alinear.x > 0 ? Color.magenta : Color.red;
                    Gizmos.DrawLine(center, center + Vector3.right * alinear.x * 1.5f);
                }
            }
        }
    }
    
    // 重置陀螺儀追蹤狀態（當光標重置時呼叫）
    public void ResetGyroTracking()
    {
        if (currentController != null)
        {
            lastGyro = currentController.angularVelocity.ReadValue();
            gyroPosition = Vector3.zero;
            Debug.Log("[test] 陀螺儀追蹤已重置");
        }
    }
}