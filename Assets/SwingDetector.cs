using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Switch;

/// <summary>
/// Joy-Con 揮動方向檢測器 - 不受握持角度影響
/// 核心概念：使用 Gyro 旋轉來將加速度從本地座標系轉換到世界座標系
/// 包含雜訊抑制與自動重新校準功能
/// </summary>
public class SwingDetector : MonoBehaviour
{
    [Header("=== 檢測參數 ===")]
    [Tooltip("揮動的加速度門檻值")]
    public float swingThreshold = 2.5f;
    
    [Tooltip("冷卻時間（秒）- 避免重複偵測")]
    public float cooldownTime = 0.3f;
    
    [Tooltip("是否啟用八方位檢測（關閉則只有上下左右四方位）")]
    public bool enableDiagonalDetection = true;
    
    [Tooltip("斜向判定的角度容差（度）")]
    [Range(10f, 45f)]
    public float diagonalAngleTolerance = 30f;
    
    [Header("=== 濾波與抗雜訊 ===")]
    [Tooltip("加速度低通濾波係數 (0-1)，越小越平滑")]
    [Range(0.1f, 1f)]
    public float accelerationSmoothing = 0.3f;
    
    [Tooltip("雜訊門檻：低於此值視為靜止狀態（抑制雜訊）")]
    [Range(0.01f, 0.5f)]
    public float noiseThreshold = 0.1f;
    
    [Tooltip("靜止偵測時間（秒）：持續靜止此時間後自動重新校準")]
    [Range(1f, 10f)]
    public float idleCalibrationTime = 3f;
    
    [Tooltip("是否啟用自動重新校準")]
    public bool enableAutoRecalibration = true;
    
    [Header("=== 轉向校正 ===")]
    [Tooltip("是否啟用轉向自動校正")]
    public bool enableRotationCalibration = true;
    
    [Tooltip("觸發轉向校正的角速度門檻（度/秒）")]
    [Range(50f, 500f)]
    public float rotationThreshold = 100f;
    
    [Tooltip("轉向後靜止多久才觸發校正（秒）")]
    [Range(0.5f, 3f)]
    public float rotationCalibrationDelay = 1f;
    
    [Header("=== Debug ===")]
    public bool showDebugInfo = true;
    public bool showGizmos = true;
    
    // 揮動方向枚舉
    public enum SwingDirection
    {
        None,
        Up,         // 向上揮
        Down,       // 向下揮
        Left,       // 向左揮
        Right,      // 向右揮
        UpLeft,     // 左上
        UpRight,    // 右上
        DownLeft,   // 左下
        DownRight   // 右下
    }
    
    // 當前檢測到的揮動方向
    private SwingDirection currentSwing = SwingDirection.None;
    
    // 冷卻計時器
    private float cooldownTimer = 0f;
    
    // 平滑後的加速度（世界座標）
    private Vector3 smoothedWorldAcceleration = Vector3.zero;
    
    // 用於校準的靜態加速度（世界座標）
    private Vector3 calibrationOffset = Vector3.zero;
    
    // 是否已校準
    private bool isCalibrated = false;
    
    // 上一幀的旋轉
    private Quaternion lastRotation = Quaternion.identity;
    
    // 雜訊抑制相關
    private float idleTimer = 0f;
    private bool isIdle = false;
    private Vector3 lastWorldAcceleration = Vector3.zero;
    
    // 高頻雜訊濾波器（中位數濾波）
    private Queue<Vector3> accelerationBuffer = new Queue<Vector3>();
    private const int BUFFER_SIZE = 5;
    
    // 漂移補償
    private float driftCompensationTimer = 0f;
    private const float DRIFT_COMPENSATION_INTERVAL = 1f;  // 每 1 秒微調一次
    
    // 轉向偵測與校正
    private bool isRotating = false;
    private float rotationStopTimer = 0f;
    private Quaternion rotationStartOrientation = Quaternion.identity;
    private bool pendingRotationCalibration = false;
    
