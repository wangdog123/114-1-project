using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;  // ★ TextMeshPro 命名空間

/// <summary>
/// 記分板系統：追蹤遊戲中的所有統計數據
/// 記錄：Perfect/Good/OK/Miss 次數、最高 Combo、總積分
/// ★ 場景切換時保留數據（DontDestroyOnLoad）
/// ★ 支援 UI 實時更新
/// </summary>
public class scoreingame : MonoBehaviour
{
    [Header("記分板統計")]
    public int perfectCount = 0;      // Perfect 次數
    public int goodCount = 0;         // Good 次數
    public int okCount = 0;           // OK 次數
    public int missCount = 0;         // Miss 次數
    public int maxCombo = 0;          // 最高 Combo
    public int totalScore = 0;        // 總積分

    private int currentCombo = 0;     // 當前 Combo

    [Header("UI 引用")]
    public TextMeshProUGUI perfectText;      // Perfect 計數 UI
    public TextMeshProUGUI goodText;         // Good 計數 UI
    public TextMeshProUGUI okText;           // OK 計數 UI
    public TextMeshProUGUI missText;         // Miss 計數 UI
    public TextMeshProUGUI maxComboText;     // 最高 Combo UI
    public TextMeshProUGUI totalScoreText;   // 總積分 UI

    [Header("Debug")]
    public bool debugLog = true;

    private static scoreingame instance;
    public SceneController sceneController;

    void Awake()
    {
        // 單例模式 + 場景切換時保留
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);  // ★ 保留此物件在場景切換時
        
        if (debugLog)
            Debug.Log("[記分板] 已初始化為持久化單例");
    }

    void OnEnable()
    {
        // 訂閱判定事件
        ScratchRhythmGame rhythmGame = FindObjectOfType<ScratchRhythmGame>();
        if (rhythmGame != null)
        {
            // 这里可以添加事件订阅逻辑（如果 ScratchRhythmGame 暴露了事件的话）
        }
    }

    void OnDisable()
    {
        // 取消訂閱
    }

    /// <summary>
    /// 初始化記分板（Gameplay 階段開始時調用）
    /// 只在第一次初始化時重置數據，之後保留累積的統計
    /// </summary>
    public void InitializeScoreboard(bool forceReset = false)
    {
        // ★ 如果數據已經初始化過且不強制重置，就保留現有數據
        bool alreadyInitialized = perfectCount > 0 || goodCount > 0 || okCount > 0 || missCount > 0 || totalScore > 0;
        
        if (alreadyInitialized && !forceReset)
        {
            if (debugLog)
                Debug.Log($"[記分板] 保留已有數據：Perfect={perfectCount}, Good={goodCount}, OK={okCount}, Miss={missCount}, Score={totalScore}");
            return;
        }
        
        // 只在第一次或強制重置時才清零
        perfectCount = 0;
        goodCount = 0;
        okCount = 0;
        missCount = 0;
        maxCombo = 0;
        totalScore = 0;
        currentCombo = 0;

        if (debugLog)
            Debug.Log("[記分板] 已初始化（首次進入 Gameplay），準備開始遊戲");
    }

    /// <summary>
    /// 記錄一次判定結果
    /// </summary>
    public void RecordJudgment(string rating, int points, int currentCombo)
    {
        this.currentCombo = currentCombo;
        
        // ★ 轉換 rating 元文本（例如 "Perfect!!" -> "perfect"）
        string ratingType = rating.ToLower();
        if (ratingType.Contains("perfect"))
            ratingType = "perfect";
        else if (ratingType.Contains("good"))
            ratingType = "good";
        else if (ratingType.Contains("ok"))
            ratingType = "ok";
        else if (ratingType.Contains("miss"))
            ratingType = "miss";

        switch (ratingType)
        {
            case "perfect":
                perfectCount++;
                break;
            case "good":
                goodCount++;
                break;
            case "ok":
                okCount++;
                break;
            case "miss":
                missCount++;
                currentCombo = 0; // Miss 時重置 Combo
                break;
        }

        // 更新最高 Combo
        if (currentCombo > maxCombo)
        {
            maxCombo = currentCombo;
        }

        if (debugLog)
            Debug.Log($"[記分板] {rating} | Perfect={perfectCount}, Good={goodCount}, OK={okCount}, Miss={missCount}, Combo={currentCombo}, MaxCombo={maxCombo}");
        
        // ★ 更新 UI
        UpdateUI();
    }

    /// <summary>
    /// 更新總積分
    /// </summary>
    public void UpdateTotalScore(int score)
    {
        totalScore = score;
        
        // ★ 更新 UI
        UpdateUI();
    }
    
    /// <summary>
    /// 更新所有 UI 文字（根據當前統計數據）
    /// </summary>
    public void UpdateUI()
    {
        // ★ 使用格式化字串顯示標籤和數值
        if (perfectText != null)
            // perfectText.text = $"Perfect:{perfectCount}";
            perfectText.text = perfectCount.ToString();
        
        if (goodText != null)
            // goodText.text = $"Good:{goodCount}";
            goodText.text = goodCount.ToString();
        
        if (okText != null)
            // okText.text = $"OK:{okCount}";
            okText.text = okCount.ToString();
        
        if (missText != null)
            // missText.text = $"Miss:{missCount}";
            missText.text = missCount.ToString();
        
        if (maxComboText != null)
            // maxComboText.text = $"Max Combo:{maxCombo}";
            maxComboText.text = maxCombo.ToString();
        
        if (totalScoreText != null)
            // totalScoreText.text = $"Score:{totalScore}";
            totalScoreText.text = totalScore.ToString();
    }

    /// <summary>
    /// 取得統計數據
    /// </summary>
    public Dictionary<string, int> GetStats()
    {
        return new Dictionary<string, int>
        {
            { "perfect", perfectCount },
            { "good", goodCount },
            { "ok", okCount },
            { "miss", missCount },
            { "maxCombo", maxCombo },
            { "totalScore", totalScore }
        };
    }

    /// <summary>
    /// 列印統計報告
    /// </summary>
    public void PrintReport()
    {
        int totalAttempts = perfectCount + goodCount + okCount + missCount;
        float accuracy = totalAttempts > 0 ? (perfectCount + goodCount) / (float)totalAttempts * 100 : 0;

        Debug.Log("=== 遊戲統計報告 ===");
        Debug.Log($"Perfect: {perfectCount}");
        Debug.Log($"Good: {goodCount}");
        Debug.Log($"OK: {okCount}");
        Debug.Log($"Miss: {missCount}");
        Debug.Log($"總嘗試: {totalAttempts}");
        Debug.Log($"準確度: {accuracy:F1}%");
        Debug.Log($"最高 Combo: {maxCombo}");
        Debug.Log($"總積分: {totalScore}");
        Debug.Log("===================");
    }
    void Update()
    {
        if(sceneController.currentState == SceneController.GameState.Gameplay&&totalScore<=1500)
        {
            sceneController.SetEndingType(SceneController.EndingType.Bad);
        }
        else if(sceneController.currentState == SceneController.GameState.Gameplay&&totalScore>1500)
        {
            sceneController.SetEndingType(SceneController.EndingType.Good);
        }
    }
}
