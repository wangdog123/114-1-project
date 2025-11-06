using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Switch;

public class swingtest : MonoBehaviour
{
    [Header("1. 衝擊門檻 (角加速度)")]
    [Tooltip("角加速度『總強度』要超過這個值（例如 15000）")]
    public float jerkThreshold = 15000f; 

    [Header("2. 冷卻門檻 (角速度)")]
    [Tooltip("角速度要低於此值，才算『靜止』重置狀態")]
    public float cooldownGyroThreshold = 10f; 
    
    [Header("3. 軸向過濾 (Roll/Twist)")]
    [Tooltip("如果 Y 軸衝擊最大，視為手腕扭轉 (Twist)，將會被忽略。")]
    public float yAxisTwistThreshold = 0.5f; // 設為 0.5f，表示 Y 軸衝擊不能超過 X 或 Z 衝擊的 50%

    [Header("Debug")]
    public bool isDebugging = true;

    // 揮擊方向的枚舉 (Enum)
    public enum SwingDirection { None, Up, Down, Left, Right }

    // 偵測狀態
    private enum State { Ready, Cooldown }
    private State currentState = State.Ready;

    // 用來儲存上一幀的角速度
    private Vector3 lastAngularVelocity = Vector3.zero;

    void Start()
    {
        if (SwitchControllerHID.current == null) {
            Debug.LogError("找不到 Switch 控制器！"); this.enabled = false; 
        }
    }

    void Update() 
    {
        if (SwitchControllerHID.current == null) return;

        // --- 1. 取得「角速度」並計算「角加速度 (Jerk)」 ---
        Vector3 currentAngularVelocity = SwitchControllerHID.current.angularVelocity.ReadValue();

        float dt = Time.deltaTime;
        if (dt < 0.0001f) dt = 0.0001f; 
        
        Vector3 angularAcceleration = (currentAngularVelocity - lastAngularVelocity) / dt;
        float jerkMagnitude = angularAcceleration.magnitude;

        // --- 2. 除錯 Log ---
        if (isDebugging && Time.frameCount % 5 == 0)
        {
            Debug.Log($"Jerk: {jerkMagnitude:F0} (門檻: {jerkThreshold}) | Gyro: {currentAngularVelocity.magnitude:F1} | State: {currentState}");
        }

        // --- 3. 狀態機 ---
        switch (currentState)
        {
            case State.Ready:
                // 【步驟 1】衝擊力是否足夠？
                if (jerkMagnitude > jerkThreshold)
                {
                    // 執行方向判斷
                    DetectDirection(angularAcceleration);
                }
                break;

            case State.Cooldown:
                // 【用角速度重置】
                if (currentAngularVelocity.magnitude < cooldownGyroThreshold)
                {
                    currentState = State.Ready;
                }
                break;
        }

        // 4. 儲存這一幀的角速度
        lastAngularVelocity = currentAngularVelocity;
    }

    /// <summary>
    /// 在偵測到衝擊的「當下」，計算揮擊的「方向」
    /// </summary>
    private void DetectDirection(Vector3 angularAcceleration)
    {
        // 根據您的實驗結果：
        // X 軸 = 左右揮
        // Z 軸 = 上下揮
        // Y 軸 = 手腕扭轉 (Twist/Roll)
        
        float jerkX = angularAcceleration.x; // 左右衝擊
        float jerkZ = angularAcceleration.z; // 上下衝擊
        float jerkY = angularAcceleration.y; // 扭轉衝擊

        float absX = Mathf.Abs(jerkX);
        float absZ = Mathf.Abs(jerkZ);
        float absY = Mathf.Abs(jerkY);

        // 判斷是否為無效的「扭轉」動作：如果 Y 軸衝擊遠大於其他兩軸
        if (absY > absX * yAxisTwistThreshold && absY > absZ * yAxisTwistThreshold)
        {
            // Y 軸衝擊力太大，判定為手腕扭轉 (Twist/Roll)，忽略此揮擊
            if (isDebugging) Debug.Log("Twist 動作被忽略 (Y 軸衝擊過大)");
            return;
        }

        SwingDirection direction = SwingDirection.None;

        // 找出 X (左右) 還是 Z (上下) 的衝擊力最強
        if (absX > absZ)
        {
            // X 軸最強 -> 左右
            direction = (jerkX > 0) ? SwingDirection.Right : SwingDirection.Left;
        }
        else
        {
            // Z 軸最強 -> 上下
            direction = (jerkZ > 0) ? SwingDirection.Up : SwingDirection.Down;
        }
        
        if (direction != SwingDirection.None)
        {
            OnSwingDetected(direction, angularAcceleration.magnitude);
        }
    }


    private void OnSwingDetected(SwingDirection direction, float jerkStrength)
    {
        // 判斷方向後，您的遊戲邏輯就在這裡執行
        Debug.LogWarning($"<<<<< 偵測到衝擊：{direction} (Jerk: {jerkStrength:F0}) >>>>>");
        
        // 立刻進入 Cooldown 狀態
        currentState = State.Cooldown;
    }
}