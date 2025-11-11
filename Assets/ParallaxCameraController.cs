using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class OrthographicCameraController : MonoBehaviour
{
    [Header("相機移動設定")]
    public float moveSpeed = 2f;               // 相機移動速度
    public Transform endTarget;                // 停止目標

    [Header("背景自動縮放")]
    public List<Transform> backgroundLayers;
    public float scaleMargin = 1.05f;
    public float imageAspect = 16f / 9f;

    [Header("立體效果設定")]
    public float depthScaleFactor = 0.05f;
    public float minDepthScale = 0.5f;
    public float maxDepthScale = 3f;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
            cam.orthographic = true;

        UpdateBackgroundScales();
    }

    void Update()
    {
        // Play 模式才自動移動相機
        if (Application.isPlaying)
        {
            if (endTarget == null || transform.position.z < endTarget.position.z)
            {
                transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
            }
        }

        // 無論是否 Play 都更新縮放
        UpdateBackgroundScales();
    }

    void OnValidate()
    {
        UpdateBackgroundScales();
    }

    void UpdateBackgroundScales()
    {
        if (backgroundLayers == null || backgroundLayers.Count == 0) return;
        if (cam == null) return;

        float orthoHeight = cam.orthographicSize * 2f;
        float orthoWidth = orthoHeight * cam.aspect;

        foreach (Transform layer in backgroundLayers)
        {
            if (layer == null) continue;

            float scaleX = orthoWidth * scaleMargin;
            float scaleY = orthoHeight * scaleMargin;

            if (scaleX / scaleY > imageAspect)
                scaleX = scaleY * imageAspect;
            else
                scaleY = scaleX / imageAspect;

            // Z 軸距離放大，每 15 單位放大 1 倍
            // 計算 Z 軸距離

float distanceZ = cam.transform.position.z - layer.position.z;

// 線性平滑縮放
float depthScale = 1 + distanceZ * depthScaleFactor;

// 限制最小與最大值
depthScale = Mathf.Clamp(depthScale, minDepthScale, maxDepthScale);

// 套用縮放
layer.localScale = new Vector3(scaleX * depthScale, scaleY * depthScale, 1f);
        }
    }
}
