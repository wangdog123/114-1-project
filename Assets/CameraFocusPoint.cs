using UnityEngine;

/// <summary>
/// 鏡頭焦點標記
/// 用於標記開場動畫中鏡頭應該放大到的位置
/// 在 Scene 視圖中會顯示一個小圖標，方便調整位置
/// </summary>
public class CameraFocusPoint : MonoBehaviour
{
    [Header("預覽設置")]
    public Color gizmoColor = Color.yellow;
    public float gizmoSize = 0.5f;
    
    [Header("焦點設置")]
    [Tooltip("鏡頭看向的方向（可選，如果未設置則使用 Transform.forward）")]
    public Transform lookAtTarget;
    
    void OnDrawGizmos()
    {
        // 繪製一個球體標記焦點位置
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoSize);
        
        // 繪製方向箭頭
        Vector3 direction = lookAtTarget != null 
            ? (lookAtTarget.position - transform.position).normalized 
            : transform.forward;
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, direction * 2f);
        
        // 繪製視野框架
        Gizmos.color = gizmoColor;
        DrawViewFrustum();
    }
    
    void DrawViewFrustum()
    {
        // 簡單繪製一個視野錐體
        float distance = 5f;
        float fov = 30f; // 與放大後的 FOV 匹配
        
        float halfHeight = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * distance;
        float halfWidth = halfHeight * Camera.main.aspect;
        
        Vector3 forward = transform.forward * distance;
        Vector3 right = transform.right * halfWidth;
        Vector3 up = transform.up * halfHeight;
        
        Vector3 center = transform.position + forward;
        
        Vector3 topLeft = center + up - right;
        Vector3 topRight = center + up + right;
        Vector3 bottomLeft = center - up - right;
        Vector3 bottomRight = center - up + right;
        
        // 繪製框架
        Gizmos.DrawLine(transform.position, topLeft);
        Gizmos.DrawLine(transform.position, topRight);
        Gizmos.DrawLine(transform.position, bottomLeft);
        Gizmos.DrawLine(transform.position, bottomRight);
        
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
    
    void OnDrawGizmosSelected()
    {
        // 被選中時顯示更多資訊
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, gizmoSize * 1.5f);
    }
}
