using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// 將 VFX 直接放在 World Space Canvas 上
/// 方法 2: 使用 World Space 模式
/// </summary>
public class VFXWorldSpaceCanvas : MonoBehaviour
{
    [Header("VFX 設定")]
    [Tooltip("VFX Prefab 或包含 VFX 的 GameObject")]
    public GameObject vfxPrefab; // VFX Prefab (改為 GameObject 類型)
    
    [Header("Canvas 設定")]
    [Tooltip("World Space Canvas 的 Transform")]
    public Transform canvasTransform;
    
    [Tooltip("VFX 在 Canvas 上的位置（本地座標）")]
    public Vector3 localPosition = Vector3.zero;
    
    [Tooltip("VFX 的縮放")]
    public float scale = 1f;
    
    private GameObject vfxInstanceObject; // VFX GameObject 實例
    private VisualEffect vfxInstance; // VFX 組件
    
    void Start()
    {
        SpawnVFX();
    }
    
    void SpawnVFX()
    {
        if (vfxPrefab == null)
        {
            Debug.LogError("VFX Prefab 未設置！");
            return;
        }
        
        if (canvasTransform == null)
        {
            Debug.LogError("Canvas Transform 未設置！");
            return;
        }
        
        // 在 Canvas 上實例化 VFX
        vfxInstanceObject = Instantiate(vfxPrefab, canvasTransform);
        vfxInstanceObject.transform.localPosition = localPosition;
        vfxInstanceObject.transform.localScale = Vector3.one * scale;
        
        // 獲取 VFX 組件
        vfxInstance = vfxInstanceObject.GetComponent<VisualEffect>();
        
        if (vfxInstance == null)
        {
            Debug.LogWarning($"警告：{vfxPrefab.name} 上沒有 VisualEffect 組件！");
        }
        
        Debug.Log($"VFX 已生成在 Canvas 上，位置：{localPosition}");
    }
    
    // 播放 VFX
    public void PlayVFX()
    {
        if (vfxInstance != null)
        {
            vfxInstance.Play();
        }
    }
    
    // 停止 VFX
    public void StopVFX()
    {
        if (vfxInstance != null)
        {
            vfxInstance.Stop();
        }
    }
    
    // 銷毀 VFX
    public void DestroyVFX()
    {
        if (vfxInstanceObject != null)
        {
            Destroy(vfxInstanceObject);
        }
    }
}
