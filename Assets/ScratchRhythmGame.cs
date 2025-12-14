using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

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
    public SceneController sceneController;
    
    [Header("劃痕檢測設置 - v3")]
    public float minSlashAccel = 1.5f;         // 觸發劃動的最低加速度閾值（使用 Joy-Con 加速度計）
    public float minSlashDistance = 100f;      // 有效劃動的最短直線距離
    public float slashAnalysisWindow = 0.15f;  // 速度達標後，回溯分析的時間窗口（秒）
    public float minDirectionDot = 0.8f;       // 方向判斷的餘弦閾值（0.8 ≈ 37度內）
    public float targetMinDistance = 150f;     // 目標之間的最小生成距離
    public float targetXOffsetRange = 100f;    // 上下目標的X軸隨機偏移範圍
    
    [Header("方向區域設置")]
    public float directionZoneWidth = 2f;      // ★ 每個方向區域的寬度（世界單位）
    
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
    public bool isTutorialMode = false; // ★ 是否為教學模式（不自動循環）
    public bool isSingleNoteTutorial = false; // ★ 是否為單音符教學（只顯示Perfect，只能暫停時揮動）
    public KeyCode debugEasy = KeyCode.Alpha1; // 按 1 生成左
    public KeyCode debugNormal = KeyCode.Alpha2; // 按 2 生成右
    public KeyCode debugHard = KeyCode.Alpha3; // 按 3 生成上
    public KeyCode debugClearKey = KeyCode.C; // 按 C 清除所有目標
    
    [Header("UI 引用")]
    public GameObject slashTargetPrefab; // 3D 目標預製體
    
    [System.Serializable]
    public struct ProjectileTypeConfig
    {
        public string name;
        public AudioClip[] hitSounds; // ★ 該類型的多個擊中音效，打中時隨機挑一個
        [Header("方向圖片")]
        public Sprite leftSprite;
        public Sprite rightSprite;
        public Sprite downLeftSprite;
        public Sprite downRightSprite;
    }

    [Header("投擲物類型設置")]
    public ProjectileTypeConfig[] projectileTypes; // 可拓展的投擲物類型列表（音效和圖片組合）
    
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
    public enum GameState { WaitingCalibration, WaitingForStart, IntroAnimation, SkillCutscene, Idle, ShowSequence, WaitingForPlayer, Checking, Tutorial }
    public GameState currentState;

    // ★ 新增事件：回合結束通知
    public event System.Action OnRoundComplete;
    
    [Header("開場鏡頭動畫設置 - Cinemachine")]
    public Cinemachine.CinemachineVirtualCamera mainVirtualCamera; // 主虛擬相機（遊戲全程使用）
    private Cinemachine.CinemachineFollowZoom followZoom; // Follow Zoom 組件引用
    public GameObject introAnimationObject; // 開場小動畫物件（可選，例如角色特寫、Logo 等）
    public Animator introAnimationAnimator; // 開場動畫的 Animator（可選，用於觸發動畫）
    public string introAnimationTrigger = "Play"; // 動畫觸發器名稱
    public float introZoomInDuration = 2.0f; // 縮放進入時間
    public float introMinFOV = 40f; // 開場時的最小 FOV（放大效果）
    public float introAnimationDuration = 3.0f; // 小動畫播放時間
    public float introZoomOutDuration = 1.5f; // 縮放退出時間
    public float normalMinFOV = 90f; // 正常遊戲時的 FOV
    
    [Header("隨機動畫設置（當無 Animator 時）")]
    public float randomMoveDistance = 3f; // 隨機移動距離（像素/單位）
    public float randomMoveSpeed = 2f; // 隨機移動速度
    public bool keepAnimationPlaying = false; // 動畫持續播放（遊戲開始後也不停止）
    
    private bool isIntroAnimationActive = false;
    private List<Coroutine> childAnimationCoroutines = new List<Coroutine>();
    private Dictionary<Transform, Vector3> childOriginalPositions = new Dictionary<Transform, Vector3>();
    
    [Header("技能演出設置")]
    public Image skillCharacterImageLeft; // 左側技能立繪
    public Image skillCharacterImageRight; // 右側技能立繪
    public Sprite[] skillCharacterSprites; // 技能立繪的 Sprite 陣列（技能演出時驚橚一個）
    public Image screenDarkOverlay; // 暗黑效果的 Panel
    public Canvas gameUICanvas; // ★ 遊戲 UI Canvas（在 Cutscene 時改為 Space Screen，結束後改為 Overlay）
    public float skillCutsceneDuration = 3f; // 技能演出續時間
    public float skillSlideInDuration = 0.5f; // 立繪滑入時間
    
    [Header("方向提示設置")]
    public Image directionIndicatorImage; // ★ 方向提示 Image（通過旋轉改變方向）
    
    // 技能演出相關變數
    private float skillCutsceneStartTime = -1f;
    private RenderMode originalCanvasRenderMode; // ★ 儲存原始的 Canvas Render Mode
    private bool isSkillCutsceneActive = false;
    private bool isGameEnding = false; // ★ 防止重複觸發 GameEnded
    
    // 暫存下一輪的設定
    private string pendingDifficulty = null;
    private float pendingBpm = -1f;
    
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
    
    // ★ 記分板系統
    private scoreingame scoreboard;
    
    // ★ 暫存每個 target 的音效（防止 destroy 時音效被中斷）
    private Dictionary<SlashTarget, AudioClip> targetHitSounds = new Dictionary<SlashTarget, AudioClip>();
    
    // ★ 間隔判定
    private float lastPlayerHitTime = -1f; // 上一次玩家擊打時間（-1表示還沒打過）
    private int hitCount = 0; // 已擊中物件數量
    private float lastSlashTime = -1f; // 上一次揮動完成時間（防止連續觸發）
    private float slashCooldown = 0.2f; // 揮動冷卻時間（秒）
    
    // 方向枚舉（大野狼抓取動作）
    public enum SlashDirection { Left, Right, DownLeft, DownRight }
    public bool startPressed = false;
    
    public void OnEnable()
    {
        // ★ 初始化技能演出UI狀態（隱藏）
        if (skillCharacterImageLeft != null) skillCharacterImageLeft.gameObject.SetActive(false);
        if (skillCharacterImageRight != null) skillCharacterImageRight.gameObject.SetActive(false);
        if (screenDarkOverlay != null) screenDarkOverlay.gameObject.SetActive(false);
        
        // ★ 初始化方向提示Image（隱藏）
        if (directionIndicatorImage != null) directionIndicatorImage.gameObject.SetActive(false);

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
        // ★ 開場鏡頭動畫處理
        if (currentState == GameState.IntroAnimation)
        {
            // 動畫由 Coroutine 控制，這裡不做處理
            return;
        }
        
        // ★ 技能演出處理
        if (currentState == GameState.SkillCutscene)
        {
            UpdateSkillCutscene();
            return;
        }
        
        // ★ 校正階段檢查
        if (currentState == GameState.WaitingCalibration)
        {
            if (AreAllConnectedControllersCalibrated())
            {
                HideCalibrationPrompt();
                Debug.Log("所有已連接的手把校正完成！等待 Start 按鈕...");
                if(debugMode) currentState = GameState.WaitingForPlayer;
                else currentState = GameState.WaitingForStart;
            }
            return; // 校正中不執行其他邏輯
        }
        
        // 等待按 Start 開始
        if (currentState == GameState.WaitingForStart)
        {
            // 檢測任何一個 Joy-Con 的 Start 按鈕

            
            // ★ 直接檢查所有 SwitchControllerHID 設備
            foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
            {
                if (device is UnityEngine.InputSystem.Switch.SwitchControllerHID switchController)
                {
                    // 檢查 East 按鈕（B 鍵）或 Left D-Pad
                    if (switchController.buttonEast.wasPressedThisFrame || 
                        switchController.dpad.left.wasPressedThisFrame)
                    {
                        startPressed = true;
                        Debug.Log($"[遊戲] 控制器按下按鈕，開始遊戲！");
                        break;
                    }
                }
            }
            
            // 或者按鍵盤 Space 以便測試
            if (Input.GetKeyDown(KeyCode.Space))
            {
                startPressed = true;
                Debug.Log("[遊戲] Space 鍵觸發，開始遊戲！");
            }
            
            if (startPressed)
            {
                Debug.Log("[遊戲] Start 按鈕觸發，開始開場鏡頭動畫！");
                
                // ★ 先進入開場鏡頭動畫階段
                StartIntroAnimation();
                return;
            }
            
            // 等待時不做其他處理
            return;
        }
        
        // Debug 模式按鍵檢測
        if (debugMode)
        {
            // ★ 按 S 測試技能演出
            if (Input.GetKeyDown(KeyCode.S))
            {
                Debug.Log("[Debug] 按 S 測試技能演出");
                StartSkillCutscene();
                return;
            }
            
            if (Input.GetKeyDown(debugEasy))
            {
                Debug.Log("[Debug] 按 1 生成 Easy 難度序列");
                StartSkillCutscene("easy", bpm);
            }
            else if (Input.GetKeyDown(debugNormal))
            {
                Debug.Log("[Debug] 按 2 生成 Normal 難度序列");
                StartSkillCutscene("normal", bpm);
            }
            else if (Input.GetKeyDown(debugHard))
            {
                Debug.Log("[Debug] 按 3 生成 Hard 難度序列");
                StartSkillCutscene("hard", bpm);
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
        if ((isInGameplayPhase || isTutorialMode) && activeTargets.Count > 0)
        {
            // ★ 檢查所有目標的飛行狀態
            for (int i = 0; i < activeTargets.Count; i++)
            {
                SlashTarget target = activeTargets[i];
                if (target != null && !target.isHit && !target.isMissed)
                {
                    float currentTime = Time.time;
                    // ★ 錯過判定：飛行到達時間（視覺上已經到達鏡頭前方）
                    // 投擲物到達目標點的時間
                    float arrivalTime = target.flyingStartTime + target.flyingDuration;
                    
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
                    
                    // ★ 檢查是否錯過（投擲物已經在視覺上到達目標點）
                    // 使用更小的容錯時間，讓視覺與判定同步
                    float missDeadline = arrivalTime + 0.1f; // 只給 0.05 秒容錯
                    if (currentTime >= missDeadline)
                    {
                        if (target.isPaused)
                        {
                            target.isMissed = false;
                        }
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
            // ★ 檢查時間是否為 0（非教學模式）
            if (!isTutorialMode && !isGameEnding && timeUIController != null && timeUIController.RemainingTime <= 0)
            {
                Debug.Log("[遊戲階段] 時間已到，清除所有目標並關閉 UI");
                
                // ★ 立即設置旗標防止重複觸發
                isGameEnding = true;
                isInGameplayPhase = false;
                
                // 清除所有剩餘目標
                ClearAllTargetsInternal();
                
                // 關閉方向提示 UI
                HideDirectionIndicator();
                
                // 關閉圓圈指示器 UI
                StopTimingIndicator();
                
                // 結束游戲階段
                StartCoroutine(GameEnded());

                return;
            }
            
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
        
        // ★ Tutorial 模式和 WaitingForPlayer 都要偵測輸入
        if (currentState == GameState.WaitingForPlayer || currentState == GameState.Tutorial)
        {
            DetectSlashInput();
        }
        
        // ★ Tutorial 模式和遊戲階段都要更新指示器
        if (isInGameplayPhase || currentState == GameState.Tutorial)
        {
            UpdateTimingIndicator();
            UpdateDirectionIndicatorForCurrentTarget();
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
    
    // ★ 更新方向提示（根據當前目標）
    void UpdateDirectionIndicatorForCurrentTarget()
    {
        if (directionIndicatorImage == null)
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
            // 根據目標方向更新提示圖示的旋轉角度
            UpdateDirectionIndicator(nextTarget.direction);
        }
        else
        {
            // 沒有目標時隱藏提示
            HideDirectionIndicator();
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
            // // ★ 暈眩時不能打擊
            // if (parallaxManager != null && parallaxManager.IsDizzy)
            // {
            //     Debug.Log("[測試] 暈眩中，無法打擊");
            //     return;
            // }
            
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
        // if (parallaxManager != null && parallaxManager.IsDizzy)
        // {
        //     return;
        // }
        
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
        
        Debug.Log($"[FindFlyingTarget] activeTargets.Count={activeTargets.Count}, 尋找方向={direction}, currentTime={currentTime:F3}");
        
        foreach (var target in activeTargets)
        {
            if (target == null || target.isHit || target.isMissed)
            {
                Debug.Log($"  [跳過] target==null:{target==null}, isHit:{target?.isHit}, isMissed:{target?.isMissed}");
                continue;
            }
            
            Debug.Log($"  [候選] stepIndex={target.stepIndex}, 方向={target.direction}, 飛行時間=[{target.flyingStartTime:F3}, {target.flyingStartTime + target.flyingDuration:F3}]");
            
            if (target.stepIndex < minStepIndex)
            {
                minStepIndex = target.stepIndex;
                nextTarget = target;
            }
        }
        
        // 檢查這個目標是否符合條件（方向匹配且在飛行中）
        if (nextTarget != null)
        {
            Debug.Log($"  [找到候選] stepIndex={nextTarget.stepIndex}, 方向={nextTarget.direction}, 檢查中...");
            
            // ★ 教學目標忽略時間窗口限制
            bool isInTimeWindow = nextTarget.isTutorialTarget || 
                                  (currentTime >= nextTarget.flyingStartTime && 
                                   currentTime < nextTarget.flyingStartTime + nextTarget.flyingDuration);
            
            if (nextTarget.direction == direction && isInTimeWindow)
            {
                Debug.Log($"  [成功找到] 目標#{nextTarget.stepIndex}");
                return nextTarget;
            }
            else
            {
                Debug.Log($"  [條件不符] 方向匹配:{nextTarget.direction == direction}, 在時間窗口內:{isInTimeWindow}, 是教學目標:{nextTarget.isTutorialTarget}");
            }
        }
        else
        {
            Debug.Log($"  [沒找到目標] activeTargets 中沒有未被擊中的目標");
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
        int controllerIndexForVibration = (cursorIndex == 0) ? 1 : 0;
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
            return ("easy", 80f, elapsedTime);
        }
        else if (elapsedTime < 20f)
        {
            return ("normal", 80f, elapsedTime);
        }

        return ("hard", 80f, elapsedTime);
    }

    // === 技能演出系統 ===
    
    // 開始技能演出
    void StartSkillCutscene(string nextDifficulty = null, float nextBpm = -1f)
    {
        // ★ 如果時間已到 0，跳過 cutscene 和音符生成
        if (!isTutorialMode && timeUIController != null && timeUIController.RemainingTime <= 0)
        {
            Debug.Log("[技能演出] 時間已到，跳過演出");
            StartCoroutine(GameEnded());
            return;
        }
        
        // 儲存下一輪的設定
        if (nextDifficulty != null) pendingDifficulty = nextDifficulty;
        if (nextBpm > 0) pendingBpm = nextBpm;

        if (skillCharacterImageLeft == null || skillCharacterImageRight == null || screenDarkOverlay == null)
        {
            Debug.LogWarning("[技能演出] 未設置必要組件，跳過演出");
            StartNewRoundAfterSkillCutscene();
            return;
        }
        
        currentState = GameState.SkillCutscene;
        isSkillCutsceneActive = true;
        
        // ★ 切換遊戲 UI Canvas 為 Screen Space - Camera
        if (gameUICanvas != null)
        {
            originalCanvasRenderMode = gameUICanvas.renderMode;
            gameUICanvas.renderMode = RenderMode.ScreenSpaceCamera;
            Debug.Log("[技能演出] Canvas 已切換為 Screen Space - Camera");
        }
        
        // ★ 隨機決定顯示左邊還是右邊
        bool showLeft = Random.value > 0.5f;
        
        // ★ 從 Sprite 陣列中選擇隨機立繪
        if (skillCharacterSprites != null && skillCharacterSprites.Length > 0)
        {
            int randomIndex = Random.Range(0, skillCharacterSprites.Length);
            
            if (showLeft)
            {
                skillCharacterImageLeft.sprite = skillCharacterSprites[randomIndex];
                Debug.Log($"[技能演出] 選擇左側立繪 #{randomIndex}");
            }
            else
            {
                skillCharacterImageRight.sprite = skillCharacterSprites[randomIndex];
                Debug.Log($"[技能演出] 選擇右側立繪 #{randomIndex}");
            }
        }
        
        StartCoroutine(PlaySkillCutsceneSequence(showLeft));
    }
    
    IEnumerator PlaySkillCutsceneSequence(bool showLeft)
    {
        // 1. 初始化狀態（設定在螢幕外）
        // 只顯示選中的那一邊
        if (showLeft) skillCharacterImageLeft.gameObject.SetActive(true);
        else skillCharacterImageRight.gameObject.SetActive(true);
        
        screenDarkOverlay.gameObject.SetActive(true);
        
        // 重置透明度
        if (showLeft)
        {
            Color c = skillCharacterImageLeft.color; c.a = 1; skillCharacterImageLeft.color = c;
        }
        else
        {
            Color c = skillCharacterImageRight.color; c.a = 1; skillCharacterImageRight.color = c;
        }
        
        RectTransform targetRT = showLeft ? skillCharacterImageLeft.rectTransform : skillCharacterImageRight.rectTransform;
        
        // 記錄原始位置（假設編輯器中擺放的位置就是目標位置）
        Vector2 originalPos = targetRT.anchoredPosition;
        
        // 設定起始位置（螢幕外）
        // 左邊往左移 1000，右邊往右移 1000
        float offset = 1000f;
        Vector2 startPos = originalPos + new Vector2(showLeft ? -offset : offset, 0);
        targetRT.anchoredPosition = startPos;
        
        // 初始化暗黑覆蓋
        Color darkColor = screenDarkOverlay.color;
        darkColor.a = 0f;
        screenDarkOverlay.color = darkColor;
        
        Debug.Log($"[技能演出] 開始滑入動畫 ({(showLeft ? "左" : "右")})");
        
        // 2. 滑入 & 變暗
        float elapsed = 0f;
        while (elapsed < skillSlideInDuration)
        {
            float t = elapsed / skillSlideInDuration;
            // EaseOutSine
            t = Mathf.Sin(t * Mathf.PI * 0.5f);
            
            targetRT.anchoredPosition = Vector2.Lerp(startPos, originalPos, t);
            
            darkColor.a = Mathf.Lerp(0f, 0.6f, t);
            screenDarkOverlay.color = darkColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 確保位置準確
        targetRT.anchoredPosition = originalPos;
        darkColor.a = 0.6f;
        screenDarkOverlay.color = darkColor;
        
        // 3. 停留展示
        yield return new WaitForSeconds(skillCutsceneDuration);
        
        // 4. 滑出 & 變亮
        elapsed = 0f;
        float slideOutDuration = 0.5f;
        while (elapsed < slideOutDuration)
        {
            float t = elapsed / slideOutDuration;
            // EaseInSine (for exit)
            // t = 1f - Mathf.Cos(t * Mathf.PI * 0.5f); 
            // 使用簡單 Lerp 即可
            
            targetRT.anchoredPosition = Vector2.Lerp(originalPos, startPos, t);
            
            darkColor.a = Mathf.Lerp(0.6f, 0f, t);
            screenDarkOverlay.color = darkColor;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 隱藏
        if (showLeft) skillCharacterImageLeft.gameObject.SetActive(false);
        else skillCharacterImageRight.gameObject.SetActive(false);
        
        screenDarkOverlay.gameObject.SetActive(false);
        
        // 恢復位置（以便下次使用，雖然下次會重置，但保持整潔）
        targetRT.anchoredPosition = originalPos;
        
        // ★ 還原遊戲 UI Canvas 的 Render Mode
        if (gameUICanvas != null)
        {
            gameUICanvas.renderMode = originalCanvasRenderMode;
            Debug.Log($"[技能演出] Canvas 已還原為 {originalCanvasRenderMode}");
        }
        
        // ★ 技能演出完成，開始遊戲
        isSkillCutsceneActive = false;
        StartNewRoundAfterSkillCutscene();
    }
    
    // 更新技能演出（邏輯已移至 Coroutine，此方法留空或移除）
    void UpdateSkillCutscene()
    {
        // 空方法，由 Coroutine 控制
    }
    
    // ============================================
    // ★ 開場鏡頭動畫系統
    // ============================================
    
    void StartIntroAnimation()
    {
        currentState = GameState.IntroAnimation;
        
        if (mainVirtualCamera == null)
        {
            Debug.LogWarning("[開場動畫] 未設置 Main Virtual Camera，跳過動畫直接開始遊戲");
            // 直接啟動遊戲流程
            if (parallaxManager != null)
            {
                parallaxManager.StartCameraMove();
            }
            StartSkillCutscene();
            return;
        }
        
        // 獲取 Follow Zoom 組件
        followZoom = mainVirtualCamera.GetComponent<Cinemachine.CinemachineFollowZoom>();
        if (followZoom == null)
        {
            Debug.LogWarning("[開場動畫] Virtual Camera 上未找到 Follow Zoom 組件，跳過縮放效果");
        }
        
        isIntroAnimationActive = true;
        StartCoroutine(PlayIntroAnimationSequence());
    }
    
    IEnumerator PlayIntroAnimationSequence()
    {
        Debug.Log("[開場動畫] 開始 FOV 縮放動畫序列");
        
        float originalMinFOV = normalMinFOV;
        if (followZoom != null)
        {
            originalMinFOV = followZoom.m_MinFOV;
        }
        
        // === 第一階段：FOV 縮小（放大效果） ===
        if (followZoom != null)
        {
            Debug.Log($"[開場動畫] 縮放進入，FOV: {originalMinFOV} → {introMinFOV}，時間 {introZoomInDuration}s");
            
            float elapsed = 0f;
            while (elapsed < introZoomInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / introZoomInDuration);
                followZoom.m_MinFOV = Mathf.Lerp(originalMinFOV, introMinFOV, t);
                yield return null;
            }
            followZoom.m_MinFOV = introMinFOV;
        }
        else
        {
            yield return new WaitForSeconds(introZoomInDuration);
        }
        
        // === 第二階段：Zoom In 完成後才啟動動畫 ===
        if (introAnimationObject != null)
        {
            introAnimationObject.SetActive(true);
            Debug.Log("[開場動畫] 啟動動畫物件");
        }
        
        // 如果有 Animator，觸發動畫
        if (introAnimationAnimator != null && !string.IsNullOrEmpty(introAnimationTrigger))
        {
            introAnimationAnimator.SetTrigger(introAnimationTrigger);
            Debug.Log($"[開場動畫] 觸發動畫：{introAnimationTrigger}");
        }
        // 如果沒有 Animator，使用隨機移動動畫
        else if (introAnimationObject != null)
        {
            Debug.Log("[開場動畫] 未設置 Animator，啟動隨機移動動畫");
            StartRandomChildAnimations(introAnimationObject);
        }
        
        // 等待動畫播放完成
        Debug.Log($"[開場動畫] 播放動畫 {introAnimationDuration}s");
        yield return new WaitForSeconds(introAnimationDuration);
        
        // === 第三階段：FOV 恢復（縮小效果）- 動畫繼續播放 ===
        if (followZoom != null)
        {
            Debug.Log($"[開場動畫] 縮放退出，FOV: {introMinFOV} → {originalMinFOV}，時間 {introZoomOutDuration}s（動畫繼續播放）");
            
            float elapsed = 0f;
            while (elapsed < introZoomOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, elapsed / introZoomOutDuration);
                followZoom.m_MinFOV = Mathf.Lerp(introMinFOV, originalMinFOV, t);
                yield return null;
            }
            followZoom.m_MinFOV = originalMinFOV;
        }
        else
        {
            yield return new WaitForSeconds(introZoomOutDuration);
        }
        
        // === Zoom Out 完成後才停止動畫 ===
        // 如果未勾選持續播放，則停止動畫
        if (!keepAnimationPlaying)
        {
            // 停止所有隨機動畫
            StopRandomChildAnimations();
            
            // 關閉動畫物件
            if (introAnimationObject != null)
            {
                introAnimationObject.SetActive(false);
            }
        }
        else
        {
            Debug.Log("[開場動畫] 持續播放模式已啟用，動畫將繼續播放");
        }
        
        isIntroAnimationActive = false;
        Debug.Log("[開場動畫] 動畫序列完成，開始遊戲流程");
        
        // === 動畫完成後，啟動視差背景並開始技能演出 ===
        if (parallaxManager != null)
        {
            parallaxManager.StartCameraMove();
            Debug.Log("[ParallaxManager] 視差背景已啟動");
        }
        
        // 進入技能演出階段
        StartSkillCutscene();
    }
    
    // ★ 開始所有子物件的隨機移動動畫
    void StartRandomChildAnimations(GameObject parent)
    {
        // 清除舊的協程列表和位置記錄
        childAnimationCoroutines.Clear();
        childOriginalPositions.Clear();
        
        // 遍歷所有子物件
        foreach (Transform child in parent.transform)
        {
            if (child.gameObject.activeSelf)
            {
                // 保存原始位置
                childOriginalPositions[child] = child.localPosition;
                
                Coroutine coroutine = StartCoroutine(RandomMoveAnimation(child));
                childAnimationCoroutines.Add(coroutine);
            }
        }
        
        Debug.Log($"[開場動畫] 啟動 {childAnimationCoroutines.Count} 個子物件的隨機移動動畫");
    }
    
    // ★ 停止所有隨機移動動畫
    void StopRandomChildAnimations()
    {
        // 停止所有協程
        foreach (var coroutine in childAnimationCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        
        // 恢復所有子物件的原始位置
        foreach (var pair in childOriginalPositions)
        {
            if (pair.Key != null)
            {
                pair.Key.localPosition = pair.Value;
            }
        }
        
        childAnimationCoroutines.Clear();
        childOriginalPositions.Clear();
        
        Debug.Log("[開場動畫] 已停止所有隨機移動動畫並恢復原始位置");
    }
    
    // ★ 單個物件的隨機移動動畫
    IEnumerator RandomMoveAnimation(Transform target)
    {
        Vector3 originalPosition = target.localPosition;
        
        // 隨機延遲開始（製造錯落感）
        float randomDelay = Random.Range(0f, 0.3f);
        yield return new WaitForSeconds(randomDelay);
        
        // 所有可能的方向
        Vector2[] directions = new Vector2[]
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right,
            new Vector2(1, 1).normalized,    // 右上
            new Vector2(-1, 1).normalized,   // 左上
            new Vector2(1, -1).normalized,   // 右下
            new Vector2(-1, -1).normalized   // 左下
        };
        
        // 來回移動動畫 - 無限循環直到被 StopCoroutine 停止
        while (true)
        {
            // 每個周期隨機生成新的參數
            Vector2 randomDirection = directions[Random.Range(0, directions.Length)];
            float randomDistance = randomMoveDistance * Random.Range(0.5f, 1.5f); // 距離也隨機
            float randomSpeed = randomMoveSpeed * Random.Range(0.7f, 1.3f); // 速度也隨機
            
            Vector3 targetOffset = new Vector3(
                randomDirection.x * randomDistance,
                randomDirection.y * randomDistance,
                0
            );
            
            // 完成一個完整的來回動畫周期（一個正弦波周期）
            float cycleTime = 0f;
            float cycleDuration = (2f * Mathf.PI) / (randomSpeed * Mathf.PI); // 計算完整周期時間
            
            while (cycleTime < cycleDuration)
            {
                cycleTime += Time.deltaTime;
                
                // 使用正弦波創建來回移動效果
                float wave = Mathf.Sin(cycleTime * randomSpeed * Mathf.PI);
                target.localPosition = originalPosition + targetOffset * wave;
                
                yield return null;
            }
            
            // 周期結束，回到原始位置，然後重新隨機新的方向
            target.localPosition = originalPosition;
        }
    }
    
    // ★ 新增方法：在技能演出後開始遊戲
    void StartNewRoundAfterSkillCutscene()
    {
        // 使用暫存的設定（如果有）
        string diff = pendingDifficulty;
        float b = pendingBpm;
        
        // 清除暫存
        pendingDifficulty = null;
        pendingBpm = -1f;
        
        StartNewRound(diff, b);
    }
    
    public void StartNewRound(string overrideDifficulty = null, float overrideBpm = -1f)
    {
        Debug.Log("=== 開始新一輪 ===");
        
        // ★ 重置遊戲結束旗標
        isGameEnding = false;
        
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
            currentState = GameState.WaitingForPlayer;
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
        
        // ★ 初始化擊打計數（在每一輪開始時）
        // 分數、combo、統計都不重置，因為需要累積到遊戲結束
        hitCount = 0;
        
        // ★ 在遊戲階段開始時初始化記分板（只在非教學模式下）
        if (!isTutorialMode)
        {
            if (scoreboard == null)
                scoreboard = FindObjectOfType<scoreingame>();
            
            if (scoreboard != null)
            {
                // scoreboard.InitializeScoreboard();
                // Debug.Log("[ScratchRhythmGame] 記分板已初始化");
            }
        }
        

        
        // sequenceDisplayText.text = "記住這個順序...";
        Debug.Log($"[新回合] 時間={elapsedTime:F1}s, BPM={bpm}, 難度={difficulty}, 模式長度={currentPattern.Count}");
    }
    
    // 在節拍上生成下一個目標（提示階段）
    void SpawnNextBeatTarget()
    {
        // ★ 檢查時間是否到期，如果時間 <= 0 就不再生成（教學模式跳過）
        if (!isTutorialMode && timeUIController != null && timeUIController.RemainingTime <= 0)
        {
            currentState = GameState.Idle;
            Debug.Log("[遊戲階段] 時間已到，清除所有目標並關閉 UI");
                
            // 清除所有剩餘目標
            ClearAllTargetsInternal();
            
            // 關閉方向提示 UI
            HideDirectionIndicator();
            
            // 關閉圓圈指示器 UI
            StopTimingIndicator();
            
            // 結束游戲階段
            StartCoroutine(GameEnded());
            return;
        }
        
        if (currentPatternIndex >= currentPattern.Count)
        {
            // 所有目標已生成，準備開始遊戲階段
            // sequenceDisplayText.text = "準備好了嗎？";
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
        
        // ★ 更新方向提示 Image
        UpdateDirectionIndicator(dir);
        
        // 播放提示音效
        if (beatSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(beatSound);
        }
        
        // ★ 選擇投擲物類型（隨機或根據邏輯）
        AudioClip typeHitSound = null;
        Sprite typeSprite = null;
        
        if (projectileTypes != null && projectileTypes.Length > 0)
        {
            // 隨機選擇一種類型
            int typeIndex = Random.Range(0, projectileTypes.Length);
            var pType = projectileTypes[typeIndex];
            
            // ★ 從該類型的音效陣列中隨機挑一個
            if (pType.hitSounds != null && pType.hitSounds.Length > 0)
            {
                int soundIndex = Random.Range(0, pType.hitSounds.Length);
                typeHitSound = pType.hitSounds[soundIndex];
            }
            
            // 根據方向選擇對應圖片
            switch (dir)
            {
                case SlashDirection.Left: typeSprite = pType.leftSprite; break;
                case SlashDirection.Right: typeSprite = pType.rightSprite; break;
                case SlashDirection.DownLeft: typeSprite = pType.downLeftSprite; break;
                case SlashDirection.DownRight: typeSprite = pType.downRightSprite; break;
            }
        }
        
        // ★ 3D 模式：在世界空間生成（只用一個 Prefab）
        Transform parent3D = targets3DParent != null ? targets3DParent : null;
        GameObject targetObj = Instantiate(slashTargetPrefab, parent3D);
        
        // ★ 根據方向計算生成位置偏差（跳舞機分區）
        Vector3 offset = CalculateDirectionOffset(dir);
        
        if (spawnPoint != null)
        {
            Vector3 position = spawnPoint.position;
            position += offset;
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
        target3D.targetOffset = offset; // ★ 恢復偏移，目標點隨著生成位置偏移
        target3D.arcHeight = arcHeight;
        
        // ★ 設置類型特定的音效（存在字典上，不存在 target 上）
        if (typeHitSound != null)
        {
            targetHitSounds[target3D] = typeHitSound;
        }
        if (typeSprite != null) target3D.SetDirectionSprite(typeSprite);
        
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

    // ★ 新增方法：生成教學用單一目標
    public SlashTarget3D SpawnTutorialTarget(SlashDirection dir, float duration)
    {
        // ★ 3D 模式：在世界空間生成（只用一個 Prefab）
        Transform parent3D = targets3DParent != null ? targets3DParent : null;
        GameObject targetObj = Instantiate(slashTargetPrefab, parent3D);
        
        // ★ 根據方向計算生成位置偏差
        Vector3 offset = CalculateDirectionOffset(dir);
        
        if (spawnPoint != null)
        {
            Vector3 position = spawnPoint.position;
            position += offset;
            targetObj.transform.position = position;
        }
        
        SlashTarget3D target3D = targetObj.GetComponent<SlashTarget3D>();
        if (target3D == null)
            target3D = targetObj.AddComponent<SlashTarget3D>();
        
        target3D.direction = dir;
        target3D.stepIndex = 999; // 特殊索引
        target3D.spawnTime = Time.time;
        target3D.spawnPoint = spawnPoint;
        target3D.targetPoint = targetPoint;
        target3D.targetOffset = offset;
        target3D.arcHeight = arcHeight;
        
        // 設置方向圖片和音效
        if (projectileTypes != null && projectileTypes.Length > 0)
        {
            var pType = projectileTypes[0]; // 使用第一種類型
            Sprite typeSprite = null;
            switch (dir)
            {
                case SlashDirection.Left: typeSprite = pType.leftSprite; break;
                case SlashDirection.Right: typeSprite = pType.rightSprite; break;
                case SlashDirection.DownLeft: typeSprite = pType.downLeftSprite; break;
                case SlashDirection.DownRight: typeSprite = pType.downRightSprite; break;
            }
            if (typeSprite != null) target3D.SetDirectionSprite(typeSprite);
            
            // ★ 設置音效（從 projectileTypes 中隨機選一個）
            if (pType.hitSounds != null && pType.hitSounds.Length > 0)
            {
                int soundIndex = Random.Range(0, pType.hitSounds.Length);
                targetHitSounds[target3D] = pType.hitSounds[soundIndex];
            }
        }
        
        // 如果沒有從 projectileTypes 獲取到音效，使用預設音效
        if (!targetHitSounds.ContainsKey(target3D) && hitSound != null)
        {
            targetHitSounds[target3D] = hitSound;
        }
        
        target3D.customInterval = duration;
        target3D.flyingStartTime = Time.time;
        target3D.flyingDuration = duration;
        target3D.hasPlayedJudgmentBeat = false; // ★ 教學模式也播放節拍音效
        target3D.isTutorialTarget = true; // ★ 標記為教學目標
        
        target3D.Initialize();
        activeTargets.Add(target3D);
        
        // ★ 播放提示音效（教學模式也需要）
        if (beatSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(beatSound);
            Debug.Log("[Tutorial] 播放提示音效");
        }
        
        // ★ 只更新方向提示，不啟動圓圈指示器
        // 圓圈指示器會在玩家階段才啟動
        UpdateDirectionIndicator(dir);
        
        return target3D;
    }
    
    // ★ 新增方法：更新方向提示 Image（通過旋轉）
    void UpdateDirectionIndicator(SlashDirection direction)
    {
        if (directionIndicatorImage == null)
        {
            return;
        }
        
        float rotationAngle = 0f;
        
        switch (direction)
        {
            case SlashDirection.Left:
                rotationAngle = 180f; // 左
                break;
            case SlashDirection.Right:
                rotationAngle = 0f; // 右
                break;
            case SlashDirection.DownLeft:
                rotationAngle = 225f; // 左下
                break;
            case SlashDirection.DownRight:
                rotationAngle = 315f; // 右下
                break;
        }
        
        directionIndicatorImage.rectTransform.localRotation = Quaternion.Euler(0, 0, rotationAngle);
        directionIndicatorImage.gameObject.SetActive(true);
        Debug.Log($"[方向提示] 旋轉至 {rotationAngle}°（{direction}）");
    }
    
    // ★ 新增方法：隱藏方向提示
    public void HideDirectionIndicator()
    {
        if (directionIndicatorImage != null)
        {
            directionIndicatorImage.gameObject.SetActive(false);
        }
    }
    
    // ★ 新增方法：根據方向計算投擲物的生成位置偏差（左到右四個區域）
    Vector3 CalculateDirectionOffset(SlashDirection direction)
    {
        Vector3 offset = Vector3.zero;
        
        switch (direction)
        {
            case SlashDirection.Left:
                // 最左邊區域
                offset.x = -directionZoneWidth * 1.5f;
                break;
            case SlashDirection.DownLeft:
                // 左邊區域
                offset.x = -directionZoneWidth * 0.5f;
                break;
            case SlashDirection.DownRight:
                // 右邊區域
                offset.x = directionZoneWidth * 0.5f;
                break;
            case SlashDirection.Right:
                // 最右邊區域
                offset.x = directionZoneWidth * 1.5f;
                break;
        }
        
        Debug.Log($"[方向區域] {direction} 偏差: {offset}");
        return offset;
    }
    
    // 開始遊戲階段
    IEnumerator StartGameplayPhase()
    {
        currentState = GameState.Idle; // 暫停狀態
        
        // ★ 隱藏方向提示（提示階段結束）
        HideDirectionIndicator();
        
        // ★ 移除等待時間，讓指示器能完整顯示 1 秒的縮小過程
        // yield return new WaitForSeconds(beatInterval * 0.5f);
        yield return null;
        
        // sequenceDisplayText.text = "開始！跟著節拍揮動！";
        
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
        
        // ★ 隱藏方向提示
        HideDirectionIndicator();
        
        // ★ 教學模式立即觸發事件並結束
        if (isTutorialMode)
        {
            Debug.Log("教學模式回合結束，立即觸發事件");
            ClearAllTargetsInternal();
            OnRoundComplete?.Invoke();
            currentState = GameState.Idle;
            yield break;
        }
        
        yield return new WaitForSeconds(1f);
        
        // sequenceDisplayText.text = $"完成！分數: {score}";

        // yield return new WaitForSeconds(2f);
        
        // ★ 移除難度提升邏輯，改由 StartNewRound 根據時間控制
        // currentLevel++;
        // bpm += 10f; 
        // beatInterval = 60f / bpm; 
        // speedMultiplier += 0.1f;
        
        Debug.Log($"[下一輪準備] 當前分數: {score}");
        
        // 可以在這裡開始下一輪或返回主選單
        Debug.Log("清除目標並開始新一輪");
        ClearAllTargetsInternal();
        
        // ★ 觸發回合結束事件
        OnRoundComplete?.Invoke();

        // ★ 在開始新一輪前先播放技能演出
        currentState = GameState.SkillCutscene;
        StartSkillCutscene();
    }
    
    // 劃動完成（全螢幕檢測）
    void OnSlashComplete(SlashTarget target, float slashDistance, float slashTime, int controllerIndex = -1)
    {
        Debug.Log($"[遊戲] 劃動完成：目標#{target.stepIndex}, 方向={target.direction}, 距離={slashDistance:F1}, 時間={slashTime:F2}s, 控制器={controllerIndex}");
        
        float currentTime = Time.time;
        
        // ★ 單音符教學模式：只能在目標暫停時揮動
        bool isTutorialHit = false;
        if (isSingleNoteTutorial)
        {
            if(!target.isPaused)
            {
                Debug.Log("[單音符教學] 目標未暫停，忽略此次揮動");
                target.isHit = false; // 重置 isHit 標記
                return;
            }
            else 
            {
                isSingleNoteTutorial = false; // 只允許一次
                isTutorialHit = true;
            }
        }
        
        // ★ 檢查冷卻時間，防止一次揮動觸發多個物件
        if (lastSlashTime > 0 && currentTime - lastSlashTime < slashCooldown)
        {
            Debug.Log($"[遊戲] ✗ 揮動冷卻中（距離上次 {currentTime - lastSlashTime:F2}s < {slashCooldown:F2}s），忽略此次揮動");
            return;
        }
        
        if (currentState != GameState.WaitingForPlayer && currentState != GameState.ShowSequence && currentState != GameState.Tutorial)
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
        
        // ★ 優先從字典中取出該 target 的擊中音效
        if (targetHitSounds.TryGetValue(target, out AudioClip targetSound))
        {
            soundToPlay = targetSound;
        }
        
        // ★ 單音符教學模式：強制判定為 Perfect
        if (isTutorialHit)
        {
            rating = "Perfect!!";
            points = 0;
            timingOffset = 0f;
            
            // 使用字典中的音效或 Perfect 音效
            if (!targetHitSounds.TryGetValue(target, out soundToPlay))
            {
                soundToPlay = perfectSound != null ? perfectSound : hitSound;
            }
            
            // 觸發 Perfect 震動
            if (vibrationManager != null)
            {
                vibrationManager.VibrateOnPerfect(controllerIndex);
            }
            
            Debug.Log("[單音符教學] 強制判定為 PERFECT!!!");
        }
        // ★ 正常遊戲模式：間隔比較判定
        else if (lastPlayerHitTime < 0f && hitCount > 1) // 防呆：如果不是第一個且時間未設置
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
                // ★ 使用字典中的音效或 Perfect 音效
                if (!targetHitSounds.TryGetValue(target, out soundToPlay))
                {
                    soundToPlay = perfectSound != null ? perfectSound : hitSound;
                }
                
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
                // ★ 如果字典中沒有，就用預設 Hit 音效
                if (!targetHitSounds.TryGetValue(target, out soundToPlay))
                {
                    soundToPlay = hitSound;
                }
                
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
                // ★ 如果字典中沒有，就用預設 Hit 音效
                if (!targetHitSounds.TryGetValue(target, out soundToPlay))
                {
                    soundToPlay = hitSound;
                }
                
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
        
        // ★ 清理字典中的音效記錄
        if (targetHitSounds.ContainsKey(target))
        {
            targetHitSounds.Remove(target);
        }
        
        // 劃動成功！
        target.MarkAsCompleted();
        
        // 如果是 Miss，不增加 Combo
        if (rating != "Miss")
        {
            combo++;
            score += points * combo;
        }
        
        // ★ 記錄到記分板（非教學模式）
        if (!isTutorialMode)
        {
            if (scoreboard == null)
                scoreboard = FindObjectOfType<scoreingame>();
            
            if (scoreboard != null)
            {
                scoreboard.RecordJudgment(rating, points, combo);
                scoreboard.UpdateTotalScore(score);
            }
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
        
        // ★ 記錄到記分板（非教學模式）
        if (!isTutorialMode)
        {
            if (scoreboard == null)
                scoreboard = FindObjectOfType<scoreingame>();
            
            if (scoreboard != null)
            {
                scoreboard.RecordJudgment("Miss", 0, combo);
                scoreboard.UpdateTotalScore(score);
            }
        }
        
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
        
        // ★ 記錄到記分板（非教學模式）
        if (!isTutorialMode)
        {
            if (scoreboard == null)
                scoreboard = FindObjectOfType<scoreingame>();
            
            if (scoreboard != null)
            {
                scoreboard.RecordJudgment("Miss", 0, combo);
                scoreboard.UpdateTotalScore(score);
            }
        }
        
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
    public bool AreAllConnectedControllersCalibrated()
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
        // ★ 清空音效字典
        targetHitSounds.Clear();
        
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
    public void ActivateTimingIndicator(float interval)
    {
        // ★ 同時顯示 perfectCircle（內圈）和 timingCircle（外圈）
        if (perfectCircle != null)
        {
            perfectCircle.gameObject.SetActive(true);
        }
        
        if (timingCircle != null)
        {
            timingCircle.gameObject.SetActive(true);
            timingCircle.transform.localScale = Vector3.one * 4f;
            timingCircle.color = Color.red;
        }
        
        // ★ 解決方案：重設基準時間，確保動畫從頭開始
        lastPlayerHitTime = Time.time;
        
        currentTargetInterval = interval;
        nextExpectedHitTime = lastPlayerHitTime + interval;
        isIndicatorActive = true;
        currentHitIndex = 1;
        
        Debug.Log($"[指示器] 啟動（內外圈同時顯示），間隔={interval:F3}s, 下次預期={nextExpectedHitTime:F2}");
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
    public void StopTimingIndicator()
    {
        isIndicatorActive = false;
        
        // ★ 同時隱藏兩個圓圈
        if (timingCircle != null)
            timingCircle.gameObject.SetActive(false);
        
        if (perfectCircle != null)
            perfectCircle.gameObject.SetActive(false);
        
        Debug.Log("[指示器] 已停止（內外圈已隱藏）");
    }

    // ==========================================
    // ★ Tutorial Support Methods
    // ==========================================

    public void StartTutorialMode()
    {
        isTutorialMode = true;
        currentState = GameState.Tutorial;
        // Hide gameplay UI
        SetGameplayUIActive(false);
    }
    
    /// <summary>
    /// 初始化遊戲分數（進入 Gameplay 階段時調用，只初始化一次）
    /// </summary>
    public void InitializeGameScore()
    {
        score = 0;      // 總積分只在進入 Gameplay 時重置
        combo = 0;      // Combo 也只在進入 Gameplay 時重置
        hitCount = 0;   // 擊打計數只在進入 Gameplay 時重置
        lastPlayerHitTime = -1f;
        lastSlashTime = -1f;
        
        Debug.Log("[ScratchRhythmGame] 遊戲分數已初始化（進入 Gameplay），之後所有統計累積");
    }

    public void SetGameplayUIActive(bool active)
    {
        if (scoreText) scoreText.gameObject.SetActive(active);
        if (feedbackText) feedbackText.gameObject.SetActive(active);
        if (flyingTimerText) flyingTimerText.gameObject.SetActive(active);
        if (sequenceDisplayText) sequenceDisplayText.gameObject.SetActive(active);
    }

    public SlashTarget3D lastSpawnedTutorialTarget;

    public void SpawnSingleTutorialTarget(SlashDirection dir, float speedMultiplier)
    {
        if (slashTargetPrefab == null) return;

        GameObject targetObj = Instantiate(slashTargetPrefab, spawnPoint.position, Quaternion.identity);
        targetObj.transform.SetParent(targets3DParent);
        
        SlashTarget3D target3D = targetObj.AddComponent<SlashTarget3D>();
        target3D.direction = dir;
        target3D.spawnTime = Time.time;
        target3D.spawnPoint = spawnPoint;
        target3D.targetPoint = targetPoint;
        
        // Calculate offset
        Vector3 offset = CalculateDirectionOffset(dir);
        target3D.targetOffset = offset;
        
        target3D.arcHeight = arcHeight;
        target3D.flyingStartTime = Time.time;
        float duration = flyingDuration / speedMultiplier;
        target3D.flyingDuration = duration;
        
        // Set sprite
        if (projectileTypes != null && projectileTypes.Length > 0)
        {
             var pType = projectileTypes[0]; // Use first type
             Sprite s = null;
             switch(dir) {
                 case SlashDirection.Left: s = pType.leftSprite; break;
                 case SlashDirection.Right: s = pType.rightSprite; break;
                 case SlashDirection.DownLeft: s = pType.downLeftSprite; break;
                 case SlashDirection.DownRight: s = pType.downRightSprite; break;
             }
             if (s != null) target3D.SetDirectionSprite(s);
        }

        target3D.Initialize();
        activeTargets.Add(target3D);
        lastSpawnedTutorialTarget = target3D;
        
        // ★ Update indicator
        UpdateDirectionIndicator(dir);
        
        // ★ Activate Timing Circle
        // We manually set the timing variables to match this single target
        if (timingCircle != null)
        {
            timingCircle.gameObject.SetActive(true);
            timingCircle.transform.localScale = Vector3.one * 4f;
            timingCircle.color = Color.red;
        }
        currentTargetInterval = duration;
        // For tutorial, we want the circle to close exactly when the target arrives
        nextExpectedHitTime = Time.time + duration;
        lastPlayerHitTime = Time.time; // Fake last hit time
        isIndicatorActive = true;
    }

    public void SpawnPracticeSequence(int count)
    {
        StartCoroutine(SpawnPracticeSequenceRoutine(count));
    }

    private IEnumerator SpawnPracticeSequenceRoutine(int count)
    {
        float interval = 2.0f; // Fixed interval for practice
        
        // Reset timing for sequence
        lastPlayerHitTime = Time.time;
        
        for (int i = 0; i < count; i++)
        {
            SlashDirection dir = (SlashDirection)Random.Range(0, 4);
            
            // Spawn target
            if (slashTargetPrefab != null)
            {
                GameObject targetObj = Instantiate(slashTargetPrefab, spawnPoint.position, Quaternion.identity);
                targetObj.transform.SetParent(targets3DParent);
                
                SlashTarget3D target3D = targetObj.AddComponent<SlashTarget3D>();
                target3D.direction = dir;
                target3D.spawnTime = Time.time;
                target3D.spawnPoint = spawnPoint;
                target3D.targetPoint = targetPoint;
                target3D.targetOffset = CalculateDirectionOffset(dir);
                target3D.arcHeight = arcHeight;
                target3D.flyingStartTime = Time.time;
                target3D.flyingDuration = flyingDuration;
                
                // Set sprite
                if (projectileTypes != null && projectileTypes.Length > 0)
                {
                     var pType = projectileTypes[0];
                     Sprite s = null;
                     switch(dir) {
                         case SlashDirection.Left: s = pType.leftSprite; break;
                         case SlashDirection.Right: s = pType.rightSprite; break;
                         case SlashDirection.DownLeft: s = pType.downLeftSprite; break;
                         case SlashDirection.DownRight: s = pType.downRightSprite; break;
                     }
                     if (s != null) target3D.SetDirectionSprite(s);
                }

                target3D.Initialize();
                activeTargets.Add(target3D);
            }

            // ★ Update Indicators
            UpdateDirectionIndicator(dir);
            
            // Ensure Timing Indicator is running for this new target
            if (!isIndicatorActive)
            {
                lastPlayerHitTime = Time.time;
                ActivateTimingIndicator(flyingDuration);
            }
            
            yield return new WaitForSeconds(interval); 
        }
    }

    public void StartGameFromTutorial()
    {
        isTutorialMode = false;
        SetGameplayUIActive(true);
        currentState = GameState.WaitingForStart;
        StartSkillCutscene();
    }
    IEnumerator GameEnded()
    {
        // ★ 確保關閉所有立繪和暗黑覆蓋
        yield return new WaitForSeconds(1.0f);
        parallaxManager.impactVolume.weight = 0;

        if (skillCharacterImageLeft != null) 
            skillCharacterImageLeft.gameObject.SetActive(false);
        if (skillCharacterImageRight != null) 
            skillCharacterImageRight.gameObject.SetActive(false);
        if (screenDarkOverlay != null) 
            screenDarkOverlay.gameObject.SetActive(false);
        
        // ★ 重置 cutscene 狀態
        isSkillCutsceneActive = false;
        
        // ★ 還原 Canvas Render Mode
        // if (gameUICanvas != null && originalCanvasRenderMode != RenderMode.ScreenSpaceOverlay)
        // {
        //     gameUICanvas.renderMode = originalCanvasRenderMode;
        // }
        
        Debug.Log("[GameEnded] 立繪已關閉，1秒後切換到 ScoreDisplay");
        
        // ★ 保留 1 秒延遲
        sceneController.SetState(SceneController.GameState.ScoreDisplay);
        
        // ★ 重置旗標（下一場遊戲使用）
        isGameEnding = false;
    }
    public void uploadscore()
    {
        GoogleSheetDataHandler.Instance.UploadScore(score);
    }
}
