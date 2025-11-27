using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Switch;

/// <summary>
/// 管理多個 Switch 控制器，讓兩隻手把可以同時使用
/// </summary>
public class MultiSwitchControllerManager : MonoBehaviour
{
    [Header("控制器設定")]
    [Tooltip("是否在控制器連接/斷開時自動更新列表")]
    public bool autoUpdateControllers = true;
    
    [Tooltip("是否自動初始化新連接的控制器（關閉後需要手動調用 InitializeNewControllers）")]
    public bool autoInitializeNewControllers = true;

    [Header("調試資訊")]
    public bool showDebugInfo = true;

    // 存儲所有連接的 Switch 控制器
    private List<SwitchControllerHID> controllers = new List<SwitchControllerHID>();
    
    // 記錄已初始化過的控制器 ID
    private HashSet<int> initializedControllerIds = new HashSet<int>();

    // 校正數據結構
    [System.Serializable]
    public struct JoyConCalibrationData
    {
        public Vector3 gyroOffset;
        public Quaternion initialOrientation;
        public bool isCalibrated;
    }

    // 存儲每個控制器的校正數據 (Key: Device ID)
    private Dictionary<int, JoyConCalibrationData> calibrationDataMap = new Dictionary<int, JoyConCalibrationData>();

    // 單例模式
    private static MultiSwitchControllerManager instance;
    public static MultiSwitchControllerManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MultiSwitchControllerManager>();
            }
            return instance;
        }
    }

    // 公開屬性，方便其他腳本訪問
    public List<SwitchControllerHID> Controllers => controllers;

    /// <summary>
    /// 獲取第一個控制器（Player 1）
    /// </summary>
    public SwitchControllerHID Controller1 => controllers.Count > 0 ? controllers[0] : null;

    /// <summary>
    /// 獲取第二個控制器（Player 2）
    /// </summary>
    public SwitchControllerHID Controller2 => controllers.Count > 1 ? controllers[1] : null;

    /// <summary>
    /// 當前連接的控制器數量
    /// </summary>
    public int ControllerCount => controllers.Count;

    void Awake()
    {

    }

    /// <summary>
    /// 保存控制器的校正數據
    /// </summary>
    public void SaveCalibration(int deviceId, Vector3 offset, Quaternion orientation, bool calibrated)
    {
        JoyConCalibrationData data = new JoyConCalibrationData
        {
            gyroOffset = offset,
            initialOrientation = orientation,
            isCalibrated = calibrated
        };

        if (calibrationDataMap.ContainsKey(deviceId))
        {
            calibrationDataMap[deviceId] = data;
        }
        else
        {
            calibrationDataMap.Add(deviceId, data);
        }
        
        if (showDebugInfo)
            Debug.Log($"[Manager] 已保存控制器 ID {deviceId} 的校正數據");
    }

    /// <summary>
    /// 獲取控制器的校正數據
    /// </summary>
    public bool TryGetCalibration(int deviceId, out JoyConCalibrationData data)
    {
        return calibrationDataMap.TryGetValue(deviceId, out data);
    }

    void OnEnable()
    {
        // 初始化時掃描所有已連接的控制器
        UpdateControllerList();
                // 單例模式初始化
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // 讓此物件在切換場景時不被銷毀
        }
        else if (instance != this)
        {
            // 如果已經有另一個實例存在（例如從場景 A 切換到場景 B，場景 B 也有這個 Manager）
            // 銷毀新的這個，保留舊的（帶有數據的）
            Destroy(gameObject);
            return;
        }

        // 訂閱設備變化事件
        if (autoUpdateControllers)
        {
            InputSystem.onDeviceChange += OnDeviceChange;
        }
    }

    void OnDestroy()
    {
        // 取消訂閱
        if (autoUpdateControllers)
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
        }
    }

    /// <summary>
    /// 處理設備連接/斷開事件
    /// </summary>
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is SwitchControllerHID controller)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    if (showDebugInfo)
                        Debug.Log($"Switch 控制器已連接: {device.name} (ID: {device.deviceId})");
                    
                    if (autoUpdateControllers)
                    {
                        UpdateControllerList();
                        
                        // 如果開啟自動初始化
                        if (autoInitializeNewControllers)
                        {
                            InitializeController(controller);
                        }
                    }
                    break;

                case InputDeviceChange.Removed:
                    if (showDebugInfo)
                        Debug.Log($"Switch 控制器已斷開: {device.name} (ID: {device.deviceId})");
                    
                    // 從已初始化列表中移除
                    initializedControllerIds.Remove(device.deviceId);
                    
                    if (autoUpdateControllers)
                    {
                        UpdateControllerList();
                    }
                    break;
            }
        }
    }
    
    /// <summary>
    /// 手動初始化所有尚未初始化的控制器
    /// </summary>
    public void InitializeNewControllers()
    {
        foreach (var controller in controllers)
        {
            if (controller != null && !initializedControllerIds.Contains(controller.deviceId))
            {
                StartCoroutine(InitializeControllerDelayed(controller));
            }
        }
    }

    /// <summary>
    /// 初始化單個控制器（只在第一次連接時執行）
    /// </summary>
    private void InitializeController(SwitchControllerHID controller)
    {
        if (controller == null)
            return;
        
        // 檢查是否已經初始化過
        if (initializedControllerIds.Contains(controller.deviceId))
        {
            if (showDebugInfo)
                Debug.Log($"控制器 {controller.deviceId} 已經初始化過，跳過");
            return;
        }

        // 使用協程延遲初始化，避免影響現有控制器
        StartCoroutine(InitializeControllerDelayed(controller));
    }
    
    /// <summary>
    /// 延遲初始化控制器，給它時間穩定
    /// </summary>
    private System.Collections.IEnumerator InitializeControllerDelayed(SwitchControllerHID controller)
    {
        // 等待一小段時間，讓控制器穩定下來
        yield return new WaitForSeconds(0.5f);
        
        if (controller == null)
            yield break;
        
        // 啟用 IMU
        controller.SetIMUEnabled(true);
        yield return new WaitForSeconds(0.1f); // 給 SetIMUEnabled 一點時間
        
        controller.ReadUserIMUCalibrationData();
        
        // 標記為已初始化
        initializedControllerIds.Add(controller.deviceId);
        
        if (showDebugInfo)
            Debug.Log($"控制器 {controller.deviceId} 初始化完成");
    }

    /// <summary>
    /// 手動更新控制器列表
    /// </summary>
    public void UpdateControllerList()
    {
        // 獲取所有 Switch 控制器
        var newControllers = InputSystem.devices
            .Where(d => d is SwitchControllerHID)
            .Cast<SwitchControllerHID>()
            .ToList();

        if (showDebugInfo)
        {
            Debug.Log($"找到 {newControllers.Count} 個 Switch 控制器:");
            for (int i = 0; i < newControllers.Count; i++)
            {
                var controller = newControllers[i];
                Debug.Log($"  [{i}] {controller.name} (ID: {controller.deviceId})");
            }
        }

        // 只初始化新加入的控制器
        foreach (var controller in newControllers)
        {
            InitializeController(controller);
        }
        
        // 更新列表
        controllers = newControllers;
    }

    /// <summary>
    /// 根據索引獲取控制器
    /// </summary>
    public SwitchControllerHID GetController(int index)
    {
        if (index >= 0 && index < controllers.Count)
            return controllers[index];
        return null;
    }

    /// <summary>
    /// 根據 deviceId 獲取控制器
    /// </summary>
    public SwitchControllerHID GetControllerById(int deviceId)
    {
        return controllers.FirstOrDefault(c => c.deviceId == deviceId);
    }

    /// <summary>
    /// 檢查特定索引的控制器是否存在
    /// </summary>
    public bool HasController(int index)
    {
        return index >= 0 && index < controllers.Count;
    }

    // 在編輯器中顯示 Gizmos 信息
    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 400, 200));
        GUILayout.Label($"連接的控制器數量: {controllers.Count}");

        for (int i = 0; i < controllers.Count; i++)
        {
            var controller = controllers[i];
            GUILayout.Label($"控制器 {i + 1}: {controller.name} (ID: {controller.deviceId})");
            
            // 顯示基本輸入狀態
            if (controller.buttonSouth.isPressed)
                GUILayout.Label($"  → 按下了 South 按鈕");
        }

        GUILayout.EndArea();
    }
}
