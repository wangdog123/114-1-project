using UnityEngine;
using UnityEngine.InputSystem;

public class claws : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5f; // 移動速度
    [SerializeField] private float gyroSensitivity = 2f; // 陀螺儀靈敏度
    [SerializeField] private float tiltThreshold = 0.1f; // 傾斜閾值（避免微小晃動）
    
    [Header("材質設定")]
    [SerializeField] private Material transparentMaterial; // 半透明材質
    [SerializeField] private float transparency = 0.5f; // 透明度 (0-1)
    
    private Vector3 gyroInput; // 陀螺儀輸入
    private Gamepad gamepad; // 手把參考
    private Renderer objectRenderer;
    private bool useGyro = true; // 是否使用陀螺儀

    void Start()
    {
        // 獲取Renderer組件
        objectRenderer = GetComponent<Renderer>();
        
        // 如果沒有指定材質，創建一個半透明材質
        if (transparentMaterial == null && objectRenderer != null)
        {
            SetupTransparentMaterial();
        }
        else if (transparentMaterial != null && objectRenderer != null)
        {
            objectRenderer.material = transparentMaterial;
            SetTransparency(transparency);
        }
        
        // 獲取當前連接的手把
        if (Gamepad.current != null)
        {
            gamepad = Gamepad.current;
            Debug.Log("Joy-Con/手把已連接");
        }
        else
        {
            Debug.LogWarning("未檢測到Joy-Con/手把");
        }
    }

    void Update()
    {
        if (useGyro && gamepad != null)
        {
            // 讀取陀螺儀數據（重力感應）
            // 使用設備的加速度計來檢測傾斜
            Vector3 acceleration = Input.acceleration;
            
            // 將加速度轉換為移動向量
            // X軸：左右傾斜
            // Z軸：前後傾斜
            float moveX = acceleration.x;
            float moveZ = acceleration.y; // 手把的Y軸對應世界的Z軸
            
            // 應用閾值，避免微小晃動造成移動
            if (Mathf.Abs(moveX) < tiltThreshold) moveX = 0;
            if (Mathf.Abs(moveZ) < tiltThreshold) moveZ = 0;
            
            // 創建移動向量
            Vector3 movement = new Vector3(moveX, 0, moveZ) * gyroSensitivity;
            
            // 移動物體
            transform.position += movement * moveSpeed * Time.deltaTime;
        }
    }

    // 設置半透明材質
    private void SetupTransparentMaterial()
    {
        if (objectRenderer != null && objectRenderer.material != null)
        {
            Material mat = objectRenderer.material;
            
            // 設置渲染模式為透明
            mat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
            mat.SetFloat("_Blend", 0); // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
            
            // 啟用混合模式
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            
            // 設置透明度
            SetTransparency(transparency);
            
            // 啟用關鍵字
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        }
    }

    // 設置透明度
    private void SetTransparency(float alpha)
    {
        if (objectRenderer != null && objectRenderer.material != null)
        {
            Color color = objectRenderer.material.color;
            color.a = Mathf.Clamp01(alpha);
            objectRenderer.material.color = color;
            
            // 如果使用HDRP，還需要設置BaseColor的alpha
            if (objectRenderer.material.HasProperty("_BaseColor"))
            {
                Color baseColor = objectRenderer.material.GetColor("_BaseColor");
                baseColor.a = Mathf.Clamp01(alpha);
                objectRenderer.material.SetColor("_BaseColor", baseColor);
            }
        }
    }

    // 在編輯器中即時調整透明度
    private void OnValidate()
    {
        if (Application.isPlaying && objectRenderer != null)
        {
            SetTransparency(transparency);
        }
    }
    
    // 顯示調試信息
    private void OnGUI()
    {
        if (useGyro)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"加速度: {Input.acceleration}");
            GUI.Label(new Rect(10, 30, 300, 20), $"位置: {transform.position}");
            GUI.Label(new Rect(10, 50, 300, 20), $"陀螺儀靈敏度: {gyroSensitivity}");
        }
    }
}
