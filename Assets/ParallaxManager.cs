using UnityEngine;
using System.Collections.Generic; // ✅ 加上這行

[System.Serializable]
public class ParallaxLayer
{
    public Transform layerTransform;
    public float parallaxFactor = 0.5f;
    [HideInInspector] public float initialZ; // 記錄初始 Z
}

public class ParallaxManager : MonoBehaviour
{
    public List<ParallaxLayer> layers = new List<ParallaxLayer>();
    public Camera mainCamera;
    public float scaleMargin = 1.05f;
    public float imageAspect = 16f / 9f;

    private float cameraInitialZ;

    void Start()
    {
        if (mainCamera != null)
            cameraInitialZ = mainCamera.transform.position.z;

        foreach (var p in layers)
        {
            if (p.layerTransform != null)
                p.initialZ = p.layerTransform.position.z;
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        float orthoHeight = mainCamera.orthographicSize * 2f;
        float orthoWidth = orthoHeight * mainCamera.aspect;

        float cameraDeltaZ = mainCamera.transform.position.z - cameraInitialZ;

        foreach (var p in layers)
        {
            if (p.layerTransform == null) continue;

            // 縮放
            float scaleX = orthoWidth * scaleMargin;
            float scaleY = orthoHeight * scaleMargin;
            if (scaleX / scaleY > imageAspect)
                scaleX = scaleY * imageAspect;
            else
                scaleY = scaleX / imageAspect;
            p.layerTransform.localScale = new Vector3(scaleX, scaleY, 1f);

            // Parallax 移動：根據相機位移量
            float zOffset = cameraDeltaZ * p.parallaxFactor;
            p.layerTransform.position = new Vector3(
                p.layerTransform.position.x,
                p.layerTransform.position.y,
                p.initialZ + zOffset
            );
        }
    }
}