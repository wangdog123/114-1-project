using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ★ 基類定義
public abstract class SlashTarget : MonoBehaviour
{
    public ScratchRhythmGame.SlashDirection direction;
    public int stepIndex;
    public bool isHit = false;
    public bool isMissed = false;
    public float spawnTime;
    public float flyingStartTime; // 飛行開始時間
    public float flyingDuration; // 飛行持續時間
    public float customInterval; // 自定義間隔
    public bool hasPlayedJudgmentBeat = false; // 是否已播放判定音效
    public AudioClip hitSound; // ★ 該目標特定的擊中音效
    public bool isTutorialTarget = false; // ★ 是否為教學目標（忽略時間窗口限制）
    
    public abstract void Initialize();
    public abstract void MarkAsCompleted();
    public abstract void MarkAsFailed();
    public abstract bool IsActive();
    public abstract Vector3 GetPosition();
}

// ★ 3D 版本
public class SlashTarget3D : SlashTarget
{
    [Header("3D 飛行設置")]
    public Transform spawnPoint; // 發射點（遠方）
    public Transform targetPoint; // 目標點（鏡頭前方）
    public Vector3 targetOffset; // 目標點的偏移量
    public float arcHeight = 5f; // 拋物線高度
    
    [Header("3D 視覺組件")]
    public MeshRenderer meshRenderer; // 3D 模型渲染器（可選，用於擊中變色）
    public Transform slashSprite; // 劃痕 Sprite 的 Transform
    public SpriteRenderer targetSpriteRenderer; // 用於顯示方向圖片的 SpriteRenderer
    
    // 內部狀態
    private bool isFlying = false;
    private Vector3 startPosition;
    private float flyingProgress = 0f; // 飛行進度 (0-1)
    public bool isPaused = false; // ★ 是否暫停飛行（但不暫停時間）
    
    void Start()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();
            
        // 自動尋找 SpriteRenderer
        if (targetSpriteRenderer == null && slashSprite != null)
        {
            targetSpriteRenderer = slashSprite.GetComponent<SpriteRenderer>();
        }
    }
    
    void Update()
    {
        // 檢查是否應該開始飛行
        if (!isFlying && flyingStartTime > 0 && Time.time >= flyingStartTime)
        {
            StartFlying();
        }
        
        // 更新飛行位置（★ 但如果 isPaused 則跳過）
        if (isFlying && !isHit && !isMissed && !isPaused)
        {
            UpdateFlying();
        }
    }
    
    // 開始飛行
    void StartFlying()
    {
        isFlying = true;
        startPosition = transform.position;
        
        Debug.Log($"[飛行] 物件 #{stepIndex} 開始飛行，從 {startPosition} 飛向目標點");
    }
    
    // 更新飛行位置（追蹤目標點）
    void UpdateFlying()
    {
        if (targetPoint == null)
        {
            Debug.LogWarning($"[飛行] 物件 #{stepIndex} 沒有設置目標點！");
            return;
        }
        
        // 計算飛行進度 (0-1)
        float elapsedTime = Time.time - flyingStartTime;
        flyingProgress = Mathf.Clamp01(elapsedTime / flyingDuration);
        
        // ★ 改為線性移動，確保視覺與時間完全同步
        float linearProgress = flyingProgress;
        
        // ★ 前半段（0-0.7）：保持分區效果，各自飛向偏移的目標點
        // ★ 後半段（0.7-1.0）：收斂到中心目標點
        Vector3 endPosition;
        float convergenceThreshold = 0.85f;
        
        if (linearProgress < convergenceThreshold)
        {
            // 前半段：保持分區
            endPosition = targetPoint.position + targetOffset;
        }
        else
        {
            // 後半段：從偏移點線性收斂到中心點
            float convergenceProgress = (linearProgress - convergenceThreshold) / (1f - convergenceThreshold);
            Vector3 offsetPosition = targetPoint.position + targetOffset;
            Vector3 centerPosition = targetPoint.position;
            endPosition = Vector3.Lerp(offsetPosition, centerPosition, convergenceProgress);
        }
        
        Vector3 currentPos = Vector3.Lerp(startPosition, endPosition, linearProgress);
        
        // 使用 sin 曲線創造拋物線效果（在飛行中間達到最高點）
        float heightOffset = Mathf.Sin(flyingProgress * Mathf.PI) * arcHeight;
        currentPos.y += heightOffset;
        
        transform.position = currentPos;
    }

    // 設定該目標的圖片（根據方向和類型）
    public void SetDirectionSprite(Sprite sprite)
    {
        if (sprite == null) return;
        
        if (targetSpriteRenderer != null)
        {
            targetSpriteRenderer.sprite = sprite;
        }
        else if (slashSprite != null)
        {
            var sr = slashSprite.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprite;
            }
        }
    }
    
    // 初始化目標
    public override void Initialize()
    {
        // ★ 不再旋轉，因為現在不同方向會使用不同的 Prefab/圖片
        // RotateSpriteToDirection();
    }
    
    // 根據方向旋轉劃痕 Sprite（只旋轉 sprite，不旋轉整個物件）
    void RotateSpriteToDirection()
    {
        if (slashSprite == null)
        {
            Debug.LogWarning("[SlashTarget3D] 未設置 slashSprite！");
            return;
        }
        
        float zRotation = 0f;
        
        switch (direction)
        {
            case ScratchRhythmGame.SlashDirection.Right:
                zRotation = 180f;
                break;
            case ScratchRhythmGame.SlashDirection.Left:
                zRotation = 0f;
                break;
            case ScratchRhythmGame.SlashDirection.DownLeft:
                zRotation = 45f; // 左斜下
                break;
            case ScratchRhythmGame.SlashDirection.DownRight:
                zRotation = 135f; // 右斜下
                break;
        }
        
        slashSprite.localRotation = Quaternion.Euler(0, 0, zRotation);
    }
    
    // 標記為已完成
    public override void MarkAsCompleted()
    {
        isHit = true;
        
        // 擊中後可以播放特效或改變顏色
        if (meshRenderer != null)
        {
            meshRenderer.material.color = Color.green;
        }
        
        // 短暫延遲後銷毀
        Destroy(gameObject, 0.1f);
    }
    
    // 標記為失敗
    public override void MarkAsFailed()
    {
        isMissed = true;
        
        if (meshRenderer != null)
        {
            meshRenderer.material.color = Color.red;
        }
        
        Destroy(gameObject, 0.1f);
    }
    
    // 檢查是否還在活動
    public override bool IsActive()
    {
        return !isHit && !isMissed;
    }
    
    // 獲取當前位置
    public override Vector3 GetPosition()
    {
        return transform.position;
    }
}
