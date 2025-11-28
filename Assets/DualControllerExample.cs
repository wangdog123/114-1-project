using UnityEngine;
using UnityEngine.InputSystem.Switch;

/// <summary>
/// 示範如何同時使用兩個 Switch 控制器
/// </summary>
public class DualControllerExample : MonoBehaviour
{
    [Header("控制器管理器")]
    public MultiSwitchControllerManager controllerManager;

    [Header("玩家 1 設定")]
    public GameObject player1Object;
    public Color player1Color = Color.blue;

    [Header("玩家 2 設定")]
    public GameObject player2Object;
    public Color player2Color = Color.red;

    [Header("移動設定")]
    public float moveSpeed = 5f;
    public float gyroSensitivity = 0.1f;

    private Vector3 player1Position;
    private Vector3 player2Position;

    void Start()
    {
        // 如果沒有指定管理器，嘗試自動找到
        if (controllerManager == null)
        {
            controllerManager = FindObjectOfType<MultiSwitchControllerManager>();
        }

        if (controllerManager == null)
        {
            Debug.LogError("找不到 MultiSwitchControllerManager！請確保場景中有這個組件。");
            return;
        }

        // 初始化玩家位置
        if (player1Object != null)
            player1Position = player1Object.transform.position;
        if (player2Object != null)
            player2Position = player2Object.transform.position;
    }

    void Update()
    {
        if (controllerManager == null) return;

        // 處理玩家 1（第一個控制器）
        if (controllerManager.HasController(0))
        {
            HandlePlayer1(controllerManager.Controller1);
        }

        // 處理玩家 2（第二個控制器）
        if (controllerManager.HasController(1))
        {
            HandlePlayer2(controllerManager.Controller2);
        }
    }

    void HandlePlayer1(SwitchControllerHID controller)
    {
        if (controller == null || player1Object == null) return;

        // 使用陀螺儀控制移動
        Vector3 gyro = controller.angularVelocity.ReadValue();
        
        // 將陀螺儀數據轉換為移動向量
        Vector3 movement = new Vector3(gyro.y, 0, -gyro.x) * gyroSensitivity * Time.deltaTime;
        player1Position += movement;
        player1Object.transform.position = player1Position;

        // 檢測按鈕輸入
        if (controller.buttonSouth.wasPressedThisFrame)
        {
            Debug.Log("玩家 1 按下了 A 按鈕");
        }

        if (controller.buttonEast.wasPressedThisFrame)
        {
            Debug.Log("玩家 1 按下了 B 按鈕");
        }

        // 使用搖桿控制旋轉
        Vector2 leftStick = controller.leftStick.ReadValue();
        if (leftStick.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(leftStick.x, leftStick.y) * Mathf.Rad2Deg;
            player1Object.transform.rotation = Quaternion.Euler(0, angle, 0);
        }
    }

    void HandlePlayer2(SwitchControllerHID controller)
    {
        if (controller == null || player2Object == null) return;

        // 使用陀螺儀控制移動
        Vector3 gyro = controller.angularVelocity.ReadValue();
        
        // 將陀螺儀數據轉換為移動向量
        Vector3 movement = new Vector3(gyro.y, 0, -gyro.x) * gyroSensitivity * Time.deltaTime;
        player2Position += movement;
        player2Object.transform.position = player2Position;

        // 檢測按鈕輸入
        if (controller.buttonSouth.wasPressedThisFrame)
        {
            Debug.Log("玩家 2 按下了 A 按鈕");
        }

        if (controller.buttonEast.wasPressedThisFrame)
        {
            Debug.Log("玩家 2 按下了 B 按鈕");
        }

        // 使用搖桿控制旋轉
        Vector2 leftStick = controller.leftStick.ReadValue();
        if (leftStick.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(leftStick.x, leftStick.y) * Mathf.Rad2Deg;
            player2Object.transform.rotation = Quaternion.Euler(0, angle, 0);
        }
    }

    void OnGUI()
    {
        // 顯示調試信息
        GUILayout.BeginArea(new Rect(10, 250, 400, 300));

        if (controllerManager != null)
        {
            GUILayout.Label("=== 玩家 1 ===");
            if (controllerManager.Controller1 != null)
            {
                var gyro1 = controllerManager.Controller1.angularVelocity.ReadValue();
                GUILayout.Label($"陀螺儀: {gyro1:F2}");
                GUILayout.Label($"加速度: {controllerManager.Controller1.acceleration.ReadValue():F2}");
            }
            else
            {
                GUILayout.Label("未連接");
            }

            GUILayout.Space(10);

            GUILayout.Label("=== 玩家 2 ===");
            if (controllerManager.Controller2 != null)
            {
                var gyro2 = controllerManager.Controller2.angularVelocity.ReadValue();
                GUILayout.Label($"陀螺儀: {gyro2:F2}");
                GUILayout.Label($"加速度: {controllerManager.Controller2.acceleration.ReadValue():F2}");
            }
            else
            {
                GUILayout.Label("未連接");
            }
        }

        GUILayout.EndArea();
    }
}
