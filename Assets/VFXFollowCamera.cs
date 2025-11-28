using UnityEngine;

/// <summary>
/// 讓物件跟隨攝影機移動（適用於 World Space Canvas 或 VFX 管理器）
/// </summary>
public class VFXFollowCamera : MonoBehaviour
{
    [Header("跟隨設置")]
    public Transform targetCamera; // 要跟隨的攝影機
    public Vector3 offset = new Vector3(0, 0, 10); // 相對攝影機的偏移量
    public bool followRotation = false; // 是否跟隨旋轉
    public bool useInitialOffset = true; // 是否使用初始位置作為偏移量

    void Start()
    {
        // 如果未設置攝影機，自動抓取主攝影機
        if (targetCamera == null)
        {
            if (Camera.main != null)
            {
                targetCamera = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning("[VFXFollowCamera] 未找到 Main Camera，請手動指定 Target Camera");
            }
        }

        // 如果選擇使用初始偏移，則計算當前物件與攝影機的相對位置
        if (useInitialOffset && targetCamera != null)
        {
            if (followRotation)
            {
                // 如果跟隨旋轉，偏移量需要考慮旋轉（Local Space）
                offset = targetCamera.InverseTransformPoint(transform.position);
            }
            else
            {
                // 如果不跟隨旋轉，只計算世界座標差值
                offset = transform.position - targetCamera.position;
            }
        }
    }

    void LateUpdate()
    {
        if (targetCamera != null)
        {
            if (followRotation)
            {
                // 跟隨位置和旋轉
                transform.position = targetCamera.TransformPoint(offset);
                transform.rotation = targetCamera.rotation;
            }
            else
            {
                // 只跟隨位置，保持自身旋轉
                transform.position = targetCamera.position + offset;
            }
        }
    }
}
