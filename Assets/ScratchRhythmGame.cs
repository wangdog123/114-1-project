using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScratchRhythmGame : MonoBehaviour
{
    public bool calijoycon = false;
    [Header("遊戲設置")]
    public VirtualCursor cursor; // 虛擬光標引用
    public int sequenceLength = 3; // 序列長度（例如：左-右-上）
    
    [Header("節奏設置")]
    public float bpm = 60f; // 每分鐘節拍數（初始值：60 = 超簡單）
    public float beatOffset = 0f; // 節拍偏移（秒）- 用於對齊音樂
    private float beatInterval; // 每拍間隔時間（秒）
    private float nextBeatTime; // 下一個節拍的時間
    private int currentBeatIndex = 0; // 當前節拍索引
    private float gameplayStartTime = 0f; // 遊戲階段開始時間（用於播放判定音效）
    private bool isInGameplayPhase = false; // 是否在遊戲階段
    
    [Header("時間判定窗口")]
    public float perfectWindow = 0.05f; // Perfect 判定窗口（±50ms）
    public float greatWindow = 0.1f; // Great 判定窗口（±100ms）
    public float goodWindow = 0.15f; // Good 判定窗口（±150ms）
    
    [Header("音效")]
    public AudioClip beatSound; // 節拍音效（目標出現時）
    public AudioClip hitSound; // 擊中音效（普通）
    public AudioClip perfectSound; // Perfect 音效
    private AudioSource audioSource;
    
    [Header("劃痕檢測設置 - v3")]
    public float minSlashSpeed = 1200f;        // 觸發劃動的最低瞬時速度 (px/s)
    public float minSlashDistance = 100f;      // 有效劃動的最短直線距離
    public float slashAnalysisWindow = 0.15f;  // 速度達標後，回溯分析的時間窗口（秒）
    public float minDirectionDot = 0.8f;       // 方向判斷的餘弦閾值（0.8 ≈ 37度內）
    public float targetMinDistance = 150f;     // 目標之間的最小生成距離
    public float targetXOffsetRange = 100f;    // 上下目標的X軸隨機偏移範圍
    
    [Header("難度設置")]
    public int currentLevel = 1;
    public float speedMultiplier = 1f; // 速度倍數
    public float flyingDuration = 8f; // 飛行時間（秒）
    public int maxSequenceLength = 8; // 最大序列長度（避免物件太多）
    
    [Header("變奏設置")]
    public int variationStartLevel = 5; // 從第幾關開始出現變奏
    public float variationChance = 0.3f; // 變奏出現機率（0-1）
    public float variationMinMultiplier = 0.75f; // 變奏最小倍數（0.75 = 間隔縮短 25%）
    public float variationMaxMultiplier = 1.25f; // 變奏最大倍數（1.25 = 間隔延長 25%）
    
    [Header("Debug 模式")]
    public bool debugMode = false; // 開啟 Debug 模式
    public KeyCode debugLeftKey = KeyCode.Alpha1; // 按 1 生成左
    public KeyCode debugRightKey = KeyCode.Alpha2; // 按 2 生成右
    public KeyCode debugUpKey = KeyCode.Alpha3; // 按 3 生成上
    public KeyCode debugDownKey = KeyCode.Alpha4; // 按 4 生成下
    public KeyCode debugClearKey = KeyCode.C; // 按 C 清除所有目標
    
    [Header("UI 引用")]
    public Transform slashTargetsParent; // 劃痕目標的父對象（2D Canvas 用）
    public GameObject slashTargetPrefab; // 統一預製體（2D/3D 共用）
    
    public TextMeshProUGUI sequenceDisplayText; // 顯示序列的文本
    public TextMeshProUGUI scoreText; // 分數文本
    public TextMeshProUGUI feedbackText; // 反饋文本（Perfect/Good/Miss）
    public TextMeshProUGUI flyingTimerText; // 飛行倒數計時文本
    public TextMeshProUGUI calibrationText; // 校正提示文本
    
    [Header("Joy-Con 校正")]
    public test joyConController; // Joy-Con 控制器腳本引用
    
    [Header("3D 飛行設置")]
    public Transform targets3DParent; // 3D 目標的父對象（場景中的空物件，非 Canvas）
    public Transform spawnPoint; // 物件發射點（遠方）
    public Transform targetPoint; // 物件目標點（鏡頭前方）
    public bool use3DMode = false; // 是否使用 3D 模式

    // 遊戲狀態
    private enum GameState { WaitingCalibration, Idle, ShowSequence, WaitingForPlayer, Checking }
    private GameState currentState;
    
    // 序列數據
    private List<SlashDirection> currentSequence = new List<SlashDirection>();
    private List<SlashTarget> activeTargets = new List<SlashTarget>(); // 2D/3D 統一列表
    private int currentStep = 0; // 當前步驟
    private int currentStepIndex = 0; // 當前等待完成的目標索引
    
    // === 劃動檢測狀態（v3）===
    private List<Vector2> positionHistory = new List<Vector2>();
    private List<float> timeHistory = new List<float>();
    private bool isSlashOnCooldown = false;
    
    // 分數
    private int score = 0;
    private int combo = 0;
    
    // ★ 間隔判定
    private float lastPlayerHitTime = -1f; // 上一次玩家擊打時間（-1表示還沒打過）
    private int hitCount = 0; // 已擊中物件數量
    private float lastSlashTime = -1f; // 上一次揮動完成時間（防止連續觸發）
    private float slashCooldown = 0.2f; // 揮動冷卻時間（秒）
    
    // 方向枚舉
    public enum SlashDirection { Left, Right, Up, Down }
    
    void Start()
    {
        // 初始化音效組件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 計算節拍間隔
        beatInterval = 60f / bpm;
        
        // 檢查是否需要校正
        if (joyConController != null && !joyConController.Calibrated && calijoycon)
        {
            currentState = GameState.WaitingCalibration;
            ShowCalibrationPrompt();
            Debug.Log("等待 Joy-Con 校正...");
            return;
        }
        
        if (!debugMode)
        {
            StartNewRound();
        }
        else
        {
            Debug.Log("=== Debug 模式已啟動 ===");
            Debug.Log("按 1: 生成左目標");
            Debug.Log("按 2: 生成右目標");
            Debug.Log("按 3: 生成上目標");
            Debug.Log("按 4: 生成下目標");
            Debug.Log("按 C: 清除所有目標");
            
            currentState = GameState.WaitingForPlayer;
            sequenceDisplayText.text = "Debug 模式 - 按數字鍵生成目標";
        }
    }
    
    void Update()
    {
        // 等待校正狀態
        if (currentState == GameState.WaitingCalibration)
        {
            // 檢查是否已完成校正
            if (joyConController != null && joyConController.Calibrated)
            {
                HideCalibrationPrompt();
                Debug.Log("校正完成！開始遊戲");
                
                if (!debugMode)
                {
                    StartNewRound();
                }
                else
                {
                    currentState = GameState.WaitingForPlayer;
                    sequenceDisplayText.text = "Debug 模式 - 按數字鍵生成目標";
                }
            }
            return; // 校正中不執行其他邏輯
        }
        
        // Debug 模式按鍵檢測
        if (debugMode)
        {
            if (Input.GetKeyDown(debugLeftKey))
            {
                Debug.Log("[Debug] 生成左目標");
                SpawnDebugTarget(SlashDirection.Left);
            }
            else if (Input.GetKeyDown(debugRightKey))
            {
                Debug.Log("[Debug] 生成右目標");
                SpawnDebugTarget(SlashDirection.Right);
            }
            else if (Input.GetKeyDown(debugUpKey))
            {
                Debug.Log("[Debug] 生成上目標");
                SpawnDebugTarget(SlashDirection.Up);
            }
            else if (Input.GetKeyDown(debugDownKey))
            {
                Debug.Log("[Debug] 生成下目標");
                SpawnDebugTarget(SlashDirection.Down);
            }
            else if (Input.GetKeyDown(debugClearKey))
            {
                Debug.Log("[Debug] 清除所有目標");
                ClearAllTargets();
            }
        }
        
        // ===== 階段1：提示階段 - 目標按節拍出現 =====
        if (currentState == GameState.ShowSequence && Time.time >= nextBeatTime)
        {
            SpawnNextBeatTarget();
        }
        
        // ===== 階段2：遊戲階段 - 檢測飛行物件和玩家操作 =====
        if (isInGameplayPhase && activeTargets.Count > 0)
        {
            // ★ 檢查所有目標的飛行狀態（2D 和 3D 統一處理）
            for (int i = 0; i < activeTargets.Count; i++)
            {
                SlashTarget target = activeTargets[i];
                if (target != null && !target.isHit && !target.isMissed)
                {
                    float currentTime = Time.time;
                    float deadlineTime = target.flyingStartTime + target.flyingDuration;
                    
                    // 播放判定音效（在飛行開始時）
                    if (!target.hasPlayedJudgmentBeat && currentTime >= target.flyingStartTime)
                    {
                        if (beatSound != null && audioSource != null)
                        {
                            audioSource.PlayOneShot(beatSound);
                            Debug.Log($"[飛行開始] 目標 #{i} ({target.direction}) 開始飛行，時間={currentTime:F2}");
                        }
                        target.hasPlayedJudgmentBeat = true;
                    }
                    
                    // 檢查是否錯過（飛到死亡線）
                    if (currentTime >= deadlineTime)
                    {
                        target.isMissed = true;
                        target.MarkAsFailed();
                        
                        // 扣分
                        int penalty = 50;
                        score = Mathf.Max(0, score - penalty);
                        combo = 0;
                        ShowFeedback($"Miss! -{penalty}");
                        UpdateUI();
                        
                        Debug.Log($"[Miss] 目標 #{i} ({target.direction}) 錯過！時間={currentTime:F2}, 死亡線={deadlineTime:F2}");
                    }
                }
            }
        }
        
        if (isInGameplayPhase)
        {
            
            // ★ 更新飛行倒數計時器（顯示下一個未擊中物件的剩餘時間）
            UpdateFlyingTimer();
            
            // 檢查遊戲是否結束（所有物件都已處理）
            bool allProcessed = true;
            foreach (var t in activeTargets)
            {
                if (t != null && !t.isHit && !t.isMissed)
                {
                    allProcessed = false;
                    break;
                }
            }
            
            if (allProcessed)
            {
                OnGameplayPhaseEnd();
                Debug.Log("[遊戲結束] 所有物件已處理完畢");
            }
        }
        
        if (currentState == GameState.WaitingForPlayer)
        {
            DetectSlashInput();
        }
    }
    
    // ★ 更新飛行倒數計時器
    void UpdateFlyingTimer()
    {
        if (flyingTimerText == null)
            return;
        
        // 找到下一個未擊中的物件
        SlashTarget nextTarget = null;
        int minStepIndex = int.MaxValue;
        
        foreach (var target in activeTargets)
        {
            if (target != null && !target.isHit && !target.isMissed)
            {
                if (target.stepIndex < minStepIndex)
                {
                    minStepIndex = target.stepIndex;
                    nextTarget = target;
                }
            }
        }
        
        if (nextTarget != null)
        {
            float remainingTime = (nextTarget.flyingStartTime + nextTarget.flyingDuration) - Time.time;
            remainingTime = Mathf.Max(0f, remainingTime);
            
            // 顯示剩餘秒數（一位小數）
            flyingTimerText.text = $"剩餘時間: {remainingTime:F1}s";
            
            // 根據剩餘時間改變顏色（警告效果）
            if (remainingTime <= 1f)
                flyingTimerText.color = Color.red; // 紅色警告
            else if (remainingTime <= 2f)
                flyingTimerText.color = Color.yellow; // 黃色提示
            else
                flyingTimerText.color = Color.white; // 正常白色
        }
        else
        {
            flyingTimerText.text = "";
        }
    }
    
    // 全螢幕揮動檢測（不需要碰到目標）
    void DetectSlashInput()
    {
        if (activeTargets.Count == 0)
            return;
        
        // ★ WASD 快速測試（模擬完成劃動）- 改為檢查飛行中的物件
        SlashDirection? pressedDirection = null;
        
        if (Input.GetKeyDown(KeyCode.A)) // 左
        {
            pressedDirection = SlashDirection.Left;
            Debug.Log("[測試] 按 A 鍵 - 模擬左劃");
        }
        else if (Input.GetKeyDown(KeyCode.D)) // 右
        {
            pressedDirection = SlashDirection.Right;
            Debug.Log("[測試] 按 D 鍵 - 模擬右劃");
        }
        else if (Input.GetKeyDown(KeyCode.W)) // 上
        {
            pressedDirection = SlashDirection.Up;
            Debug.Log("[測試] 按 W 鍵 - 模擬上劃");
        }
        else if (Input.GetKeyDown(KeyCode.S)) // 下
        {
            pressedDirection = SlashDirection.Down;
            Debug.Log("[測試] 按 S 鍵 - 模擬下劃");
        }
        
        // 如果按了 WASD，檢查是否有匹配的飛行中物件
        if (pressedDirection.HasValue)
        {
            SlashTarget targetToHit = FindFlyingTarget(pressedDirection.Value);
            if (targetToHit != null)
            {
                // 模擬一個成功的劃動
                OnSlashComplete(targetToHit, 150f, 0.3f); 
            }
            else
            {
                Debug.Log($"[測試] 沒有飛行中的 {pressedDirection.Value} 方向物件");
            }
            return;
        }
        
        // 如果按了 WASD，檢查是否有匹配的飛行中物件
        if (pressedDirection.HasValue)
        {
            SlashTarget targetToHit = FindFlyingTarget(pressedDirection.Value);
            if (targetToHit != null)
            {
                OnSlashComplete(targetToHit, 150f, 0.3f);
            }
            else
            {
                Debug.Log($"[測試] 沒有飛行中的 {pressedDirection.Value} 方向物件");
            }
            return;
        }
        
        // === 新版劃動檢測邏輯 v3 ===
        if (isSlashOnCooldown) return;

        Vector2 currentPos = cursor.GetCursorPositionInCanvas();
        float currentTime = Time.time;

        // 記錄歷史數據
        positionHistory.Add(currentPos);
        timeHistory.Add(currentTime);

        // 清理過舊的歷史數據
        while (timeHistory.Count > 0 && currentTime - timeHistory[0] > 0.5f)
        {
            timeHistory.RemoveAt(0);
            positionHistory.RemoveAt(0);
        }

        // 計算當前速度
        if (positionHistory.Count < 2) return;
        
        Vector2 lastPos = positionHistory[positionHistory.Count - 2];
        float lastTime = timeHistory[timeHistory.Count - 2];
        float speed = (Time.deltaTime > 0) ? Vector2.Distance(currentPos, lastPos) / (currentTime - lastTime) : 0;

        // 檢查速度是否達到閾值
        if (speed >= minSlashSpeed)
        {
            AnalyzeRecentSlash(currentTime);
        }
    }
    
    // ★ 尋找飛行中且方向匹配的第一個物件
    // ★ 尋找飛行中且方向匹配的第一個物件（必須按順序）
    SlashTarget FindFlyingTarget(SlashDirection direction)
    {
        float currentTime = Time.time;
        
        // ★ 找出還沒被打的第一個物件（按 stepIndex 順序）
        SlashTarget nextTarget = null;
        int minStepIndex = int.MaxValue;
        
        foreach (var target in activeTargets)
        {
            if (target != null && !target.isHit && !target.isMissed)
            {
                if (target.stepIndex < minStepIndex)
                {
                    minStepIndex = target.stepIndex;
                    nextTarget = target;
                }
            }
        }
        
        // 檢查這個目標是否符合條件
        if (nextTarget != null &&
            nextTarget.direction == direction &&
            currentTime >= nextTarget.flyingStartTime && 
            currentTime < nextTarget.flyingStartTime + nextTarget.flyingDuration)
        {
            return nextTarget;
        }
        
        return null;
    }

    // === 新版劃動檢測核心方法 v3 ===

    void AnalyzeRecentSlash(float peakTime)
    {
        // 1. 找到分析窗口的起點
        int startIndex = -1;
        for (int i = timeHistory.Count - 1; i >= 0; i--)
        {
            if (peakTime - timeHistory[i] <= slashAnalysisWindow)
            {
                startIndex = i;
            }
            else
            {
                break;
            }
        }

        if (startIndex == -1 || timeHistory.Count - startIndex < 2)
        {
            Debug.Log("[Slash v3] ✗ 分析失敗：分析窗口內數據不足");
            return;
        }

        // 2. 獲取窗口內的起點和終點
        Vector2 startPos = positionHistory[startIndex];
        Vector2 endPos = positionHistory[positionHistory.Count - 1];
        float slashTime = timeHistory[timeHistory.Count - 1] - timeHistory[startIndex];
        float slashDist = Vector2.Distance(startPos, endPos);

        // 3. 檢查距離
        if (slashDist < minSlashDistance)
        {
            Debug.Log($"[Slash v3] ✗ 分析失敗：距離太短 ({slashDist:F1} < {minSlashDistance})");
            return;
        }

        // 4. 判斷方向
        Vector2 slashVector = (endPos - startPos).normalized;
        SlashDirection dir = DetectDirection(slashVector);

        if (dir == (SlashDirection)(-1))
        {
            Debug.Log($"[Slash v3] ✗ 分析失敗：方向不明確");
            return;
        }

        // 5. 尋找目標
        SlashTarget target = FindFlyingTarget(dir);
        if (target == null)
        {
            Debug.Log($"[Slash v3] ✓ 分析成功，但無匹配目標。方向={dir}");
            return;
        }

        // 6. 成功！
        Debug.Log($"[Slash v3] ✓✓✓ 成功！方向={dir}, 距離={slashDist:F1}, 時間={slashTime:F2}s");
        OnSlashComplete(target, slashDist, slashTime);
        StartCoroutine(SlashCooldown());
    }
    
    IEnumerator SlashCooldown()
    {
        isSlashOnCooldown = true;
        // 清空歷史記錄，避免舊數據干擾
        positionHistory.Clear();
        timeHistory.Clear();
        yield return new WaitForSeconds(slashCooldown);
        isSlashOnCooldown = false;
    }

    // 方向檢測 (v3 版)
    SlashDirection DetectDirection(Vector2 vector)
    {
        if (vector.magnitude < 0.1f) return (SlashDirection)(-1);
        
        Vector2 normalized = vector.normalized;
        
        if (Vector2.Dot(normalized, Vector2.right) > minDirectionDot) return SlashDirection.Right;
        if (Vector2.Dot(normalized, Vector2.left) > minDirectionDot) return SlashDirection.Left;
        if (Vector2.Dot(normalized, Vector2.up) > minDirectionDot) return SlashDirection.Up;
        if (Vector2.Dot(normalized, Vector2.down) > minDirectionDot) return SlashDirection.Down;
        
        return (SlashDirection)(-1);
    }
    
    // === 舊版輔助方法（保留兼容）===
    
    // 獲取期望的劃動方向向量
    Vector2 GetExpectedDirection(SlashDirection dir)
    {
        switch (dir)
        {
            case SlashDirection.Right: return Vector2.right;
            case SlashDirection.Left: return Vector2.left;
            case SlashDirection.Up: return Vector2.up;
            case SlashDirection.Down: return Vector2.down;
            default: return Vector2.zero;
        }
    }
    
    // 開始新一輪
    void StartNewRound()
    {
        Debug.Log("=== 開始新一輪 ===");
        currentStep = 0;
        currentSequence.Clear();
        activeTargets.Clear();
        currentBeatIndex = 0;
        isInGameplayPhase = false;
        
        // ★ 重置擊打記錄
        lastPlayerHitTime = -1f;
        hitCount = 0;
        lastSlashTime = -1f; // 重置揮動冷卻
        
        // 生成隨機序列
        int length = sequenceLength + (currentLevel - 1); // 難度越高，序列越長
        length = Mathf.Min(length, maxSequenceLength); // 限制最大長度
        for (int i = 0; i < length; i++)
        {
            SlashDirection dir = (SlashDirection)Random.Range(0, 4);
            currentSequence.Add(dir);
        }
        
        // 設置第一個節拍時間
        nextBeatTime = Time.time + beatOffset + beatInterval;
        currentState = GameState.ShowSequence;
        currentStepIndex = 0;
        
        sequenceDisplayText.text = "記住這個順序...";
        Debug.Log($"[新回合] BPM={bpm}, 節拍間隔={beatInterval}秒, 序列長度={length}");
    }
    
    // 在節拍上生成下一個目標（提示階段）
    void SpawnNextBeatTarget()
    {
        if (currentBeatIndex >= currentSequence.Count)
        {
            // 所有目標已生成，準備開始遊戲階段
            sequenceDisplayText.text = "準備好了嗎？";
            Debug.Log("[提示階段結束] 準備開始遊戲階段...");
            
            // ★ 立刻改變狀態，防止重複觸發
            currentState = GameState.Idle;
            
            // 延遲一小段時間後開始遊戲階段
            StartCoroutine(StartGameplayPhase());
            return;
        }
        
        SlashDirection dir = currentSequence[currentBeatIndex];
        
        // 播放提示音效
        if (beatSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(beatSound);
        }
        
        GameObject targetObj;
        
        // ★ 根據模式生成 2D 或 3D 物件
        if (use3DMode)
        {
            // 3D 模式：在世界空間生成（不是 Canvas 子物件）
            Transform parent3D = targets3DParent != null ? targets3DParent : null;
            targetObj = Instantiate(slashTargetPrefab, parent3D);
            
            // 設置初始位置
            if (spawnPoint != null)
            {
                Vector3 position = spawnPoint.position;
                if (dir == SlashDirection.Up || dir == SlashDirection.Down)
                {
                    // 根據世界單位的偏移量來調整
                    float worldOffsetX = targetXOffsetRange / 100f; // 將 Canvas 單位轉換為世界單位 (可調整)
                    position.x += Random.Range(-worldOffsetX, worldOffsetX);
                }
                targetObj.transform.position = position;
            }
            
            SlashTarget3D target3D = targetObj.GetComponent<SlashTarget3D>();
            if (target3D == null)
                target3D = targetObj.AddComponent<SlashTarget3D>();
            
            target3D.direction = dir;
            target3D.stepIndex = currentBeatIndex;
            target3D.spawnTime = Time.time;
            target3D.spawnPoint = spawnPoint;
            target3D.targetPoint = targetPoint;
            
            // ★ 計算這個物件的間隔（是否要變奏）
            float thisInterval = beatInterval;
            
            if (currentBeatIndex > 0 && currentLevel >= variationStartLevel)
            {
                if (Random.value < variationChance)
                {
                    float multiplier = Random.Range(variationMinMultiplier, variationMaxMultiplier);
                    thisInterval = beatInterval * multiplier;
                    Debug.Log($"[變奏] 目標 #{currentBeatIndex} 間隔變化：{beatInterval:F2}s × {multiplier:F2} = {thisInterval:F2}s");
                }
            }
            
            target3D.customInterval = thisInterval;
            target3D.flyingStartTime = Time.time;
            target3D.flyingDuration = flyingDuration;
            
            // ★ 提示階段生成的目標不播放音效，直接標記為已播放
            target3D.hasPlayedJudgmentBeat = true;
            
            Debug.Log($"[提示階段 3D] 目標 #{currentBeatIndex} ({dir}): 間隔={thisInterval:F2}s, 飛行時間={target3D.flyingDuration:F2}s");
            
            // 預製體已經包含方向圖片，不需要設置文字
            target3D.Initialize();
            
            // 加入統一目標列表（SlashTarget3D 繼承 SlashTarget）
            activeTargets.Add(target3D);
        }
        else
        {
            // 2D 模式：在 Canvas 中生成（原邏輯）
            Vector2 position = GetTargetPosition(dir, currentBeatIndex);
            targetObj = Instantiate(slashTargetPrefab, slashTargetsParent);
            RectTransform rt = targetObj.GetComponent<RectTransform>();
            rt.anchoredPosition = position;
            
            SlashTarget target = targetObj.GetComponent<SlashTarget>();
            if (target == null)
                target = targetObj.AddComponent<SlashTarget>();
                
            target.direction = dir;
            target.stepIndex = currentBeatIndex;
            target.spawnTime = Time.time;
            
            // ★ 提示階段生成的目標不播放音效，直接標記為已播放
            target.hasPlayedJudgmentBeat = true;
            
            // ★ 計算這個物件的間隔（是否要變奏）
            float thisInterval = beatInterval;
            
            if (currentBeatIndex > 0 && currentLevel >= variationStartLevel)
            {
                if (Random.value < variationChance)
                {
                    float multiplier = Random.Range(variationMinMultiplier, variationMaxMultiplier);
                    thisInterval = beatInterval * multiplier;
                    Debug.Log($"[變奏] 目標 #{currentBeatIndex} 間隔變化：{beatInterval:F2}s × {multiplier:F2} = {thisInterval:F2}s");
                }
            }
            
            target.customInterval = thisInterval;
            target.flyingStartTime = Time.time;
            target.flyingDuration = flyingDuration;
            target.idealHitTime = 0f;
            
            Debug.Log($"[提示階段 2D] 目標 #{currentBeatIndex} ({dir}): 間隔={thisInterval:F2}s, 飛行時間={target.flyingDuration:F2}s");
            
            target.SetDirectionText(GetDirectionSymbol(dir) + (currentBeatIndex + 1));
            target.Initialize();
            
            activeTargets.Add(target);
        }
        
        // ★ 設置下一個節拍時間（使用這個物件的實際間隔）
        // 注意：thisInterval 在 2D/3D 分支中都有定義
        float intervalForNextBeat = beatInterval; // 預設值
        
        if (use3DMode)
        {
            SlashTarget3D target3D = targetObj.GetComponent<SlashTarget3D>();
            if (target3D != null)
                intervalForNextBeat = target3D.customInterval;
        }
        else
        {
            SlashTarget target = targetObj.GetComponent<SlashTarget>();
            if (target != null)
                intervalForNextBeat = target.customInterval;
        }
        
        currentBeatIndex++;
        nextBeatTime += intervalForNextBeat;
    }
    
    // 開始遊戲階段
    IEnumerator StartGameplayPhase()
    {
        currentState = GameState.Idle; // 暫停狀態
        
        // 等待一小段時間
        yield return new WaitForSeconds(beatInterval * 0.5f);
        
        sequenceDisplayText.text = "開始！跟著節拍揮動！";
        
        // 記錄遊戲階段開始時間
        gameplayStartTime = Time.time;
        
        // ★ 飛行已經在提示階段開始了，這裡只需記錄開始時間
        Debug.Log($"[遊戲階段開始] 開始時間={gameplayStartTime:F2}，物件已在飛行中");
        
        isInGameplayPhase = true;
        currentState = GameState.WaitingForPlayer;
    }
    
    // 遊戲階段結束
    void OnGameplayPhaseEnd()
    {
        isInGameplayPhase = false;
        currentState = GameState.Checking;
        
        Debug.Log("[遊戲階段結束] 檢查未完成的目標...");
        
        // 檢查未完成的目標，標記為失敗並重置 combo
        foreach (var target in activeTargets)
        {
            if (target != null && target.IsActive())
            {
                target.MarkAsFailed();
                combo = 0; // Miss 重置 combo
                Debug.Log($"[Miss] 目標 #{target.stepIndex} 未完成，Combo 重置");
            }
        }
        
        // 延遲後開始下一輪或顯示結果
        StartCoroutine(EndRound());
    }
    
    IEnumerator EndRound()
    {
        Debug.Log("[回合結束] 開始下一輪準備");
        yield return new WaitForSeconds(1f);
        
        sequenceDisplayText.text = $"完成！分數: {score}";

        yield return new WaitForSeconds(2f);
        
        // ★ 提升難度：增加 BPM 和關卡
        currentLevel++;
        bpm += 10f; // 每關增加 10 BPM（例如：60→70→80）
        beatInterval = 60f / bpm; // 重新計算節拍間隔
        speedMultiplier += 0.1f;
        
        Debug.Log($"[難度提升] Level {currentLevel}, BPM={bpm}, 間隔={beatInterval:F2}s");
        
        // 可以在這裡開始下一輪或返回主選單
        Debug.Log("清除目標並開始新一輪");
        ClearAllTargets();
        StartNewRound();
    }
    
    // 生成所有目標（使用預先計算的位置）- 用於 Debug 模式
    void SpawnAllTargets(List<Vector2> positions)
    {
        activeTargets.Clear();
        
        for (int i = 0; i < currentSequence.Count; i++)
        {
            GameObject targetObj = Instantiate(slashTargetPrefab, slashTargetsParent);
            
            RectTransform rt = targetObj.GetComponent<RectTransform>();
            rt.anchoredPosition = positions[i];
            
            SlashTarget target = targetObj.GetComponent<SlashTarget>();
            if (target == null)
                target = targetObj.AddComponent<SlashTarget>();
                
            target.direction = currentSequence[i];
            target.stepIndex = i;
            target.SetDirectionText(GetDirectionSymbol(currentSequence[i]) + (i + 1));
            target.Initialize(); // 設置完 direction 後初始化（包含旋轉）
            
            activeTargets.Add(target);
        }
    }
    
    // 获取目标位置（檢查與已存在的位置列表）
    Vector2 GetTargetPositionWithExisting(SlashDirection dir, List<Vector2> existingPositions)
    {
        Vector2 basePos = Vector2.zero;
        
        switch (dir)
        {
            case SlashDirection.Left:
                basePos = new Vector2(-200, 0);
                break;
            case SlashDirection.Right:
                basePos = new Vector2(200, 0);
                break;
            case SlashDirection.Up:
                basePos = new Vector2(0, 200);
                break;
            case SlashDirection.Down:
                basePos = new Vector2(0, -200);
                break;
        }
        
        // 添加隨機偏移，並確保與已生成的目標保持最小距離
        Vector2 finalPos;
        int maxAttempts = 20; // 最多嘗試 20 次
        int attempts = 0;
        
        do
        {
            Vector2 randomOffset = new Vector2(Random.Range(-50, 50), Random.Range(-50, 50));
            finalPos = basePos + randomOffset;
            attempts++;
            
            // 檢查與所有已生成位置的距離
            bool tooClose = false;
            foreach (var existingPos in existingPositions)
            {
                float distance = Vector2.Distance(finalPos, existingPos);
                if (distance < targetMinDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            
            if (!tooClose || attempts >= maxAttempts)
                break;
                
        } while (attempts < maxAttempts);
        
        return finalPos;
    }
    
    // 获取目标位置（舊方法，檢查 activeTargets）
    Vector2 GetTargetPosition(SlashDirection dir, int index)
    {
        Vector2 basePos = Vector2.zero;
        
        switch (dir)
        {
            case SlashDirection.Left:
                basePos = new Vector2(-200, 0);
                break;
            case SlashDirection.Right:
                basePos = new Vector2(200, 0);
                break;
            case SlashDirection.Up:
                basePos = new Vector2(Random.Range(-targetXOffsetRange, targetXOffsetRange), 200);
                break;
            case SlashDirection.Down:
                basePos = new Vector2(Random.Range(-targetXOffsetRange, targetXOffsetRange), -200);
                break;
        }
        
        // 添加隨機偏移，並確保與已生成的目標保持最小距離
        Vector2 finalPos;
        int maxAttempts = 20; // 最多嘗試 20 次
        int attempts = 0;
        
        do
        {
            Vector2 randomOffset = new Vector2(Random.Range(-50, 50), Random.Range(-50, 50));
            finalPos = basePos + randomOffset;
            attempts++;
            
            // 檢查與所有已生成目標的距離
            bool tooClose = false;
            foreach (var existingTarget in activeTargets)
            {
                float distance = Vector2.Distance(finalPos, existingTarget.GetPosition());
                if (distance < targetMinDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            
            if (!tooClose || attempts >= maxAttempts)
                break;
                
        } while (attempts < maxAttempts);
        
        return finalPos;
    }
    
    // 劃動完成（全螢幕檢測）
    void OnSlashComplete(SlashTarget target, float slashDistance, float slashTime)
    {
        Debug.Log($"[遊戲] 劃動完成：目標#{target.stepIndex}, 方向={target.direction}, 距離={slashDistance:F1}, 時間={slashTime:F2}s");
        
        float currentTime = Time.time;
        
        // ★ 檢查冷卻時間，防止一次揮動觸發多個物件
        if (lastSlashTime > 0 && currentTime - lastSlashTime < slashCooldown)
        {
            Debug.Log($"[遊戲] ✗ 揮動冷卻中（距離上次 {currentTime - lastSlashTime:F2}s < {slashCooldown:F2}s），忽略此次揮動");
            return;
        }
        
        if (currentState != GameState.WaitingForPlayer && currentState != GameState.ShowSequence)
        {
            Debug.LogWarning($"[遊戲] ✗ 遊戲狀態不對！當前狀態={currentState}");
            return;
        }
        
        // ★ 標記物件已被擊中
        target.isHit = true;
        hitCount++;
        lastSlashTime = currentTime; // 記錄揮動時間
        
        string rating;
        int points;
        AudioClip soundToPlay = hitSound;
        float timingOffset = 0f;
        
        // ★ 新的判定邏輯：間隔比較
        if (lastPlayerHitTime < 0f)
        {
            // 第一個物件：只要打到就給分
            rating = "OK";
            points = 50;
            Debug.Log($"[遊戲] 第一個物件，擊中時間={currentTime:F2}");
        }
        else
        {
            // ★ 計算玩家間隔與理想間隔的差距（使用物件的 customInterval）
            float playerInterval = currentTime - lastPlayerHitTime;
            float idealInterval = target.customInterval > 0f ? target.customInterval : beatInterval;
            timingOffset = Mathf.Abs(playerInterval - idealInterval);
            
            // 根據間隔差距評分
            if (timingOffset <= perfectWindow)
            {
                rating = "Perfect!!";
                points = 300;
                soundToPlay = perfectSound != null ? perfectSound : hitSound;
            }
            else if (timingOffset <= greatWindow)
            {
                rating = "Great!";
                points = 200;
            }
            else if (timingOffset <= goodWindow)
            {
                rating = "Good";
                points = 100;
            }
            else
            {
                rating = "OK";
                points = 50;
            }
            
            Debug.Log($"[遊戲] 間隔判定：玩家間隔={playerInterval:F3}s, 理想間隔={idealInterval:F3}s, 差距={timingOffset:F3}s");
        }
        
        // 更新上次擊打時間
        lastPlayerHitTime = currentTime;
        
        // 播放音效
        if (soundToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
        
        // 劃動成功！
        target.MarkAsCompleted();
        combo++;
        
        score += points * combo;
        ShowFeedback($"{rating} x{combo}");
        UpdateUI();
        
        // 重置光標到 (0, 0)
        if (cursor != null)
        {
            // cursor.ResetToOrigin();
        }
        
        Debug.Log($"[遊戲] ✓ 成功！評分={rating}, 得分={points}x{combo}={points*combo}, 已擊中數={hitCount}");
    }
    
    // 目標劃動完成（舊版，已廢棄）
    public void OnTargetSlashComplete(SlashTarget target, float slashDistance, float slashTime)
    {
        // 這個方法已經不再使用，保留是為了避免舊代碼調用出錯
        Debug.LogWarning("[遊戲] OnTargetSlashComplete 已廢棄，請使用全螢幕檢測模式");
    }
    
    // ★ 物件錯過懲罰（飛到死亡線但未被擊中）
    void OnMissedTarget(SlashTarget target)
    {
        target.MarkAsFailed();
        
        // 扣分
        int penalty = 50;
        score = Mathf.Max(0, score - penalty);
        
        // 重置 Combo
        combo = 0;
        
        ShowFeedback($"Miss! -{penalty}");
        UpdateUI();
        
        Debug.Log($"[Miss] 目標 #{target.stepIndex} ({target.direction}) 錯過，扣 {penalty} 分，Combo 重置");
        
        // TODO: 後續可添加
        // - Joy-Con 震動
        // - 畫面扭曲/色彩失真效果
    }
    
    // 3D 目標錯過處理
    void OnMissedTarget3D(SlashTarget3D target)
    {
        target.MarkAsFailed();
        
        // 扣分
        int penalty = 50;
        score = Mathf.Max(0, score - penalty);
        
        // 重置 Combo
        combo = 0;
        
        ShowFeedback($"Miss! -{penalty}");
        UpdateUI();
        
        Debug.Log($"[Miss 3D] 目標 #{target.stepIndex} ({target.direction}) 錯過，扣 {penalty} 分，Combo 重置");
    }
    
    // 序列完成
    void OnSequenceComplete()
    {
        currentState = GameState.Checking;
        combo++;
        ShowFeedback($"完美！Combo x{combo}");
        
        // ★ 提升難度：增加 BPM
        currentLevel++;
        bpm += 10f; // 每關增加 10 BPM（例如：120→130→140）
        beatInterval = 60f / bpm; // 重新計算節拍間隔
        speedMultiplier += 0.1f;
        
        Debug.Log($"[難度提升] Level {currentLevel}, BPM={bpm}, 間隔={beatInterval:F2}s");
        
        // 清除目標
        foreach (var target in activeTargets)
        {
            if (target != null)
                Destroy(target.gameObject);
        }
        
        // 開始下一輪
        Invoke("EndRound", 1.5f);  
    }
    
    // 序列失敗
    void OnSequenceFailed(string reason)
    {
        currentState = GameState.Checking;
        combo = 0;
        ShowFeedback(reason);
        
        // 降低難度
        if (currentLevel > 1)
            currentLevel--;
        speedMultiplier = Mathf.Max(1f, speedMultiplier - 0.1f);
        
        // 清除目標
        foreach (var target in activeTargets)
        {
            if (target != null)
                Destroy(target.gameObject);
        }
        
        // 重新開始
        Invoke("EndRound", 2f);
    }
    
    // 更新分數UI
    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score}\nLevel: {currentLevel}\nCombo: x{combo}";
    }
    
    // 更新分數UI（舊版別名）
    void UpdateScoreUI()
    {
        UpdateUI();
    }
    
    // 顯示反饋
    void ShowFeedback(string text)
    {
        if (feedbackText != null)
        {
            feedbackText.text = text;
            feedbackText.gameObject.SetActive(true);
            CancelInvoke("HideFeedback");
            Invoke("HideFeedback", 1f);
        }
    }
    
    void HideFeedback()
    {
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
    }
    
    // 顯示校正提示
    void ShowCalibrationPrompt()
    {
        if (calibrationText != null)
        {
            calibrationText.text = "請按 B 鍵進行 Joy-Con 校正";
            calibrationText.gameObject.SetActive(true);
        }
        
        // 隱藏其他 UI
        if (sequenceDisplayText != null)
            sequenceDisplayText.gameObject.SetActive(false);
        if (scoreText != null)
            scoreText.gameObject.SetActive(false);
        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);
        if (flyingTimerText != null)
            flyingTimerText.gameObject.SetActive(false);
    }
    
    // 隱藏校正提示
    void HideCalibrationPrompt()
    {
        if (calibrationText != null)
            calibrationText.gameObject.SetActive(false);
        
        // 顯示遊戲 UI
        if (sequenceDisplayText != null)
            sequenceDisplayText.gameObject.SetActive(true);
        if (scoreText != null)
            scoreText.gameObject.SetActive(true);
        if (flyingTimerText != null)
            flyingTimerText.gameObject.SetActive(true);
    }
    
    // 獲取方向符號
    string GetDirectionSymbol(SlashDirection dir)
    {
        switch (dir)
        {
            case SlashDirection.Left: return "←";
            case SlashDirection.Right: return "→";
            case SlashDirection.Up: return "↑";
            case SlashDirection.Down: return "↓";
            default: return "?";
        }
    }
    
    // === Debug 模式方法 ===
    
    // 生成單個 Debug 目標
    void SpawnDebugTarget(SlashDirection direction)
    {
        GameObject targetObj;
        
        if (use3DMode)
        {
            // 3D 模式：在世界空間生成
            Transform parent3D = targets3DParent != null ? targets3DParent : null;
            targetObj = Instantiate(slashTargetPrefab, parent3D);
            
            if (spawnPoint != null)
            {
                Vector3 position = spawnPoint.position;
                if (direction == SlashDirection.Up || direction == SlashDirection.Down)
                {
                    // 根據世界單位的偏移量來調整
                    float worldOffsetX = targetXOffsetRange / 100f; // 將 Canvas 單位轉換為世界單位 (可調整)
                    position.x += Random.Range(-worldOffsetX, worldOffsetX);
                }
                targetObj.transform.position = position;
            }
            
            SlashTarget3D target3D = targetObj.GetComponent<SlashTarget3D>();
            if (target3D == null)
                target3D = targetObj.AddComponent<SlashTarget3D>();
            
            target3D.direction = direction;
            target3D.stepIndex = activeTargets.Count;
            target3D.spawnTime = Time.time;
            target3D.hasPlayedJudgmentBeat = false;
            target3D.spawnPoint = spawnPoint;
            target3D.targetPoint = targetPoint;
            target3D.customInterval = beatInterval;
            target3D.flyingStartTime = Time.time;
            target3D.flyingDuration = flyingDuration;
            target3D.Initialize();
            
            // 加入統一目標列表
            activeTargets.Add(target3D);
            
            Debug.Log($"[Debug 3D] 已生成目標 #{activeTargets.Count}: {direction}");
        }
        else
        {
            // 2D 模式
            // ★ 修正：讓 Debug 模式也使用帶有隨機偏移的 GetTargetPosition
            Vector2 position = GetTargetPosition(direction, activeTargets.Count);
            
            targetObj = Instantiate(slashTargetPrefab, slashTargetsParent);
            RectTransform rt = targetObj.GetComponent<RectTransform>();
            rt.anchoredPosition = position;
            
            SlashTarget target = targetObj.GetComponent<SlashTarget>();
            if (target == null)
                target = targetObj.AddComponent<SlashTarget>();
                
            target.direction = direction;
            target.stepIndex = activeTargets.Count;
            target.SetDirectionText(GetDirectionSymbol(direction) + (activeTargets.Count + 1));
            target.Initialize();
            
            activeTargets.Add(target);
            
            Debug.Log($"[Debug 2D] 已生成目標 #{activeTargets.Count - 1}: {direction}, 位置={position}");
        }
    }
    
    // 獲取 Debug 目標位置（固定位置，不檢查重疊）
    Vector2 GetDebugTargetPosition(SlashDirection dir)
    {
        switch (dir)
        {
            case SlashDirection.Left:
                return new Vector2(-200, 0);
            case SlashDirection.Right:
                return new Vector2(200, 0);
            case SlashDirection.Up:
                return new Vector2(0, 200);
            case SlashDirection.Down:
                return new Vector2(0, -200);
            default:
                return Vector2.zero;
        }
    }
    
    // 清除所有目標
    void ClearAllTargets()
    {
        // 清除所有目標（2D 和 3D 統一）
        foreach (var target in activeTargets)
        {
            if (target != null)
                Destroy(target.gameObject);
        }
        activeTargets.Clear();
        
        currentStepIndex = 0;
        currentStep = 0;
        
        Debug.Log("[Debug] 已清除所有目標");
    }
}
