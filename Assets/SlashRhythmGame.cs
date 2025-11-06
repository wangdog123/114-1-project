// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// /// <summary>
// /// 劃痕節奏遊戲管理器
// /// 玩家需要按照螢幕上出現的劃痕方向順序來揮動 Joy-Con
// /// </summary>
// public class SlashRhythmGame : MonoBehaviour
// {
//     [Header("=== 遊戲設定 ===")]
//     [Tooltip("當前關卡/難度")]
//     public int currentLevel = 1;
    
//     [Tooltip("是否包含斜向方向")]
//     public bool includeDiagonalDirections = false;
    
//     [Tooltip("每個關卡的劃痕數量")]
//     public int slashesPerLevel = 3;
    
//     [Tooltip("每升一級增加的劃痕數")]
//     public int slashIncreasePerLevel = 1;
    
//     [Tooltip("顯示劃痕的時間（秒）")]
//     public float displayDuration = 2f;
    
//     [Tooltip("每升一級減少的顯示時間")]
//     public float timeDecreasePerLevel = 0.1f;
    
//     [Tooltip("最短顯示時間")]
//     public float minDisplayDuration = 0.5f;
    
//     [Tooltip("完成後的暫停時間")]
//     public float pauseAfterCompletion = 1f;
    
//     [Header("=== UI 參考 ===")]
//     [Tooltip("顯示劃痕圖示的父物件")]
//     public Transform slashIconContainer;
    
//     [Tooltip("劃痕圖示預製件 - 需要有 Image 和 SlashIcon 組件")]
//     public GameObject slashIconPrefab;
    
//     [Tooltip("分數文字")]
//     public Text scoreText;
    
//     [Tooltip("關卡文字")]
//     public Text levelText;
    
//     [Tooltip("提示文字")]
//     public Text hintText;
    
//     [Header("=== 參考 ===")]
//     [Tooltip("SwingDetector 腳本（兩者選一）")]
//     public SwingDetector swingDetector;
    
//     [Tooltip("SwingDetectorAdvanced 腳本（推薦用於節奏遊戲）")]
//     public SwingDetectorAdvanced swingDetectorAdvanced;
    
//     [Header("=== 音效 (可選) ===")]
//     public AudioClip correctSound;
//     public AudioClip wrongSound;
//     public AudioClip levelCompleteSound;
//     private AudioSource audioSource;
    
//     // 遊戲狀態
//     private enum GameState
//     {
//         Showing,    // 顯示劃痕序列
//         Waiting,    // 等待玩家輸入
//         Paused      // 暫停（完成後）
//     }
    
//     private GameState currentState = GameState.Showing;
    
//     // 當前關卡的劃痕序列
//     private List<SwingDetector.SwingDirection> currentSequence = new List<SwingDetector.SwingDirection>();
    
//     // 玩家輸入的序列
//     private List<SwingDetector.SwingDirection> playerInput = new List<SwingDetector.SwingDirection>();
    
//     // 當前應該輸入的索引
//     private int currentInputIndex = 0;
    
//     // 分數
//     private int score = 0;
    
//     // UI 圖示列表
//     private List<SlashIcon> slashIcons = new List<SlashIcon>();
    
//     // 上一幀檢測到的揮動
//     private SwingDetector.SwingDirection lastDetectedSwing = SwingDetector.SwingDirection.None;
    
//     void Start()
//     {
//         // 取得或創建 AudioSource
//         audioSource = GetComponent<AudioSource>();
//         if (audioSource == null)
//         {
//             audioSource = gameObject.AddComponent<AudioSource>();
//         }
        
//         // 如果沒有指定 SwingDetector，嘗試自動找到
//         if (swingDetector == null && swingDetectorAdvanced == null)
//         {
//             swingDetector = FindObjectOfType<SwingDetector>();
//             swingDetectorAdvanced = FindObjectOfType<SwingDetectorAdvanced>();
//         }
        
//         if (swingDetector == null && swingDetectorAdvanced == null)
//         {
//             Debug.LogError("[SlashRhythmGame] 找不到揮動檢測器！請確保場景中有 SwingDetector 或 SwingDetectorAdvanced");
//             enabled = false;
//             return;
//         }
        
