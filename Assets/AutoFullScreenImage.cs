using UnityEngine;

[ExecuteAlways]
public class AutoFullScreenImage : MonoBehaviour
{
    public Camera targetCamera;
    public float aspectRatio = 16f / 9f; // 你的圖片比例
    public float margin = 1.05f;         // 稍微放大避免邊緣露出

    void Update()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        float distance = Vector3.Distance(transform.position, targetCamera.transform.position);
        float height = 2f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * aspectRatio;

        // 根據距離動態縮放圖片，確保畫面滿版
        transform.localScale = new Vector3(width * margin, height * margin, 1f);
    }
}
