using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ScratchRhythmGame : MonoBehaviour
{
    public KeyCode startButton = KeyCode.Space;
    public bool calijoycon = false;
    [Header("遊戲設置")]
    public VirtualCursor[] cursors; // 虛擬光標陣列（支援多個控制器）
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
    public float goodWindow = 0.1f; // Good 判定窗口（±100ms）
    public float okWindow = 0.3f; // OK 判定窗口（±300ms）
    
    [Header("音效")]
    public AudioClip beatSound; // 節拍音效（目標出現時）
    public AudioClip hitSound; // 擊中音效（普通）
    public AudioClip perfectSound; // Perfect 音效
    private AudioSource audioSource;
    
    [Header("VFX 特效")]
    public RhythmGameVFXManager vfxManager; // VFX 管理器（可選）
    
    [Header("震動回饋")]
    public VibrationManager vibrationManager; // 震動管理器（可選）

    [Header("視差背景管理")]
    public ParallaxManager parallaxManager; // 視差背景管理器（可選）
    
    [Header("時間管理")]
    public TimeUIController timeUIController; // 時間管理器（用於檢查時間是否到期）
    
    [Header("劃痕檢測設置 - v3")]
    public float minSlashAccel = 1.5f;         // 觸發劃動的最低加速度閾值（使用 Joy-Con 加速度計）
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
    public KeyCode debugEasy = KeyCode.Alpha1; // 按 1 生成左
    public KeyCode debugNormal = KeyCode.Alpha2; // 按 2 生成右
    public KeyCode debugHard = KeyCode.Alpha3; // 按 3 生成上
    public KeyCode debugClearKey = KeyCode.C; // 按 C 清除所有目標
    
    [Header("UI 引用")]
    public GameObject slashTargetPrefab; // 3D 目標預製體
    
    public TextMeshProUGUI sequenceDisplayText; // 顯示序列的文本
    public TextMeshProUGUI scoreText; // 分數文本
    public TextMeshProUGUI feedbackText; // 反饋文本（Perfect/Good/Miss）
    public TextMeshProUGUI flyingTimerText; // 飛行倒數計時文本
    public TextMeshProUGUI calibrationText; // 校正提示文本
    
    [Header("節拍指示器")]
    public Image perfectCircle; // Perfect 圓圈（小圈，固定大小）
    public Image timingCircle; // 時機圓圈（大圈，會縮放）
    public float perfectCircleSize = 50f; // Perfect 圓圈大小
    public float maxCircleSize = 200f; // 時機圓圈最大大小
    public float shrinkStartDelay = 0.5f; // 提示階段結束後多久開始縮小
    public float perfectTolerance = 0.02f; // Perfect 超出容錯（20ms）
    
    [Header("Joy-Con 校正")]
    public test joyConController; // Joy-Con 控制器腳本引用（主要手把）
    public test joyConController2; // 第二個 Joy-Con 控制器腳本引用（選填）
    public MultiSwitchControllerManager controllerManager; // 控制器管理器引用
    
    [Header("3D 飛行設置")]
    public Transform targets3DParent; // 3D 目標的父對象（場景中的空物件）
    public Transform spawnPoint; // 物件發射點（遠方）
    public Transform targetPoint; // 物件目標點（鏡頭前方）
    public float arcHeight = 2f; // 飛行拱高度

    // 遊戲狀態
    private enum GameState { WaitingCalibration, WaitingForStart, Idle, ShowSequence, WaitingForPlayer, Checking }
    private GameState currentState;
    
    // 序列數據
    private List<SlashDirection> currentSequence = new List<SlashDirection>();
    private List<SlashTarget> activeTargets = new List<SlashTarget>(); // 3D 目標列表
    private int currentStep = 0; // 當前步驟
    
    // 節拍指示器內部狀態
    private float nextExpectedHitTime = -1f; // 下一次預期擊打時間
    private float currentTargetInterval = 0f; // 當前目標的間隔
    private int currentHitIndex = 0; // 當前擊打索引
    private bool isIndicatorActive = false; // 指示器是否啟動
    private int currentStepIndex = 0; // 當前等待完成的目標索引


    // ★ 新增節奏生成器相關變數
    private RhythmGenerator rhythmGenerator = new RhythmGenerator();
    private List<NoteType> currentPattern = new List<NoteType>();
    private int currentPatternIndex = 0;
    private int currentSequenceIndex = 0;
    
    // ★ 遊戲時間控制
    private float gameSessionStartTime = -1f;
    
    // === 劃動檢測狀態（v3）- 改為每個控制器獨立追蹤 ===
    private class SlashDetectionState
    {
        public List<Vector2> positionHistory = new List<Vector2>();
        public List<float> timeHistory = new List<float>();
        public bool isSlashOnCooldown = false;
        public float lastAccelCheckTime = 0f; // 上次加速度檢查時間
    }
    
    private Dictionary<int, SlashDetectionState> slashStates = new Dictionary<int, SlashDetectionState>();
    
    // Joy-Con 控制器對應索引（用於多控制器追蹤）
    private Dictionary<int, test> cursorToController = new Dictionary<int, test>();
    
    // 分數
    private int score = 0;
    private int combo = 0;
    
    // ★ 間隔判定
    private float lastPlayerHitTime = -1f; // 上一次玩家擊打時間（-1表示還沒打過）
    private int hitCount = 0; // 已擊中物件數量
    private float lastSlashTime = -1f; // 上一次揮動完成時間（防止連續觸發）
    private float slashCooldown = 0.2f; // 揮動冷卻時間（秒）
    
    // 方向枚舉（大野狼抓取動作）
    public enum SlashDirection { Left, Right, DownLeft, DownRight }
    
    void OnEnable()
    {
        // 初始化音效組件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 自動尋找震動管理器
        if (vibrationManager == null)
        {
            vibrationManager = FindObjectOfType<VibrationManager>();
            if (vibrationManager != null)
            {
                Debug.Log("[ScratchRhythmGame] 找到 VibrationManager");
            }
            else
            {
                Debug.LogWarning("[ScratchRhythmGame] 找不到 VibrationManager，震動功能將無法使用");
            }
        }

        // 自動尋找視差背景管理器
        if (parallaxManager == null)
        {
            parallaxManager = FindObjectOfType<ParallaxManager>();
            if (parallaxManager != null)
            {
                Debug.Log("[ScratchRhythmGame] 找到 ParallaxManager");
            }
        }
        
        // 自動尋找時間管理器
        if (timeUIController == null)
        {
            timeUIController = FindObjectOfType<TimeUIController>();
            if (timeUIController != null)
            {
                Debug.Log("[ScratchRhythmGame] 找到 TimeUIController");
            }
        }
        
        // 初始化每個光標的檢測狀態和控制器映射
        if (cursors != null)
        {

            for (int i = 0; i < cursors.Length; i++)
            {
                if (cursors[i] != null)
                {
                    slashStates[i] = new SlashDetectionState();
                    
                    // 映射光標到對應的 Joy-Con 控制器
                    if (i == 0 && joyConController != null)
                    {
                        // 強制設定左手把為 Index 1
                        // joyConController.controllerIndex = 1;
                        cursorToController[i] = joyConController;
                        Debug.Log($"[ScratchRhythmGame] 強制設定左手把 (Cursor 0) 為 Controller Index 1");
                    }
                    else if (i == 1 && joyConController2 != null)
                    {
                        // 強制設定右手把為 Index 0
                        // joyConController2.controllerIndex = 0;
                        cursorToController[i] = joyConController2;
                        Debug.Log($"[ScratchRhythmGame] 強制設定右手把 (Cursor 1) 為 Controller Index 0");
                    }
                }
            }
        }
        
        // 計算節拍間隔
        beatInterval = 60f / bpm;
        
        // 檢查是否需要校正（只檢查已連接的手把）
        if (calijoycon && !AreAllConnectedControllersCalibrated())
        {
            currentState = GameState.WaitingCalibration;
            ShowCalibrationPrompt();
            Debug.Log("等待已連接的 Joy-Con 校正...");
            return;
        }

    }
    
    void Update()
    {
        
        // 檢查是否需要校正（無論什麼狀態，只要需要校正就切換）
        if (calijoycon && !AreAllConnectedControllersCalibrated() && currentState != GameState.WaitingCalibration)
        {
            currentState = GameState.WaitingCalibration;
            ShowCalibrationPrompt();
            Debug.Log("[Update] 檢測到未校正的手把，進入校正模式");
            return;
        }

        // 等待校正狀態
        if (currentState == GameState.WaitingCalibration)
        {
            // 檢查是否所有已連接的手把都已完成校正
            if (AreAllConnectedControllersCalibrated())
            {
                HideCalibrationPrompt();
                Debug.Log("所有已連接的手把校正完成！開始遊戲");
                
                if (!debugMode)
                {
                    currentState = GameState.WaitingForStart;
                    // StartNewRound();
                }
                else
                {
                    currentState = GameState.WaitingForPlayer;
                    sequenceDisplayText.text = "Debug 模式 - 按數字鍵生成目標";
                }
            }
            return; // 校正中不執行其他邏輯
        }
                // 等待按 Start 開始
        if (currentState == GameState.WaitingForStart)
        {
            // 檢測任何一個 Joy-Con 的 Start 按鈕
            bool startPressed = false;
            
            
            // 或者按鍵盤 Space 以便測試
            if (Input.GetKeyDown(KeyCode.Space))
            {
                startPressed = true;
            }
            
            if (startPressed)
            {
                Debug.Log("[遊戲] Start 按鈕觸發，開始遊戲！");
                
                // 啟動視差背景移動
                if (parallaxManager != null)
                {
                    parallaxManager.StartCameraMove();
                    Debug.Log("[遊戲] 啟動 ParallaxManager 相機移動");
                }
                
                // 開始第一回合
                StartNewRound();
                return;
            }
            
            // 等待時不做其他處理
            return;
        }
        
        // Debug 模式按鍵檢測
        if (debugMode)
        {
            if (Input.GetKeyDown(debugEasy))
            {
                Debug.Log("[Debug] 按 1 生成 Easy 難度序列");
                StartNewRound("easy", bpm);
            }
            else if (Input.GetKeyDown(debugNormal))
            {
                Debug.Log("[Debug] 按 2 生成 Normal 難度序列");
                StartNewRound("normal", bpm);
            }
            else if (Input.GetKeyDown(debugHard))
            {
                Debug.Log("[Debug] 按 3 生成 Hard 難度序列");
                StartNewRound("hard", bpm);
            }
            else if (Input.GetKeyDown(debugClearKey))
            {
                Debug.Log("[Debug] 清除所有目標");
                ClearAllTargetsInternal();
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
            // ★ 檢查所有目標的飛行狀態
            for (int i = 0; i < activeTargets.Count; i++)
            {
                SlashTarget target = activeTargets[i];
                if (target != null && !target.isHit && !target.isMissed)
                {
                    float currentTime = Time.time;
                    // ★ 錯過判定：飛行時間 + OK 窗口
                    // 超過這個時間表示連 OK 都拿不到了，直接算 Miss
                    float deadlineTime = target.flyingStartTime + target.flyingDuration + okWindow;
                    
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
                        
                        // 呼叫統一的錯過處理方法（包含震動、VFX、HitStop）
                        if (target is SlashTarget3D target3D)
                        {
                            OnMissedTarget3D(target3D);
                        }
                        else
                        {
                            OnMissedTarget(target);
                        }
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
            
            // Debug 模式下不自動結束遊戲階段
            if (allProcessed && !debugMode)
            {
                OnGameplayPhaseEnd();
                Debug.Log("[遊戲結束] 所有物件已處理完畢");
            }
        }
        
        if (currentState == GameState.WaitingForPlayer)
        {
            DetectSlashInput();
        }
        
        // ★ 將指示器更新移到最後，確保在輸入處理後立即更新視覺
        if (isInGameplayPhase)
        {
            UpdateTimingIndicator();
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
    
    // 全螢幕揮動檢測（支援多個控制器）
    void DetectSlashInput()
    {
        if (activeTargets.Count == 0)
            return;
        
        // ★ WASD 快速測試（模擬完成劃動）
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
        else if (Input.GetKey(KeyCode.W)&&Input.GetKey(KeyCode.A)) // 左斜下
        {
            pressedDirection = SlashDirection.DownLeft;
            Debug.Log("[測試] 按 W 鍵 - 模擬左斜下抓");
        }
        else if (Input.GetKey(KeyCode.W)&&Input.GetKey(KeyCode.D)) // 右斜下
        {
            pressedDirection = SlashDirection.DownRight;
            Debug.Log("[測試] 按 S 鍵 - 模擬右斜下抓");
        }
        
        // 如果按了 WASD，檢查是否有匹配的飛行中物件
        if (pressedDirection.HasValue)
        {
            // ★ 暈眩時不能打擊
            if (parallaxManager != null && parallaxManager.IsDizzy)
            {
                Debug.Log("[測試] 暈眩中，無法打擊");
                return;
            }
            
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
        
        // === 檢測所有光標的劃動（多控制器支援，使用加速度檢測）===
        if (cursors == null || cursors.Length == 0)
            return;
        
        // ★ 暈眩時不能打擊
        if (parallaxManager != null && parallaxManager.IsDizzy)
        {
            return;
        }
        
        for (int cursorIndex = 0; cursorIndex < cursors.Length; cursorIndex++)
        {
            if (cursors[cursorIndex] == null)
                continue;
            
            // 確保該光標有檢測狀態
            if (!slashStates.ContainsKey(cursorIndex))
                slashStates[cursorIndex] = new SlashDetectionState();
            
            SlashDetectionState state = slashStates[cursorIndex];
            
            // 檢查冷卻時間
            if (state.isSlashOnCooldown)
                continue;
            
            Vector2 currentPos = cursors[cursorIndex].GetCursorPositionInCanvas();
            float currentTime = Time.time;

            // 記錄歷史數據（用於方向分析）
            state.positionHistory.Add(currentPos);
            state.timeHistory.Add(currentTime);

            // 清理過舊的歷史數據
            while (state.timeHistory.Count > 0 && currentTime - state.timeHistory[0] > 0.5f)
            {
                state.timeHistory.RemoveAt(0);
                state.positionHistory.RemoveAt(0);
            }

            // === 使用 Joy-Con 加速度檢測劃動觸發 ===
            test controller = null;
            if (cursorToController.ContainsKey(cursorIndex))
            {
                controller = cursorToController[cursorIndex];
            }
            
            if (controller != null)
            {
                // 取得線性加速度（已去除重力）
                Vector3 accel = controller.alinear;
                float accelMagnitude = new Vector2(accel.x, accel.z).magnitude; // 只考慮水平方向
                
                // 檢查加速度是否達到閾值（降低間隔提升響應速度）
                if (accelMagnitude >= minSlashAccel && currentTime - state.lastAccelCheckTime > 0.05f)
                {
                    state.lastAccelCheckTime = currentTime;
                    Debug.Log($"[加速度觸發] 光標 {cursorIndex}: 加速度 = {accelMagnitude:F2}, 閾值 = {minSlashAccel}");
                    AnalyzeRecentSlash(cursorIndex, currentTime);
                }
            }
            else
            {
                // 沒有控制器時回退到鼠標速度檢測（向下兼容）
                if (state.positionHistory.Count < 2)
                    continue;
                
                Vector2 lastPos = state.positionHistory[state.positionHistory.Count - 2];
                float lastTime = state.timeHistory[state.timeHistory.Count - 2];
                float deltaTime = currentTime - lastTime;
                float speed = (deltaTime > 0) ? Vector2.Distance(currentPos, lastPos) / deltaTime : 0;

                // 使用一個合理的預設速度閾值（當沒有 Joy-Con 時）
                float fallbackSpeedThreshold = 1200f;
                if (speed >= fallbackSpeedThreshold)
                {
                    AnalyzeRecentSlash(cursorIndex, currentTime);
                }
            }
        }
    }
    
    // ★ 尋找飛行中且方向匹配的下一個目標（強制按順序）
    SlashTarget FindFlyingTarget(SlashDirection direction)
    {
        float currentTime = Time.time;
        
        // ★ 找出還沒被打的、stepIndex 最小的目標
        SlashTarget nextTarget = null;
        int minStepIndex = int.MaxValue;
        
        foreach (var target in activeTargets)
        {
            if (target == null || target.isHit || target.isMissed)
                continue;
            
            if (target.stepIndex < minStepIndex)
            {
                minStepIndex = target.stepIndex;
                nextTarget = target;
            }
        }
        
        // 檢查這個目標是否符合條件（方向匹配且在飛行中）
        if (nextTarget != null &&
            nextTarget.direction == direction &&
            currentTime >= nextTarget.flyingStartTime && 
            currentTime < nextTarget.flyingStartTime + nextTarget.flyingDuration)
        {
            return nextTarget;
        }
        
        return null;
    }

    // === 新版劃動檢測核心方法 v3（多控制器版）===

    void AnalyzeRecentSlash(int cursorIndex, float peakTime)
    {
        if (!slashStates.ContainsKey(cursorIndex))
            return;
        
        SlashDetectionState state = slashStates[cursorIndex];
        
        // 1. 找到分析窗口的起點
        int startIndex = -1;
        for (int i = state.timeHistory.Count - 1; i >= 0; i--)
        {
            if (peakTime - state.timeHistory[i] <= slashAnalysisWindow)
            {
                startIndex = i;
            }
            else
            {
                break;
            }
        }

        if (startIndex == -1 || state.timeHistory.Count - startIndex < 2)
        {
            Debug.Log($"[Slash v3][控制器{cursorIndex}] ✗ 分析失敗：分析窗口內數據不足");
            return;
        }

        // 2. 獲取窗口內的起點和終點
        Vector2 startPos = state.positionHistory[startIndex];
        Vector2 endPos = state.positionHistory[state.positionHistory.Count - 1];
        float slashTime = state.timeHistory[state.timeHistory.Count - 1] - state.timeHistory[startIndex];
        float slashDist = Vector2.Distance(startPos, endPos);

        // 3. 檢查距離
        if (slashDist < minSlashDistance)
        {
            Debug.Log($"[Slash v3][控制器{cursorIndex}] ✗ 分析失敗：距離太短 ({slashDist:F1} < {minSlashDistance})");
            return;
        }

        // 4. 判斷方向
        Vector2 slashVector = (endPos - startPos).normalized;
        SlashDirection dir = DetectDirection(slashVector);

        // if (dir == (SlashDirection)(-1))
        // {
        //     Debug.Log($"[Slash v3][控制器{cursorIndex}] ✗ 分析失敗：方向不明確");
        //     return;
        // }

        // 5. 尋找目標
        SlashTarget target = FindFlyingTarget(dir);
        if (target == null)
        {
            Debug.Log($"[Slash v3][控制器{cursorIndex}] ✓ 分析成功，但無匹配目標。方向={dir}");
            return;
        }

        // 6. 成功！
        Debug.Log($"[Slash v3][控制器{cursorIndex}] ✓✓✓ 成功！方向={dir}, 距離={slashDist:F1}, 時間={slashTime:F2}s");
        // 反轉控制器索引（cursorIndex 0 -> controllerIndex 1, cursorIndex 1 -> controllerIndex 0）
        int controllerIndexForVibration = (cursorIndex == 0) ? 0 : 1;
        OnSlashComplete(target, slashDist, slashTime, controllerIndexForVibration);
        StartCoroutine(SlashCooldown(cursorIndex));
    }
    
    IEnumerator SlashCooldown(int cursorIndex)
    {
        if (!slashStates.ContainsKey(cursorIndex))
            yield break;
        
        SlashDetectionState state = slashStates[cursorIndex];
        state.isSlashOnCooldown = true;
        
        // ★ 不清空歷史記錄，讓數據繼續累積
        // 舊數據會在 Update 中自動清理（超過 0.5s 的記錄）
        
        yield return new WaitForSeconds(slashCooldown);
        state.isSlashOnCooldown = false;
    }

    // 方向檢測 (v3 版 - 改為左右+斜下)
    SlashDirection DetectDirection(Vector2 vector)
    {
        if (vector.magnitude < 0.1f) return (SlashDirection)(-1);
        
        Vector2 normalized = vector.normalized;
        
        // 定義四個方向向量
        Vector2 right = Vector2.right;                          // 右 (1, 0)
        Vector2 left = Vector2.left;                            // 左 (-1, 0)
        Vector2 downLeft = new Vector2(-1, -1).normalized;      // 左斜下 (-0.707, -0.707)
        Vector2 downRight = new Vector2(1, -1).normalized;      // 右斜下 (0.707, -0.707)
        
        // 計算與各方向的相似度
        float dotRight = Vector2.Dot(normalized, right);
        float dotLeft = Vector2.Dot(normalized, left);
        float dotDownLeft = Vector2.Dot(normalized, downLeft);
        float dotDownRight = Vector2.Dot(normalized, downRight);
        
        // 找出最匹配的方向
        float maxDot = Mathf.Max(dotRight, dotLeft, dotDownLeft, dotDownRight);
        
        // 必須超過閾值才算有效
        if (maxDot < minDirectionDot)
            return (SlashDirection)(-1);
        
        // 返回最匹配的方向
        if (maxDot == dotRight) return SlashDirection.Right;
        if (maxDot == dotLeft) return SlashDirection.Left;
        if (maxDot == dotDownLeft) return SlashDirection.DownLeft;
        if (maxDot == dotDownRight) return SlashDirection.DownRight;
        
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
            case SlashDirection.DownLeft: return new Vector2(-1, -1).normalized;
            case SlashDirection.DownRight: return new Vector2(1, -1).normalized;
            default: return Vector2.zero;
        }
    }
    
    private (string difficulty, float bpm, float elapsedTime) EvaluateTimeBasedDifficulty()
    {
        if (gameSessionStartTime < 0)
        {
            gameSessionStartTime = Time.time;
        }

        float elapsedTime = Time.time - gameSessionStartTime;

        if (elapsedTime < 10f)
        {
            return ("easy", 120f, elapsedTime);
        }
        else if (elapsedTime < 20f)
        {
            return ("normal", 140f, elapsedTime);
        }

        return ("hard", 160f, elapsedTime);
    }

    // 開始新一輪
    void StartNewRound(string overrideDifficulty = null, float overrideBpm = -1f)
    {
        Debug.Log("=== 開始新一輪 ===");
        
        string difficulty = "easy";
        float elapsedTime = 0f;
        float targetBpm = bpm;
        
        if (overrideDifficulty != null && overrideBpm > 0)
        {
            difficulty = overrideDifficulty;
            targetBpm = overrideBpm;
            Debug.Log($"[Debug] 使用指定難度: {difficulty}, BPM: {targetBpm}");
        }
        else if (debugMode)
        {
            Debug.Log("Debug 模式：需要傳入難度參數");
            return;
        }
        else
        {
            var info = EvaluateTimeBasedDifficulty();
            difficulty = info.difficulty;
            targetBpm = info.bpm;
            elapsedTime = info.elapsedTime;
        }

        bpm = targetBpm;
        beatInterval = 60f / bpm;

        currentStep = 0;
        currentSequence.Clear();
        activeTargets.Clear();
        currentBeatIndex = 0;
        isInGameplayPhase = false;
        
        // ★ 重置擊打記錄
        lastPlayerHitTime = -1f;
        hitCount = 0;
        lastSlashTime = -1f; // 重置揮動冷卻
        
        // ★ 使用 RhythmGenerator 生成節奏模式
        // 假設每回合固定 4 拍
        float totalBeats = 4f; 
        currentPattern = rhythmGenerator.GeneratePattern(totalBeats, difficulty);
        currentPatternIndex = 0;
        currentSequenceIndex = 0;

        // 為非休止符的音符生成方向
        foreach (var note in currentPattern) {
            if (!note.ToString().StartsWith("Rest")) {
                SlashDirection dir = (SlashDirection)Random.Range(0, 4);
                currentSequence.Add(dir);
            }
        }
        
        // ★ 計算模式總持續時間，並設置飛行時間
        float totalPatternDuration = 0f;
        foreach (var note in currentPattern)
        {
            totalPatternDuration += NoteValue.GetBeats(note) * (60f / bpm);
        }
        // 第一個目標要在提示階段結束後 1 秒到達
        // 提示階段總長 = totalPatternDuration
        // 飛行時間 = totalPatternDuration + 1.0f
        flyingDuration = totalPatternDuration + 1.0f;
        Debug.Log($"[飛行時間] 設置為 {flyingDuration:F2}s (模式長度 {totalPatternDuration:F2}s + 1.0s)");
        
        // 設置第一個節拍時間
        nextBeatTime = Time.time + beatOffset + beatInterval;
        currentState = GameState.ShowSequence;
        currentStepIndex = 0;
        
        sequenceDisplayText.text = "記住這個順序...";
        Debug.Log($"[新回合] 時間={elapsedTime:F1}s, BPM={bpm}, 難度={difficulty}, 模式長度={currentPattern.Count}");
    }
    
    // 在節拍上生成下一個目標（提示階段）
    void SpawnNextBeatTarget()
    {
        // ★ 檢查時間是否到期，如果時間 <= 0 就不再生成
        if (timeUIController != null && timeUIController.RemainingTime <= 0)
        {
            Debug.Log("[提示階段] 時間已到，停止生成目標");
            currentState = GameState.Idle;
            sequenceDisplayText.text = "時間到！";
            return;
        }
        
        if (currentPatternIndex >= currentPattern.Count)
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

        NoteType currentNote = currentPattern[currentPatternIndex];
        float beatValue = NoteValue.GetBeats(currentNote);
        float duration = beatValue * (60f / bpm);
        
        // 如果是休止符，只更新時間，不生成目標
        if (currentNote.ToString().StartsWith("Rest"))
        {
            Debug.Log($"[提示階段] 休止符: {currentNote}, 持續時間={duration:F2}s");
            nextBeatTime += duration;
            currentPatternIndex++;
            return;
        }
        
        // 確保還有方向可用
        if (currentSequenceIndex >= currentSequence.Count)
        {
            Debug.LogError("方向序列不足！");
            currentPatternIndex++;
            return;
        }

        SlashDirection dir = currentSequence[currentSequenceIndex];
        
        // 播放提示音效
        if (beatSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(beatSound);
        }
        
        // ★ 3D 模式：在世界空間生成
        Transform parent3D = targets3DParent != null ? targets3DParent : null;
        GameObject targetObj = Instantiate(slashTargetPrefab, parent3D);
        
        // 設置初始位置
        Vector3 offset = Vector3.zero;
        if (spawnPoint != null)
        {
            Vector3 position = spawnPoint.position;
            
            // 加上隨機 X 偏移，避免生成在一條直線上
            float worldOffsetX = targetXOffsetRange / 100f; // 將 Canvas 單位轉換為世界單位 (可調整)
            float randomX = Random.Range(-worldOffsetX, worldOffsetX);
            offset.x = randomX;
            position.x += randomX;
            
            targetObj.transform.position = position;
        }
        
        SlashTarget3D target3D = targetObj.GetComponent<SlashTarget3D>();
        if (target3D == null)
            target3D = targetObj.AddComponent<SlashTarget3D>();
        
        target3D.direction = dir;
        target3D.stepIndex = currentSequenceIndex; // 使用 SequenceIndex
        target3D.spawnTime = Time.time;
        target3D.spawnPoint = spawnPoint;
        target3D.targetPoint = targetPoint;
        target3D.targetOffset = offset; // 設置目標點偏移，保持平行飛行
        target3D.arcHeight = arcHeight;
        
        // ★ 使用節奏生成的間隔
        target3D.customInterval = duration;
        
        target3D.flyingStartTime = Time.time;
        target3D.flyingDuration = flyingDuration;
        
        // ★ 提示階段生成的目標不播放音效，直接標記為已播放
        target3D.hasPlayedJudgmentBeat = true;
        
        Debug.Log($"[提示階段 3D] 目標 #{currentSequenceIndex} ({dir}): 音符={currentNote}, 間隔={duration:F2}s");
        
        // 預製體已經包含方向圖片，不需要設置文字
        target3D.Initialize();
        
        // 加入目標列表
        activeTargets.Add(target3D);
        
        currentSequenceIndex++;
        currentPatternIndex++;
        nextBeatTime += duration;
    }
    
    // 開始遊戲階段
    IEnumerator StartGameplayPhase()
    {
        currentState = GameState.Idle; // 暫停狀態
        
        // ★ 移除等待時間，讓指示器能完整顯示 1 秒的縮小過程
        // yield return new WaitForSeconds(beatInterval * 0.5f);
        yield return null;
        
        sequenceDisplayText.text = "開始！跟著節拍揮動！";
        
        // 記錄遊戲階段開始時間
        gameplayStartTime = Time.time;
        
        // ★ 飛行已經在提示階段開始了，這裡只需記錄開始時間
        Debug.Log($"[遊戲階段開始] 開始時間={gameplayStartTime:F2}，物件已在飛行中");
        
        // ★ 初始化節拍指示器
        InitializeTimingIndicator();
        
        // ★ 立即為第一個目標啟動指示器
        if (activeTargets.Count > 0)
        {
            SlashTarget firstTarget = activeTargets[0];
            
            // ★ 重設基準時間為現在（遊戲階段真正開始的時間）
            // 這樣指示器就會從 Scale 4 開始縮小
            lastPlayerHitTime = Time.time;
            
            // ★ 重設第一個目標的間隔為「剩餘飛行時間」
            // 這樣判定邏輯也會變成 (擊打時間 - 現在) vs (剩餘時間)
            float remainingTime = (firstTarget.flyingStartTime + firstTarget.flyingDuration) - Time.time;
            firstTarget.customInterval = remainingTime;
            
            ActivateTimingIndicator(firstTarget.customInterval);
            Debug.Log($"[指示器] 為第一個目標啟動，剩餘時間={remainingTime:F2}s");
        }
        
        isInGameplayPhase = true;
        currentState = GameState.WaitingForPlayer;
    }
    
    // 遊戲階段結束
    void OnGameplayPhaseEnd()
    {
        isInGameplayPhase = false;
        currentState = GameState.Checking;
        
        // ★ 停止節拍指示器
        StopTimingIndicator();
        
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
        
        // ★ 移除難度提升邏輯，改由 StartNewRound 根據時間控制
        // currentLevel++;
        // bpm += 10f; 
        // beatInterval = 60f / bpm; 
        // speedMultiplier += 0.1f;
        
        Debug.Log($"[下一輪準備] 當前分數: {score}");
        
        // 可以在這裡開始下一輪或返回主選單
        Debug.Log("清除目標並開始新一輪");
        ClearAllTargetsInternal();
        StartNewRound();
    }
    
    // 劃動完成（全螢幕檢測）
    void OnSlashComplete(SlashTarget target, float slashDistance, float slashTime, int controllerIndex = -1)
    {
        Debug.Log($"[遊戲] 劃動完成：目標#{target.stepIndex}, 方向={target.direction}, 距離={slashDistance:F1}, 時間={slashTime:F2}s, 控制器={controllerIndex}");
        
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
        // 因為現在第一個物件也會設置 lastPlayerHitTime，所以統一使用間隔判定
        if (lastPlayerHitTime < 0f && hitCount > 1) // 防呆：如果不是第一個且時間未設置
        {
            // 異常情況，照舊處理
            rating = "OK";
            points = 50;
            Debug.Log($"[遊戲] 異常：擊中時間={currentTime:F2}，但 lastPlayerHitTime 未設置");
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
                
                // 觸發 Perfect 震動（僅震動擊中的控制器）
                if (vibrationManager != null)
                {
                    Debug.Log($"[震動] 觸發 Perfect Hit 震動 - 控制器 {controllerIndex}");
                    vibrationManager.VibrateOnPerfect(controllerIndex);
                }
                else
                {
                    Debug.LogWarning("[震動] vibrationManager 為 null，無法震動");
                }
            }
            else if (timingOffset <= goodWindow)
            {
                rating = "Good!";
                points = 200;
                
                // 觸發 Good 震動（僅震動擊中的控制器）
                if (vibrationManager != null)
                {
                    Debug.Log($"[震動] 觸發 Good Hit 震動 - 控制器 {controllerIndex}");
                    vibrationManager.VibrateOnGood(controllerIndex);
                }
            }
            else if (timingOffset <= okWindow)
            {
                rating = "OK";
                points = 100;
                
                // 觸發 OK 震動（僅震動擊中的控制器）
                if (vibrationManager != null)
                {
                    Debug.Log($"[震動] 觸發 OK Hit 震動 - 控制器 {controllerIndex}");
                    vibrationManager.VibrateOnOK(controllerIndex);
                }
            }
            else
            {
                // 超出 OK 窗口，視為 Miss
                rating = "Miss";
                points = 0;
                combo = 0; // 重置 Combo
                
                // 觸發 Miss 震動
                if (vibrationManager != null)
                {
                    vibrationManager.VibrateOnMiss();
                }
            }
            
            Debug.Log($"[遊戲] 間隔判定：玩家間隔={playerInterval:F3}s, 理想間隔={idealInterval:F3}s, 差距={timingOffset:F3}s");
        }
        
        // 更新上次擊打時間
        lastPlayerHitTime = currentTime;
        
        // ★ 更新指示器
        // 因為現在第一個目標已經啟動了指示器，所以這裡只需要更新到下一個目標
        if (isIndicatorActive)
        {
            // 後續擊打，更新指示器目標
            SlashTarget nextTarget = FindNextActiveTarget(target);
            if (nextTarget != null)
            {
                float nextInterval = nextTarget.customInterval > 0f ? nextTarget.customInterval : beatInterval;
                UpdateIndicatorTarget(nextInterval);
            }
            else
            {
                // 沒有下一個目標了，停止指示器
                StopTimingIndicator();
            }
        }
        else if (hitCount == 1) // 防呆：如果指示器意外沒啟動
        {
            SlashTarget nextTarget = FindNextActiveTarget(target);
            if (nextTarget != null)
            {
                float nextInterval = nextTarget.customInterval > 0f ? nextTarget.customInterval : beatInterval;
                ActivateTimingIndicator(nextInterval);
            }
            else
            {
                // 沒有下一個目標了
                StopTimingIndicator();
            }
        }
        
        // 播放音效
        if (soundToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
        
        // 劃動成功！
        target.MarkAsCompleted();
        
        // 如果是 Miss，不增加 Combo
        if (rating != "Miss")
        {
            combo++;
            score += points * combo;
        }
        
        ShowFeedback($"{rating} " + (rating != "Miss" ? $"x{combo}" : ""));
        UpdateUI();
        
        // ★ 播放劃痕 VFX（根據劃動方向）
        if (vfxManager != null)
        {
            Vector3 vfxPosition = target.GetPosition();
            Debug.Log($"[遊戲] 準備播放 VFX：方向={target.direction}, 位置={vfxPosition}");
            
            // 根據目標的方向生成對應的劃痕 VFX
            vfxManager.SpawnSlashVFX(target.direction, vfxPosition);
            
            // 每 10 連擊播放特殊特效
            vfxManager.SpawnComboVFX(combo, vfxPosition);
        }
        else
        {
            Debug.LogWarning("[遊戲] VFX Manager 為 null！請在 Inspector 中設置 VFX Manager。");
        }
        
        // 重置所有光標到 (0, 0)
        if (cursors != null)
        {
            foreach (var cursor in cursors)
            {
                if (cursor != null)
                {
                    // cursor.ResetToOrigin();
                }
            }
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
        
        // ★ 播放錯過 VFX
        if (vfxManager != null)
        {
            vfxManager.SpawnMissVFX(target.GetPosition());
        }
        
        // 觸發 Miss 震動
        if (vibrationManager != null)
        {
            vibrationManager.VibrateOnMiss();
        }

        // 觸發被打到時的停止效果
        if (parallaxManager != null)
        {
            parallaxManager.TriggerHitStop();
        }
        
        Debug.Log($"[Miss] 目標 #{target.stepIndex} ({target.direction}) 錯過，扣 {penalty} 分，Combo 重置");
    }
    
    // 找到下一個未完成的目標
    SlashTarget FindNextActiveTarget(SlashTarget currentTarget)
    {
        int currentIndex = activeTargets.IndexOf(currentTarget);
        if (currentIndex < 0)
            return null;
        
        // 從當前目標的下一個開始找
        for (int i = currentIndex + 1; i < activeTargets.Count; i++)
        {
            SlashTarget target = activeTargets[i];
            if (target != null && !target.isHit && !target.isMissed)
            {
                return target;
            }
        }
        
        return null;
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
        
        // ★ 播放錯過 VFX
        if (vfxManager != null)
        {
            vfxManager.SpawnMissVFX(target.GetPosition());
        }
        
        // 觸發 Miss 震動
        if (vibrationManager != null)
        {
            vibrationManager.VibrateOnMiss();
        }

        // 觸發被打到時的停止效果
        if (parallaxManager != null)
        {
            parallaxManager.TriggerHitStop();
        }
        
        // ★ Miss 時也要更新時間基準，確保下一個目標的相對時間正確
        // 假設玩家在 Perfect 時間點擊打了（雖然沒打到），這樣下一個目標的間隔計算才正確
        lastPlayerHitTime = target.flyingStartTime + target.flyingDuration;
        
        // ★ 如果是第一個目標 Miss，也要啟動指示器
        if (!isIndicatorActive)
        {
             SlashTarget nextTarget = FindNextActiveTarget(target);
             if (nextTarget != null)
             {
                 float nextInterval = nextTarget.customInterval > 0f ? nextTarget.customInterval : beatInterval;
                 ActivateTimingIndicator(nextInterval);
             }
             else
             {
                 // 沒有下一個目標了
                 StopTimingIndicator();
             }
        }
        else
        {
             // 更新指示器到下一個目標
             SlashTarget nextTarget = FindNextActiveTarget(target);
             if (nextTarget != null)
             {
                 float nextInterval = nextTarget.customInterval > 0f ? nextTarget.customInterval : beatInterval;
                 UpdateIndicatorTarget(nextInterval);
             }
             else
             {
                 // 沒有下一個目標了，停止指示器
                 StopTimingIndicator();
             }
        }
        
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
            scoreText.text = score.ToString();
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
            // 根據已連接的手把數量顯示不同提示
            int connectedCount = GetConnectedControllerCount();
            List<string> uncalibratedNames = GetUncalibratedControllerNames();
            
            if (uncalibratedNames.Count > 0)
            {
                calibrationText.text = $"請按 B 鍵校正以下手把:\n{string.Join(", ", uncalibratedNames)}";
            }
            else
            {
                calibrationText.text = "請按 B 鍵進行 Joy-Con 校正";
            }
            
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
    
    /// <summary>
    /// 檢查所有已連接的手把是否都已校正
    /// </summary>
    bool AreAllConnectedControllersCalibrated()
    {
        bool allCalibrated = true;
        
        // 檢查第一個手把（如果已連接）
        if (IsControllerConnected(joyConController))
        {
            if (!joyConController.Calibrated)
            {
                Debug.Log($"手把 1 ({joyConController.name}) 尚未校正");
                allCalibrated = false;
            }
        }
        
        // 檢查第二個手把（如果已連接）
        if (IsControllerConnected(joyConController2))
        {
            if (!joyConController2.Calibrated)
            {
                Debug.Log($"手把 2 ({joyConController2.name}) 尚未校正");
                allCalibrated = false;
            }
        }
        
        return allCalibrated;
    }
    
    /// <summary>
    /// 檢查控制器是否已連接
    /// </summary>
    bool IsControllerConnected(test controller)
    {
        if (controller == null)
            return false;
        
        // 透過 controllerManager 檢查該控制器索引是否有連接
        if (controllerManager != null)
        {
            return controllerManager.HasController(controller.controllerIndex);
        }
        
        // 如果沒有 manager，假設只要 controller 存在就是連接的
        return true;
    }
    
    /// <summary>
    /// 取得已連接的手把數量
    /// </summary>
    int GetConnectedControllerCount()
    {
        int count = 0;
        if (IsControllerConnected(joyConController)) count++;
        if (IsControllerConnected(joyConController2)) count++;
        return count;
    }
    
    /// <summary>
    /// 取得未校正的手把名稱列表
    /// </summary>
    List<string> GetUncalibratedControllerNames()
    {
        List<string> names = new List<string>();
        
        if (IsControllerConnected(joyConController) && !joyConController.Calibrated)
        {
            names.Add($"手把1 (索引 {joyConController.controllerIndex})");
        }
        
        if (IsControllerConnected(joyConController2) && !joyConController2.Calibrated)
        {
            names.Add($"手把2 (索引 {joyConController2.controllerIndex})");
        }
        
        return names;
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
            case SlashDirection.DownLeft: return "↙";
            case SlashDirection.DownRight: return "↘";
            default: return "?";
        }
    }
    
    // === Debug 模式方法 ===
    
    // 生成單個 Debug 目標
    void SpawnDebugTarget(SlashDirection direction)
    {
        // 確保在遊戲階段，這樣 Update 才會處理飛行和判定
        isInGameplayPhase = true;
        
        // 3D 模式：在世界空間生成
        Transform parent3D = targets3DParent != null ? targets3DParent : null;
        GameObject targetObj = Instantiate(slashTargetPrefab, parent3D);
        
        Vector3 offset = Vector3.zero;
        if (spawnPoint != null)
        {
            Vector3 position = spawnPoint.position;
            
            // 加上隨機 X 偏移，避免生成在一條直線上
            float worldOffsetX = targetXOffsetRange / 100f;
            float randomX = Random.Range(-worldOffsetX, worldOffsetX);
            offset.x = randomX;
            position.x += randomX;
            
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
        target3D.targetOffset = offset;
        target3D.customInterval = beatInterval;
        target3D.flyingStartTime = Time.time;
        target3D.flyingDuration = flyingDuration;
        target3D.arcHeight = arcHeight;
        target3D.Initialize();
        
        activeTargets.Add(target3D);
        
        Debug.Log($"[Debug 3D] 已生成目標 #{activeTargets.Count}: {direction}");
    }
    
    // 清除所有目標
    void ClearAllTargetsInternal()
    {
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
    
    // === 節拍指示器方法 ===
    
    // 初始化時機指示器
    void InitializeTimingIndicator()
    {
        // 內圈一直開著
        if (perfectCircle != null)
        {
            perfectCircle.transform.localScale = Vector3.one; // Perfect 圓圈固定縮放 = 1
            // perfectCircle.color = Color.green;
            perfectCircle.gameObject.SetActive(true);
        }
        
        // 外圈先關閉，等第一次擊打後才開啟
        if (timingCircle != null)
        {
            timingCircle.transform.localScale = Vector3.one * 4f;
            timingCircle.color = Color.red;
            timingCircle.gameObject.SetActive(false); // 先關閉
        }
        
        isIndicatorActive = false; // 還未開始
        currentHitIndex = 0;
        
        Debug.Log($"[指示器] 初始化完成，等待第一次擊打");
    }
    
    // 更新時機指示器（在 Update 中呼叫）
    void UpdateTimingIndicator()
    {
        if (!isIndicatorActive || timingCircle == null)
            return;
        
        float currentTime = Time.time;
        
        // 計算距離下一次預期擊打的時間
        float timeUntilNext = nextExpectedHitTime - currentTime;
        float timeSinceLast = currentTime - lastPlayerHitTime;
        
        // 計算進度（0 = 剛打完上一個, 1 = 應該打下一個）
        float progress = timeSinceLast / currentTargetInterval;
        
        // ★ 從最大縮放 4 縮小到最小縬放 0.95
        float targetScale = Mathf.Lerp(4f, 0.95f, progress);
        timingCircle.transform.localScale = Vector3.one * targetScale;
        
        // ★ 根據距離 Perfect 時機的時間差來設置顏色
        float timeToPerfect = Mathf.Abs(timeUntilNext);
        
        if (timeToPerfect <= perfectWindow) // <= 0.05s
        {
            timingCircle.color = Color.green; // Perfect: 綠色
        }
        else if (timeToPerfect <= goodWindow) // <= 0.1s
        {
            timingCircle.color = Color.yellow; // Good: 黃色
        }
        else if (timeToPerfect <= okWindow) // <= 0.3s
        {
            timingCircle.color = new Color(1f, 0.5f, 0f); // OK: 橙色
        }
        else // > 0.3s
        {
            timingCircle.color = Color.red; // Miss: 紅色
        }
    }
    
    // 啟動指示器（第一次擊打後呼叫）
    void ActivateTimingIndicator(float interval)
    {
        if (timingCircle != null)
        {
            timingCircle.gameObject.SetActive(true);
            timingCircle.transform.localScale = Vector3.one * 4f;
            timingCircle.color = Color.red;
        }
        
        currentTargetInterval = interval;
        nextExpectedHitTime = lastPlayerHitTime + interval;
        isIndicatorActive = true;
        currentHitIndex = 1;
        
        Debug.Log($"[指示器] 啟動，間隔={interval:F3}s, 下次預期={nextExpectedHitTime:F2}");
    }
    
    // 更新指示器目標（每次擊打後呼叫）
    void UpdateIndicatorTarget(float interval)
    {
        if (!isIndicatorActive)
            return;
        
        currentTargetInterval = interval;
        nextExpectedHitTime = lastPlayerHitTime + interval;
        currentHitIndex++;
        
        // 重置圓圈
        if (timingCircle != null)
        {
            timingCircle.transform.localScale = Vector3.one * 4f;
            timingCircle.color = Color.red;
        }
        
        Debug.Log($"[指示器] 更新目標 #{currentHitIndex}, 間隔={interval:F3}s, 下次預期={nextExpectedHitTime:F2}");
    }
    
    // 停止時機指示器
    void StopTimingIndicator()
    {
        isIndicatorActive = false;
        
        // 內圈不關閉，一直保持顯示
        // if (perfectCircle != null)
        //     perfectCircle.gameObject.SetActive(false);
        
        // 只關閉外圈
        if (timingCircle != null)
            timingCircle.gameObject.SetActive(false);
        
        Debug.Log("[指示器] 已停止（內圈保持顯示）");
    }
}