//         // 優先使用 Advanced 版本
//         if (swingDetectorAdvanced != null)
//         {
//             Debug.Log("[SlashRhythmGame] 使用 SwingDetectorAdvanced（節奏遊戲優化版）");
//         }
        
//         // 開始遊戲
//         StartNewRound();
//     }
    
//     void Update()
//     {
//         if (currentState == GameState.Waiting)
//         {
//             // 檢測玩家揮動（支援兩種檢測器）
//             SwingDetector.SwingDirection swing = SwingDetector.SwingDirection.None;
            
//             if (swingDetectorAdvanced != null)
//             {
//                 // 使用 Advanced 版本（轉換枚舉）
//                 var advSwing = swingDetectorAdvanced.GetCurrentSwing();
//                 swing = (SwingDetector.SwingDirection)((int)advSwing);
//             }
//             else if (swingDetector != null)
//             {
//                 swing = swingDetector.GetCurrentSwing();
//             }
            
//             // 避免重複檢測同一次揮動
//             if (swing != SwingDetector.SwingDirection.None && swing != lastDetectedSwing)
//             {
//                 OnPlayerSwing(swing);
//             }
            
//             lastDetectedSwing = swing;
//         }
        
//         UpdateUI();
//     }
    
//     /// <summary>
//     /// 開始新的一輪
//     /// </summary>
//     void StartNewRound()
//     {
//         Debug.Log($"<color=cyan>[SlashRhythmGame] 開始第 {currentLevel} 關</color>");
        
//         // 計算當前關卡的劃痕數量
//         int slashCount = slashesPerLevel + (currentLevel - 1) * slashIncreasePerLevel;
        
//         // 生成隨機序列
//         currentSequence.Clear();
//         for (int i = 0; i < slashCount; i++)
//         {
//             SwingDetector.SwingDirection dir;
            
//             if (includeDiagonalDirections)
//             {
//                 // 八方位：1-8 (Up, Down, Left, Right, UpLeft, UpRight, DownLeft, DownRight)
//                 dir = (SwingDetector.SwingDirection)Random.Range(1, 9);
//             }
//             else
//             {
//                 // 四方位：只有 Up(1), Down(2), Left(3), Right(4)
//                 dir = (SwingDetector.SwingDirection)Random.Range(1, 5);
//             }
            
//             currentSequence.Add(dir);
//         }
        
//         // 重置玩家輸入
//         playerInput.Clear();
//         currentInputIndex = 0;
        
//         // 顯示序列
//         StartCoroutine(ShowSequence());
//     }
    
//     /// <summary>
//     /// 顯示劃痕序列
//     /// </summary>
//     IEnumerator ShowSequence()
//     {
//         currentState = GameState.Showing;
        
//         if (hintText != null)
//         {
//             hintText.text = "記住這些方向！";
//         }
        
//         // 清除舊的圖示
//         foreach (var icon in slashIcons)
//         {
//             if (icon != null)
//             {
//                 Destroy(icon.gameObject);
//             }
//         }
//         slashIcons.Clear();
        
//         // 創建新的圖示
//         if (slashIconContainer != null && slashIconPrefab != null)
//         {
//             foreach (var direction in currentSequence)
//             {
//                 GameObject iconObj = Instantiate(slashIconPrefab, slashIconContainer);
//                 SlashIcon icon = iconObj.GetComponent<SlashIcon>();
//                 if (icon != null)
//                 {
//                     icon.SetDirection(direction);
//                     slashIcons.Add(icon);
//                 }
//             }
//         }
        
//         // 計算顯示時間
//         float displayTime = Mathf.Max(
//             minDisplayDuration,
//             displayDuration - (currentLevel - 1) * timeDecreasePerLevel
//         );
        
//         Debug.Log($"序列: {string.Join(" → ", currentSequence)} | 顯示時間: {displayTime:F1}秒");
        
//         yield return new WaitForSeconds(displayTime);
        
//         // 隱藏序列（讓圖示半透明或隱藏）
//         foreach (var icon in slashIcons)
//         {
//             if (icon != null)
//             {
//                 icon.Hide();
//             }
//         }
        
//         // 開始等待玩家輸入
//         currentState = GameState.Waiting;
        
//         if (hintText != null)
//         {
//             hintText.text = "開始揮動！";
//         }
//     }
    
