using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Timeline;
using UnityEngine.Playables;   // 如果你用 TextMeshPro 就需要這行

public class TimeUIController : MonoBehaviour
{
    [Header("倒數時間設定")]
    public float totalTime = 30f;   // 倒數的總時間
    private float remainingTime;    // 剩餘時間
    public ParallaxManager parallaxManager; // 可選：從 ParallaxManager 拿取總時間
    
    // 公開剩餘時間供其他腳本查詢
    public float RemainingTime => remainingTime;

    [Header("UI 元件")]
    public Slider timerSlider;      // 倒數用的 Slider
    public Slider distanceSlider;   // 顯示鏡頭從起點到最終Z的進度 (0~1)
    public TMP_Text timerText;      // 倒數用的文字（可改成 Text）
    public PlayableDirector sliderTimeline; // 可選：用於控制時間軸的 Slider

    void OnEnable()
    {
        initialize();
    }

    void Update()
    {
        if (parallaxManager.started && remainingTime > 0)
        {
            sliderTimeline.Play();
            float currentTotal = (parallaxManager != null) ? parallaxManager.cameraMoveDuration : totalTime;

            // 遞減時間
            remainingTime -= Time.deltaTime;

            // 更新 Slider (用剩餘時間除以目前的總時間)
            if (timerSlider != null)
                timerSlider.value = Mathf.Clamp01(remainingTime / currentTotal);

            // 更新文字
            UpdateTimerText();
        }
        // 更新距離滑桿（如果有綁定 ParallaxManager）
        if (parallaxManager != null && distanceSlider != null)
        {
            float startZ = parallaxManager.cameraMoveStartZ;
            float finalZ = parallaxManager.cameraMoveFinalZ;
            float camZ = (parallaxManager.cam != null) ? parallaxManager.cam.transform.position.z : 0f;

            float t = 0f;
            if (!Mathf.Approximately(finalZ, startZ))
                t = Mathf.InverseLerp(startZ, finalZ, camZ);

            distanceSlider.value = Mathf.Clamp01(t);
        }
        if (remainingTime <= 0)
        {
            // 時間到，確保 Slider 為 0 並停止時間軸
            if (timerSlider != null)
                timerSlider.value = 0f;
            if (sliderTimeline != null)
                sliderTimeline.Stop();
        }
    }

    public void initialize()
    {
        // 取得目前使用的總時間（若有指定 ParallaxManager 則使用其 cameraMoveDuration）
        float currentTotal = (parallaxManager != null) ? parallaxManager.cameraMoveDuration : totalTime;
        remainingTime = currentTotal;

        // 初始化 Slider
        if (timerSlider != null)
        {
            timerSlider.minValue = 0f;
            timerSlider.maxValue = 1f;
            timerSlider.value = 1f; // 一開始是滿的
        }
        if (distanceSlider != null)
        {
            distanceSlider.minValue = 0f;
            distanceSlider.maxValue = 1f;
            distanceSlider.value = 0f;
        }
        UpdateTimerText();

    }
    // 更新倒數文字 (四捨五入取整數)
    void UpdateTimerText()
    {
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(remainingTime).ToString();
        }
    }
}
