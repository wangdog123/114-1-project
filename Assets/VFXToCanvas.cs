using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

/// <summary>
/// 將 VFX 渲染到 Canvas 上
/// 方法 1: 使用 Render Texture
/// </summary>
public class VFXToCanvas : MonoBehaviour
{
    [Header("VFX 設定")]
    [Tooltip("要渲染的 VFX GameObject（世界空間中）")]
    public GameObject vfxEffectObject;
    
    [Tooltip("專門用來渲染 VFX 的相機")]
    public Camera vfxCamera;
    
    [Header("Canvas 設定")]
    [Tooltip("Canvas 上的 RawImage 組件")]
    public RawImage canvasImage;
    
    [Header("Render Texture 設定")]
    [Tooltip("Render Texture 的解析度")]
    public Vector2Int resolution = new Vector2Int(512, 512);
    
    [Tooltip("是否使用透明背景")]
    public bool transparentBackground = true;
    
    private RenderTexture renderTexture;
    private VisualEffect vfxEffect; // VFX 組件
    
    void Start()
    {
        // 獲取 VFX 組件
        if (vfxEffectObject != null)
        {
            vfxEffect = vfxEffectObject.GetComponent<VisualEffect>();
            if (vfxEffect == null)
            {
                Debug.LogWarning($"警告：{vfxEffectObject.name} 上沒有 VisualEffect 組件！");
            }
        }
        
        SetupRenderTexture();
    }
    
    void SetupRenderTexture()
    {
        // 創建 Render Texture
        renderTexture = new RenderTexture(resolution.x, resolution.y, 24);
        renderTexture.format = RenderTextureFormat.ARGB32;
        renderTexture.Create();
        
        // 設置相機渲染到 Render Texture
        if (vfxCamera != null)
        {
            vfxCamera.targetTexture = renderTexture;
            
            // 如果要透明背景
            if (transparentBackground)
            {
                vfxCamera.clearFlags = CameraClearFlags.SolidColor;
                vfxCamera.backgroundColor = new Color(0, 0, 0, 0); // 完全透明
            }
        }
        else
        {
            Debug.LogError("VFX Camera 未設置！");
        }
        
        // 將 Render Texture 顯示在 Canvas 上
        if (canvasImage != null)
        {
            canvasImage.texture = renderTexture;
        }
        else
        {
            Debug.LogError("Canvas RawImage 未設置！");
        }
    }
    
    void OnDestroy()
    {
        // 清理 Render Texture
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
    
    // 可選：動態改變 VFX 位置
    public void SetVFXPosition(Vector3 position)
    {
        if (vfxEffectObject != null)
        {
            vfxEffectObject.transform.position = position;
        }
    }
    
    // 可選：播放 VFX
    public void PlayVFX()
    {
        if (vfxEffect != null)
        {
            vfxEffect.Play();
        }
    }
    
    // 可選：停止 VFX
    public void StopVFX()
    {
        if (vfxEffect != null)
        {
            vfxEffect.Stop();
        }
    }
}
