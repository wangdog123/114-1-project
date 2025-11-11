using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 🎥 Parallax 背景滾動效果（支援 Z 軸相機移動）
/// 掛在每一層背景（Sprite 或 Quad）上
/// 讓它根據相機移動自動產生景深滾動
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("🎞 Parallax 設定")]
    [Tooltip("控制這一層滾動速度。越小代表越遠，移動越慢。建議 0.1~1")]
    public float parallaxFactor = 0.5f;

    [Tooltip("是否在 X 軸上產生視差（若相機沿 X 軸移動）")]
    public bool affectX = false;

    [Tooltip("是否在 Z 軸上產生視差（若相機沿 Z 軸移動）")]
    public bool affectZ = true;

    [Tooltip("是否在 Y 軸上產生視差（可選）")]
    public bool affectY = false;

    private Transform cam;           // 主相機位置
    private Vector3 lastCamPos;      // 上一幀相機位置

    void Start()
    {
        cam = Camera.main.transform;
        lastCamPos = cam.position;
    }

    void LateUpdate()
    {
        // 計算相機移動差距
        Vector3 delta = cam.position - lastCamPos;

        // 根據設定移動背景
        Vector3 move = new Vector3(
            affectX ? delta.x * parallaxFactor : 0,
            affectY ? delta.y * parallaxFactor : 0,
            affectZ ? delta.z * parallaxFactor : 0);

        transform.position += move;

        // 更新上一幀相機位置
        lastCamPos = cam.position;
    }
}
