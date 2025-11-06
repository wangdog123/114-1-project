using UnityEngine;
using UnityEngine.InputSystem;

// 進階版本：使用Input System直接讀取Joy-Con陀螺儀
public class claws_advanced : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gyroSensitivity = 2f;
    [SerializeField] private float tiltThreshold = 0.1f;
    [SerializeField] private bool invertX = false; // 反轉X軸
    [SerializeField] private bool invertZ = false; // 反轉Z軸
    
    [Header("材質設定")]
    [SerializeField] private Material transparentMaterial;
    [SerializeField] private float transparency = 0.5f;
    
    [Header("移動範圍限制")]
    [SerializeField] private bool limitMovement = false;
    [SerializeField] private float maxX = 10f;
    [SerializeField] private float maxZ = 10f;
    
    private Renderer objectRenderer;
    private Vector3 currentAcceleration;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        
        if (transparentMaterial == null && objectRenderer != null)
        {
            SetupTransparentMaterial();
        }
        else if (transparentMaterial != null && objectRenderer != null)
        {
            objectRenderer.material = transparentMaterial;
            SetTransparency(transparency);
        }

        // 檢查是否有手把連接
        if (Gamepad.current != null)
        {
            Debug.Log($"手把已連接: {Gamepad.current.name}");
        }
        else
        {
            Debug.LogWarning("未檢測到手把/Joy-Con");
        }
        Input.gyro.enabled = true; // 啟用陀螺儀
        // InputSystem.EnableDevice(Gamepad.current); // 啟用手把設備
        InputSystem.EnableDevice(Accelerometer.current); // 啟用加速度計
        InputSystem.EnableDevice(AttitudeSensor.current);
    }

    void Update()
    {
        Debug.Log(Input.acceleration);
        currentAcceleration = Input.acceleration;
        
        // 計算移動向量
        float moveX = currentAcceleration.x;
        float moveZ = currentAcceleration.y; // 手把的Y對應世界的Z
        
        // 反轉選項
        if (invertX) moveX = -moveX;
        if (invertZ) moveZ = -moveZ;
        
        // 應用閾值
        if (Mathf.Abs(moveX) < tiltThreshold) moveX = 0;
        if (Mathf.Abs(moveZ) < tiltThreshold) moveZ = 0;
        
        // 創建移動向量
        Vector3 movement = new Vector3(moveX, 0, moveZ) * gyroSensitivity;
        
        // 移動物體
        transform.position += movement * moveSpeed * Time.deltaTime;
        
        // 限制移動範圍
        if (limitMovement)
        {
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, -maxX, maxX),
                transform.position.y,
                Mathf.Clamp(transform.position.z, -maxZ, maxZ)
            );
        }
    }

    // 設置半透明材質
    private void SetupTransparentMaterial()
    {
        if (objectRenderer != null && objectRenderer.material != null)
        {
            Material mat = objectRenderer.material;
            
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            
            SetTransparency(transparency);
            
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }
    }

    private void SetTransparency(float alpha)
    {
        if (objectRenderer != null && objectRenderer.material != null)
        {
            Color color = objectRenderer.material.color;
            color.a = Mathf.Clamp01(alpha);
            objectRenderer.material.color = color;
            
            if (objectRenderer.material.HasProperty("_BaseColor"))
            {
                Color baseColor = objectRenderer.material.GetColor("_BaseColor");
                baseColor.a = Mathf.Clamp01(alpha);
                objectRenderer.material.SetColor("_BaseColor", baseColor);
            }
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying && objectRenderer != null)
        {
            SetTransparency(transparency);
        }
    }
    
    // 調試信息顯示
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 14;
        style.normal.textColor = Color.white;
        
        GUI.Label(new Rect(10, 10, 400, 25), $"加速度 X: {currentAcceleration.x:F3}  Y: {currentAcceleration.y:F3}  Z: {currentAcceleration.z:F3}", style);
        GUI.Label(new Rect(10, 35, 400, 25), $"位置: X={transform.position.x:F2}, Y={transform.position.y:F2}, Z={transform.position.z:F2}", style);
        GUI.Label(new Rect(10, 60, 400, 25), $"陀螺儀靈敏度: {gyroSensitivity}  |  閾值: {tiltThreshold}", style);
        
        if (Gamepad.current != null)
        {
            GUI.Label(new Rect(10, 85, 400, 25), $"手把: {Gamepad.current.name}", style);
        }
        else
        {
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(10, 85, 400, 25), "警告: 未檢測到手把/Joy-Con", style);
        }
    }
}
