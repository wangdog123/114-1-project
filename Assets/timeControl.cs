using UnityEngine;
using UnityEngine.UI;
using TMPro;   // 如果你用 TextMeshPro 就需要這行

public class TimeUIController : MonoBehaviour
{
    [Header("倒數時間設定")]
    public float totalTime = 30f;   // 倒數的總時間
    private float remainingTime;    // 剩餘時間

    [Header("UI 元件")]
    public Slider timerSlider;      // 倒數用的 Slider
    public TMP_Text timerText;      // 倒數用的文字（可改成 Text）

    void Start()
    {
        // 初始化時間
        remainingTime = totalTime;

        // 初始化 Slider
        if (timerSlider != null)
        {
            timerSlider.minValue = 0f;
            timerSlider.maxValue = 1f;
            timerSlider.value = 1f; // 一開始是滿的
        }

        UpdateTimerText();
    }

    void Update()
    {
        if (remainingTime > 0)
        {
            // 遞減時間
            remainingTime -= Time.deltaTime;

            // 更新 Slider (用剩餘時間除以總時間)
            if (timerSlider != null)
                timerSlider.value = Mathf.Clamp01(remainingTime / totalTime);

            // 更新文字
            UpdateTimerText();
        }
        else
        {
            remainingTime = 0;
        }
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