    void Start()
    {
        if (SwitchControllerHID.current == null)
        {
            Debug.LogError("[SwingDetector] 找不到 Switch Controller！");
            enabled = false;
            return;
        }
        SwitchControllerHID.current.SetIMUEnabled(true);
        // 讀取 IMU 校準資料
        SwitchControllerHID.current.ReadUserIMUCalibrationData();
        
        // 自動校準
        StartCoroutine(AutoCalibrate());
    }
    
    void Update()
    {
        if (SwitchControllerHID.current == null) return;
        
        // 按下按鈕重新校準
        if (SwitchControllerHID.current.buttonNorth.wasPressedThisFrame)
        {
            StartCoroutine(AutoCalibrate());
            return;
        }
        
        // 更新冷卻計時器
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                currentSwing = SwingDirection.None;
            }
        }
        
        // 只在不是冷卻狀態時檢測
        if (cooldownTimer <= 0)
        {
            DetectSwing();
        }
        
        // 偵測轉向並處理校正
        if (enableRotationCalibration)
        {
            DetectRotationAndCalibrate();
        }
        
        // Debug 輸出
        if (showDebugInfo && Time.frameCount % 10 == 0)
        {
            Debug.Log($"[SwingDetector] 世界加速度: {smoothedWorldAcceleration:F2} | 當前揮動: {currentSwing}");
        }
    }
    
    /// <summary>
    /// 核心方法：檢測揮動方向
    /// </summary>
    void DetectSwing()
    {
        // 1. 取得當前 Joy-Con 的旋轉（Gyro）
        Quaternion currentRotation = SwitchControllerHID.current.deviceRotation.ReadValue();
        
        // 2. 取得加速度（本地座標系）
        Vector3 localAcceleration = SwitchControllerHID.current.acceleration.ReadValue();
        
        // 3. 【關鍵】將加速度從本地座標轉換到世界座標
        Vector3 worldAcceleration = currentRotation * localAcceleration;
        
        // 4. 減去重力偏移（校準）
        if (isCalibrated)
        {
            worldAcceleration -= calibrationOffset;
        }
        
        // 5. 中位數濾波（去除高頻雜訊尖峰）
        accelerationBuffer.Enqueue(worldAcceleration);
        if (accelerationBuffer.Count > BUFFER_SIZE)
        {
            accelerationBuffer.Dequeue();
        }
        Vector3 medianFiltered = GetMedianAcceleration();
        
        // 6. 低通濾波，平滑加速度
        smoothedWorldAcceleration = Vector3.Lerp(
            smoothedWorldAcceleration, 
            medianFiltered, 
            accelerationSmoothing
        );
        
        // 7. 雜訊抑制：小於門檻值的視為 0
        if (smoothedWorldAcceleration.magnitude < noiseThreshold)
        {
            smoothedWorldAcceleration = Vector3.zero;
        }
        
        // 8. 靜止檢測與自動重新校準
        CheckIdleState(smoothedWorldAcceleration);
        
        // 9. 檢查是否超過揮動門檻值
        float magnitude = smoothedWorldAcceleration.magnitude;
        
        if (magnitude > swingThreshold)
        {
            // 10. 重置靜止計時器（有動作）
            idleTimer = 0f;
            isIdle = false;
            
            // 11. 判斷主要方向
            SwingDirection detectedDirection = GetPrimaryDirection(smoothedWorldAcceleration);
            
            if (detectedDirection != SwingDirection.None)
            {
                OnSwingDetected(detectedDirection);
            }
        }
        
        lastRotation = currentRotation;
        lastWorldAcceleration = smoothedWorldAcceleration;
    }
    
    /// <summary>
    /// 偵測手把轉向並在停止後自動校正
    /// </summary>
    void DetectRotationAndCalibrate()
    {
        // 取得當前旋轉
        Quaternion currentRotation = SwitchControllerHID.current.deviceRotation.ReadValue();
        
        // 計算與上一幀的旋轉差異
        float rotationDelta = Quaternion.Angle(lastRotation, currentRotation);
        float rotationSpeed = rotationDelta / Time.deltaTime;  // 度/秒
        
        // 檢查是否正在旋轉（旋轉速度超過門檻）
        if (rotationSpeed > rotationThreshold)
        {
            if (!isRotating)
            {
                // 開始旋轉
                isRotating = true;
                rotationStartOrientation = lastRotation;  // 記錄開始旋轉時的方向
                
                if (showDebugInfo)
                {
                    Debug.Log($"<color=yellow>[SwingDetector] 偵測到轉向 (旋轉速度: {rotationSpeed:F1}°/s)</color>");
                }
            }
            
            // 重置停止計時器
            rotationStopTimer = 0f;
            pendingRotationCalibration = true;
        }
        else if (isRotating)
        {
            // 旋轉停止，開始計時
            rotationStopTimer += Time.deltaTime;
            
            // 停止一段時間後進行校正
            if (rotationStopTimer >= rotationCalibrationDelay && pendingRotationCalibration)
            {
                isRotating = false;
                pendingRotationCalibration = false;
                
                // 檢查旋轉角度是否足夠大（避免小抖動觸發）
                float totalRotationAngle = Quaternion.Angle(rotationStartOrientation, currentRotation);
                
                if (totalRotationAngle > 15f)  // 至少旋轉 15 度
                {
                    // 檢查是否靜止
                    if (smoothedWorldAcceleration.magnitude < noiseThreshold)
                    {
                        if (showDebugInfo)
                        {
                            Debug.Log($"<color=cyan>[SwingDetector] 轉向後校正 (總旋轉角度: {totalRotationAngle:F1}°)</color>");
                        }
                        
                        StartCoroutine(QuickCalibrate());
                    }
                }
                else
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"<color=grey>[SwingDetector] 旋轉角度太小，跳過校正 ({totalRotationAngle:F1}°)</color>");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 中位數濾波：取緩衝區的中位數（去除突波雜訊）
    /// </summary>
    Vector3 GetMedianAcceleration()
    {
        if (accelerationBuffer.Count == 0)
            return Vector3.zero;
            
        if (accelerationBuffer.Count == 1)
            return accelerationBuffer.Peek();
        
        // 簡化版：取平均值（可以改用真正的中位數）
        Vector3 sum = Vector3.zero;
        foreach (var accel in accelerationBuffer)
        {
            sum += accel;
        }
        return sum / accelerationBuffer.Count;
    }
    
    /// <summary>
    /// 檢測靜止狀態並自動重新校準
    /// </summary>
    void CheckIdleState(Vector3 currentAccel)
    {
        if (!enableAutoRecalibration) return;
        
        // 計算加速度變化
        float accelChange = Vector3.Distance(currentAccel, lastWorldAcceleration);
        
        // 如果變化很小，視為靜止
        if (accelChange < noiseThreshold && currentAccel.magnitude < noiseThreshold)
        {
            idleTimer += Time.deltaTime;
            driftCompensationTimer += Time.deltaTime;
            
            // 微量漂移補償：靜止時緩慢調整校準偏移
            if (driftCompensationTimer >= DRIFT_COMPENSATION_INTERVAL && isCalibrated)
            {
                driftCompensationTimer = 0f;
                
                // 取得當前世界加速度（未校準前）
                Quaternion rotation = SwitchControllerHID.current.deviceRotation.ReadValue();
                Vector3 localAccel = SwitchControllerHID.current.acceleration.ReadValue();
                Vector3 rawWorldAccel = rotation * localAccel;
                
                // 微調校準偏移（緩慢靠攏，避免過度補償）
                Vector3 drift = rawWorldAccel - calibrationOffset;
                if (drift.magnitude < 0.2f)  // 只在小漂移時才微調
                {
                    calibrationOffset = Vector3.Lerp(calibrationOffset, rawWorldAccel, 0.05f);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"<color=grey>[SwingDetector] 漂移補償: {drift.magnitude:F4}</color>");
                    }
                }
            }
            
            // 持續靜止超過設定時間 → 完整重新校準
            if (idleTimer >= idleCalibrationTime && !isIdle)
            {
                isIdle = true;
                Debug.Log("<color=yellow>[SwingDetector] 偵測到長時間靜止，完整重新校準...</color>");
                StartCoroutine(AutoCalibrate());
            }
        }
        else
        {
            // 有動作，重置計時器
            idleTimer = 0f;
            driftCompensationTimer = 0f;
            isIdle = false;
        }
    }
    
    /// <summary>
    /// 判斷加速度向量的主要方向（2D 平面：上下左右 + 斜向）
    /// </summary>
    SwingDirection GetPrimaryDirection(Vector3 acceleration)
    {
        // 只使用 X（左右）和 Y（上下）軸，忽略 Z 軸
        Vector2 direction2D = new Vector2(acceleration.x, acceleration.y);
        
        // 計算角度（從右方開始，逆時針）
        float angle = Mathf.Atan2(direction2D.y, direction2D.x) * Mathf.Rad2Deg;
        
        // 標準化角度到 0-360
        if (angle < 0) angle += 360f;
        
        // 如果啟用八方位檢測
        if (enableDiagonalDetection)
        {
            // 八方位判定（每個方向占 45 度）
            // 右: 337.5-22.5 (0°)
            // 右上: 22.5-67.5 (45°)
            // 上: 67.5-112.5 (90°)
            // 左上: 112.5-157.5 (135°)
            // 左: 157.5-202.5 (180°)
            // 左下: 202.5-247.5 (225°)
            // 下: 247.5-292.5 (270°)
            // 右下: 292.5-337.5 (315°)
            
            if (angle >= 337.5f || angle < 22.5f)
                return SwingDirection.Right;
            else if (angle >= 22.5f && angle < 67.5f)
                return SwingDirection.UpRight;
            else if (angle >= 67.5f && angle < 112.5f)
                return SwingDirection.Up;
            else if (angle >= 112.5f && angle < 157.5f)
                return SwingDirection.UpLeft;
            else if (angle >= 157.5f && angle < 202.5f)
                return SwingDirection.Left;
            else if (angle >= 202.5f && angle < 247.5f)
                return SwingDirection.DownLeft;
            else if (angle >= 247.5f && angle < 292.5f)
                return SwingDirection.Down;
            else if (angle >= 292.5f && angle < 337.5f)
                return SwingDirection.DownRight;
        }
        else
        {
            // 四方位判定（每個方向占 90 度）
            // 右: 315-45 (0°)
            // 上: 45-135 (90°)
            // 左: 135-225 (180°)
            // 下: 225-315 (270°)
            
            if (angle >= 315f || angle < 45f)
                return SwingDirection.Right;
            else if (angle >= 45f && angle < 135f)
                return SwingDirection.Up;
            else if (angle >= 135f && angle < 225f)
                return SwingDirection.Left;
            else if (angle >= 225f && angle < 315f)
                return SwingDirection.Down;
        }
        
        return SwingDirection.None;
    }
    
    /// <summary>
    /// 當檢測到揮動時觸發
    /// </summary>
    void OnSwingDetected(SwingDirection direction)
    {
        currentSwing = direction;
        cooldownTimer = cooldownTime;
        
        Debug.Log($"<color=green>[SwingDetector] 檢測到揮動: {direction} | 強度: {smoothedWorldAcceleration.magnitude:F2}</color>");
        
        // TODO: 在這裡觸發你的遊戲邏輯
        // 例如：檢查是否符合當前關卡要求的方向序列
    }
    
    /// <summary>
    /// 自動校準 - 取樣靜止時的重力向量
    /// </summary>
    System.Collections.IEnumerator AutoCalibrate()
    {
        Debug.Log("[SwingDetector] 開始校準... 請保持手把靜止 3 秒");
        
        yield return new WaitForSeconds(1f);
        
        Vector3 sum = Vector3.zero;
        int samples = 0;
        List<Vector3> sampleList = new List<Vector3>();
        
        // 取樣 2 秒
        float duration = 2f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            if (SwitchControllerHID.current != null)
            {
                Quaternion rotation = SwitchControllerHID.current.deviceRotation.ReadValue();
                Vector3 localAccel = SwitchControllerHID.current.acceleration.ReadValue();
                Vector3 worldAccel = rotation * localAccel;
                
                sampleList.Add(worldAccel);
                sum += worldAccel;
                samples++;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (samples > 0)
        {
            // 計算平均值
            Vector3 average = sum / samples;
            
            // 計算標準差（檢測是否真的靜止）
            float variance = 0f;
            foreach (var sample in sampleList)
            {
                variance += (sample - average).sqrMagnitude;
            }
            float stdDev = Mathf.Sqrt(variance / samples);
            
            // 如果變異數太大，表示在移動，校準失敗
            if (stdDev > 0.5f)
            {
                Debug.LogWarning($"<color=orange>[SwingDetector] 校準失敗：手把在移動（變異數: {stdDev:F3}）</color>");
                yield break;
            }
            
            calibrationOffset = average;
            isCalibrated = true;
            
            // 重置緩衝區
            accelerationBuffer.Clear();
            smoothedWorldAcceleration = Vector3.zero;
            
            Debug.Log($"<color=cyan>[SwingDetector] 校準完成！重力偏移: {calibrationOffset:F3} | 穩定度: {stdDev:F3}</color>");
        }
        else
        {
            Debug.LogWarning("[SwingDetector] 校準失敗：無法取得樣本");
        }
    }
    
    /// <summary>
    /// 快速校準 - 用於轉向後的快速校正（採樣時間較短）
    /// </summary>
    System.Collections.IEnumerator QuickCalibrate()
    {
        if (showDebugInfo)
        {
            Debug.Log("[SwingDetector] 快速校準中...");
        }
        
        yield return new WaitForSeconds(0.3f);
        
        Vector3 sum = Vector3.zero;
        int samples = 0;
        List<Vector3> sampleList = new List<Vector3>();
        
        // 快速取樣 0.5 秒
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            if (SwitchControllerHID.current != null)
            {
                Quaternion rotation = SwitchControllerHID.current.deviceRotation.ReadValue();
                Vector3 localAccel = SwitchControllerHID.current.acceleration.ReadValue();
                Vector3 worldAccel = rotation * localAccel;
                
                sampleList.Add(worldAccel);
                sum += worldAccel;
                samples++;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (samples > 0)
        {
            // 計算平均值
            Vector3 average = sum / samples;
            
            // 計算標準差
            float variance = 0f;
            foreach (var sample in sampleList)
            {
                variance += (sample - average).sqrMagnitude;
            }
            float stdDev = Mathf.Sqrt(variance / samples);
            
            // 快速校準容許較大的變異（因為可能剛停止轉動）
            if (stdDev > 1.0f)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"<color=orange>[SwingDetector] 快速校準跳過：手把不夠穩定 (變異: {stdDev:F3})</color>");
                }
                yield break;
            }
            
            // 更新校準偏移
            calibrationOffset = average;
            
            // 重置緩衝區
            accelerationBuffer.Clear();
            smoothedWorldAcceleration = Vector3.zero;
            
            if (showDebugInfo)
            {
                Debug.Log($"<color=cyan>[SwingDetector] 快速校準完成！新偏移: {calibrationOffset:F3}</color>");
            }
        }
    }
    
    /// <summary>
    /// 取得當前偵測到的揮動方向（供外部使用）
    /// </summary>
    public SwingDirection GetCurrentSwing()
    {
        return currentSwing;
    }
    
    /// <summary>
    /// 檢查是否在冷卻中
    /// </summary>
    public bool IsInCooldown()
    {
        return cooldownTimer > 0;
    }
    
    /// <summary>
    /// 取得世界座標的加速度（供外部使用）
    /// </summary>
    public Vector3 GetWorldAcceleration()
    {
        return smoothedWorldAcceleration;
    }
    
    // Debug 可視化
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying) return;
        
        // 畫出世界座標的加速度向量
        Vector3 center = transform.position;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 0.1f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(center, smoothedWorldAcceleration * 0.5f);
        
        // 顯示方向文字（需要在 Scene 視窗才看得到）
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(
            center + smoothedWorldAcceleration * 0.5f,
            $"{currentSwing}\n{smoothedWorldAcceleration.magnitude:F2}"
        );
        #endif
    }
}