//     /// <summary>
//     /// 玩家揮動時觸發
//     /// </summary>
//     void OnPlayerSwing(SwingDetector.SwingDirection direction)
//     {
//         Debug.Log($"[SlashRhythmGame] 玩家揮動: {direction}");
        
//         // 檢查是否正確
//         if (currentInputIndex < currentSequence.Count)
//         {
//             SwingDetector.SwingDirection expectedDirection = currentSequence[currentInputIndex];
            
//             if (direction == expectedDirection)
//             {
//                 // 正確！
//                 OnCorrectSwing();
//             }
//             else
//             {
//                 // 錯誤！
//                 OnWrongSwing(direction, expectedDirection);
//             }
//         }
//     }
    
//     /// <summary>
//     /// 正確的揮動
//     /// </summary>
//     void OnCorrectSwing()
//     {
//         Debug.Log($"<color=green>✓ 正確！({currentInputIndex + 1}/{currentSequence.Count})</color>");
        
//         // 更新 UI 圖示
//         if (currentInputIndex < slashIcons.Count)
//         {
//             slashIcons[currentInputIndex].MarkAsCorrect();
//         }
        
//         // 播放音效
//         PlaySound(correctSound);
        
//         // 增加分數
//         score += 10 * currentLevel;
        
//         currentInputIndex++;
        
//         // 檢查是否完成
//         if (currentInputIndex >= currentSequence.Count)
//         {
//             OnRoundComplete();
//         }
//     }
    
//     /// <summary>
//     /// 錯誤的揮動
//     /// </summary>
//     void OnWrongSwing(SwingDetector.SwingDirection actual, SwingDetector.SwingDirection expected)
//     {
//         Debug.Log($"<color=red>✗ 錯誤！期望: {expected}, 實際: {actual}</color>");
        
//         // 更新 UI 圖示
//         if (currentInputIndex < slashIcons.Count)
//         {
//             slashIcons[currentInputIndex].MarkAsWrong();
//         }
        
//         // 播放音效
//         PlaySound(wrongSound);
        
//         // 減分（可選）
//         score = Mathf.Max(0, score - 5);
        
//         // 重新開始本輪
//         StartCoroutine(RestartRoundAfterDelay());
//     }
    
//     /// <summary>
//     /// 完成本輪
//     /// </summary>
//     void OnRoundComplete()
//     {
//         Debug.Log($"<color=yellow>★ 第 {currentLevel} 關完成！</color>");
        
//         if (hintText != null)
//         {
//             hintText.text = "完美！";
//         }
        
//         // 播放音效
//         PlaySound(levelCompleteSound);
        
//         // 增加額外分數
//         score += 50 * currentLevel;
        
//         // 升級
//         currentLevel++;
        
//         // 暫停後進入下一關
//         StartCoroutine(NextRoundAfterDelay());
//     }
    
//     /// <summary>
//     /// 延遲後重新開始本輪
//     /// </summary>
//     IEnumerator RestartRoundAfterDelay()
//     {
//         currentState = GameState.Paused;
//         yield return new WaitForSeconds(pauseAfterCompletion);
//         StartNewRound();
//     }
    
//     /// <summary>
//     /// 延遲後進入下一關
//     /// </summary>
//     IEnumerator NextRoundAfterDelay()
//     {
//         currentState = GameState.Paused;
//         yield return new WaitForSeconds(pauseAfterCompletion);
//         StartNewRound();
//     }
    
//     /// <summary>
//     /// 更新 UI
//     /// </summary>
//     void UpdateUI()
//     {
//         if (scoreText != null)
//         {
//             scoreText.text = $"分數: {score}";
//         }
        
//         if (levelText != null)
//         {
//             levelText.text = $"關卡: {currentLevel}";
//         }
//     }
    
//     /// <summary>
//     /// 播放音效
//     /// </summary>
//     void PlaySound(AudioClip clip)
//     {
//         if (audioSource != null && clip != null)
//         {
//             audioSource.PlayOneShot(clip);
//         }
//     }
    
//     /// <summary>
//     /// 重置遊戲
//     /// </summary>
//     public void ResetGame()
//     {
//         currentLevel = 1;
//         score = 0;
//         StartNewRound();
//     }
// }
