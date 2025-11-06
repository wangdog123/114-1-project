using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlashTarget : MonoBehaviour
{
    [Header("目標設置")]
    public ScratchRhythmGame.SlashDirection direction; // 要求的劃動方向
    public int stepIndex; // 在序列中的索引
    public float spawnTime; // 目標生成時間（提示音效播放時間）
    public float judgmentBeatTime; // 判定節拍時間（玩家要對準的時間，spawnTime + beatInterval）
    public bool hasPlayedJudgmentBeat = false; // 是否已播放判定音效
    
    [Header("飛行設置")]
    public float flyingStartTime; // 開始飛行的時間
    public float flyingDuration = 2f; // 飛行總時長（秒）
    public float idealHitTime; // 理想擊中時間（flyingStartTime + flyingDuration * 0.75）
    public bool isHit = false; // 是否已被擊中
    public bool isMissed = false; // 是否錯過（飛到死亡線）
    
    [Header("節奏設置")]
    public float customInterval = 0f; // 自訂間隔（與上一個物件的間隔，0=使用預設beatInterval）
    
    [Header("UI 組件")]
    public Image backgroundImage; // 背景圖片
    public TextMeshProUGUI directionText; // 方向文字
    
    // 內部狀態
    private bool isActive = true; // 是否還在等待完成
    private RectTransform rectTransform;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
    }
    
    // 初始化目標（由外部呼叫，設置完 direction 後）
    public virtual void Initialize()
    {
        RotateToDirection();
    }
    
    // 根據劃動方向旋轉目標
    void RotateToDirection()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
            
        float angle = 0f;
        
        switch (direction)
        {
            case ScratchRhythmGame.SlashDirection.Right:
                angle = 0f; // 水平向右
                break;
            case ScratchRhythmGame.SlashDirection.Left:
                angle = 180f; // 水平向左
                break;
            case ScratchRhythmGame.SlashDirection.Up:
                angle = 90f; // 垂直向上
                break;
            case ScratchRhythmGame.SlashDirection.Down:
                angle = -90f; // 垂直向下
                break;
        }
        
        rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
        Debug.Log($"[目標 #{stepIndex}] 設置旋轉角度: {angle}° (方向: {direction})");
    }
    
    // 設置方向文字
    public void SetDirectionText(string text)
    {
        if (directionText != null)
            directionText.text = text;
    }
    
    // 獲取目標位置
    public virtual Vector3 GetPosition()
    {
        return rectTransform.anchoredPosition;
    }
    
    // 標記為已完成
    public virtual void MarkAsCompleted()
    {
        isActive = false;
        OnHit(true);
    }
    
    // 標記為失敗
    public virtual void MarkAsFailed()
    {
        isActive = false;
        OnHit(false);
    }
    
    // 檢查是否還活躍
    public virtual bool IsActive()
    {
        return isActive;
    }
    
    // 擊中反饋（成功/失敗）
    public void OnHit(bool correct)
    {
        isActive = false;
        
        if (correct)
        {
            // 成功：綠色
            if (backgroundImage != null)
                backgroundImage.color = new Color(0.3f, 1f, 0.3f, 0.8f);
        }
        else
        {
            // 失敗：紅色
            if (backgroundImage != null)
                backgroundImage.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        }
        
        // 開始淡出消失
        StartCoroutine(FadeOut());
    }
    
    // 淡出消失
    IEnumerator FadeOut()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        
        Color startColor = backgroundImage.color;
        Color startTextColor = directionText.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / duration);
            
            if (backgroundImage != null)
                backgroundImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            if (directionText != null)
                directionText.color = new Color(startTextColor.r, startTextColor.g, startTextColor.b, alpha);
                
            yield return null;
        }
        
        Destroy(gameObject);
    }
}
