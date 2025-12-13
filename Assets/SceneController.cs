using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Switch;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

public class SceneController : MonoBehaviour
{
    [Header("場景控制")]
    public KeyCode nextStateKey = KeyCode.Space; // 按此鍵進入下一個狀態
    public bool developerMode = false; // ★ 開發者模式：允許在 Tutorial/Gameplay 階段跳過
    public string sceneAName = "Scene A";
    public string sceneBName = "Scene B";
    public List<GameObject> sceneAObjects = new List<GameObject>();

    [Header("場景B物件 (遊戲場景)")]
    public List<GameObject> sceneBObjects = new List<GameObject>();

    [Header("UI控制")]
    public GameObject preparationUI;    // 準備階段UI (主視覺/Live2D)
    public GameObject prologueUI;       // 前導階段UI (動畫、劇情、教學)
    public GameObject tutorialUI;       // 教學階段UI (可選)
    public GameObject gameplayUI;       // 遊戲階段UI
    public GameObject scoreUI;          // 分數展示UI (積分、排行榜)
    public GameObject goodEndingUI;     // Good Ending UI (結局動畫/劇情)
    public GameObject badEndingUI;      // Bad Ending UI (結局動畫/劇情)
    public GameObject loadingUI;        // 偽loading動畫
    public Canvas gameUI;
    public float loadingDuration = 2f;  // loading持續時間

    // Ending判定
    public enum EndingType { Good, Bad } // Ending類型
    public EndingType currentEndingType = EndingType.Good;

    // 遊戲狀態
    public enum GameState
    {
        // 場景A - 準備階段
        Preparation,         // 準備階段：顯示主視覺/Live2D，交代注意事項
        PreparationLoading,  // 準備完成 → 前導階段的過渡(偽loading)
        
        // 場景A/B - 前導階段
        Prologue,            // 前導階段：播放動畫、交代劇情、操作教學
        PrologueLoading,     // 前導完成 → 遊戲體驗的過渡(偽loading)

        Tutorial,            // 教學階段 (可選，場景B專用)
        TutorialLoading,     // 教學完成 → 遊戲體驗的過渡(偽loading)
        
        // 場景B - 遊戲體驗階段
        Gameplay,            // 遊戲進行中
        GameplayLoading,     // 遊戲完成 → 收尾階段的過渡(偽loading)
        
        // 場景B - 收尾階段
        ScoreDisplay,       // 顯示分數階段
        ScoreDisplayLoading, // 分數展示完成 → 收尾階段的過渡(偽loading)
        Ending,              // 收尾階段：公布積分、排行榜、結局動畫
        EndingLoading        // 收尾完成 → 準備返回(偽loading)
    }

    public GameState currentState;
    private string currentScene = "";
    public ScratchRhythmGame rhythmGame;
    public Tutorial tutorial; // ★ Tutorial 組件引用
    private bool isTransitioning = false; // 是否在過渡中
    // 如果場景A是從場景B繼承 EndingType，設定此旗標以避免 SetupSceneA 覆寫初始狀態
    private bool inheritedEndingPending = false;
    // 紀錄是否已處理繼承的 Ending（用於避免重複初始化時被覆寫）
    private bool inheritedEndingHandled = false;
    private TransitionController transitionController;
    // 在初始化階段允許覆寫同一狀態（避免 inspector 預設值阻止 SetState 執行）
    private bool allowSetStateWhenSame = false;
    public scoreingame scoreingame;
    // public BGMController bgmController;
    public List<SwitchControllerHID> allControllers = new List<SwitchControllerHID>();
    private bool blockoverlayswitch = false;

    void OnEnable()
    {
        // 讀取當前場景名稱
        currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[SceneController] 當前場景: {currentScene}");

        // 初始化
        // InitializeScene();

        // 場景加載事件
        SceneManager.sceneLoaded += OnSceneLoaded;
        // rhythmGame = FindObjectOfType<ScratchRhythmGame>();
        
        // ★ 獲取所有連接的 Switch 控制器
        UpdateControllerList();
    }

    void Start()
    {
        // 確保 loadingUI 永遠保持 active（請在 Inspector 連結 loadingUI）
        if (loadingUI != null)
            loadingUI.SetActive(true);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        // 持續檢查當前state，確保UI和物件狀態正確
        // UpdateCurrentState();

        // ★ Gameplay 階段時，不處理 SceneController 的按鍵，交給 RhythmGame 處理
        if (currentState == GameState.Gameplay && !blockoverlayswitch)
        {
            if (gameUI != null)
                gameUI.renderMode = RenderMode.ScreenSpaceOverlay;
            return; // 跳過按鍵檢測，讓 RhythmGame 處理
        }
        
        // ★ Tutorial 階段時，只有當 Tutorial 組件正在執行時且非開發者模式才禁止跳過
        if (currentState == GameState.Tutorial && !developerMode)
        {
            // 如果 Tutorial 組件存在且未完成，禁止跳過
            if (tutorial != null && !tutorial.isTutorialCompleted)
            {
                return; // Tutorial 正在執行，禁止跳過
            }
            // Tutorial 已結束，允許繼續
        }
        
        // ★ 定期更新控制器列表（每秒更新一次避免遺漏）
        if (Time.frameCount % 60 == 0)
        {
            UpdateControllerList();
        }
        
        // 按 Space 進入下一個狀態
        if (Input.GetKeyDown(nextStateKey))
        {
            GoToNextState();
        }
        
        // ★ 檢查所有控制器的按鈕
        bool anyButtonPressed = false;
        foreach (var controller in allControllers)
        {
            if (controller != null)
            {
                // 檢查 East 按鈕（B 鍵）或 Left D-Pad
                if (controller.buttonEast.wasPressedThisFrame || controller.dpad.left.wasPressedThisFrame)
                {
                    anyButtonPressed = true;
                    break;
                }
            }
        }
        
        if (anyButtonPressed)
        {
            GoToNextState();
        }
        
        // 其他階段設置 Camera 渲染模式
        if (gameUI != null)
            gameUI.renderMode = RenderMode.ScreenSpaceCamera;
    }

    /// <summary>
    /// 持續檢查並更新當前state的UI和物件狀態
    /// </summary>
    void UpdateCurrentState()
    {
        // 先隱藏所有UI
        // HideAllUI();

        // 根據當前state顯示對應的UI
        switch (currentState)
        {
            case GameState.Preparation:
                if (preparationUI != null)
                    preparationUI.SetActive(true);
                break;

            case GameState.PreparationLoading:
                if (loadingUI != null)
                    loadingUI.SetActive(true);
                break;

            case GameState.Prologue:
                if (prologueUI != null)
                    prologueUI.SetActive(true);
                break;

            case GameState.PrologueLoading:
                if (loadingUI != null)
                    loadingUI.SetActive(true);
                break;

            case GameState.Tutorial:
                if (tutorialUI != null)
                    tutorialUI.SetActive(true);
                break;

            case GameState.TutorialLoading:
                if (loadingUI != null)
                    loadingUI.SetActive(true);
                break;

            case GameState.Gameplay:
                if (gameplayUI != null)
                    gameplayUI.SetActive(true);
                if (rhythmGame != null)
                    rhythmGame.enabled = true;
                
                // ⭐ 切換到 Gameplay 專用 BGM
                if (BGMController.Instance != null)
                {
                    BGMController.Instance.PlayGameplayBGM();
                }
                break;

            case GameState.GameplayLoading:
                if (loadingUI != null)
                    loadingUI.SetActive(true);
                break;

            case GameState.ScoreDisplay:
                if (scoreUI != null)
                    scoreUI.SetActive(true);
                
                // ⭐ 切回選單 BGM
                if (BGMController.Instance != null)
                {
                    BGMController.Instance.PlayMenuBGM();
                }
                break;

            case GameState.ScoreDisplayLoading:
                if (loadingUI != null)
                    loadingUI.SetActive(true);
                break;

            case GameState.Ending:
                // 根據Ending類型顯示對應的UI
                if (currentEndingType == EndingType.Good)
                {
                    if (goodEndingUI != null)
                        goodEndingUI.SetActive(true);
                }
                else
                {
                    if (badEndingUI != null)
                        badEndingUI.SetActive(true);
                }
                break;

            case GameState.EndingLoading:
                if (loadingUI != null)
                    loadingUI.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// 場景加載時呼叫
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentScene = scene.name;
        Debug.Log($"[SceneController] 場景已加載: {currentScene}");
        InitializeScene();
    }

    /// <summary>
    /// 根據場景初始化物件
    /// </summary>
    void InitializeScene()
    {
        // 嘗試從 PlayerPrefs 恢復繼承的 EndingType（若先前從場景B傳來）
        bool hadInheritedEnding = false;
        // 不在此重置 inheritedEndingPending — 我們需要保留該旗標直到處理完成
        if (PlayerPrefs.HasKey("InheritedEndingType"))
        {
            int stored = PlayerPrefs.GetInt("InheritedEndingType");
            if (System.Enum.IsDefined(typeof(EndingType), stored))
            {
                currentEndingType = (EndingType)stored;
                hadInheritedEnding = true;
                inheritedEndingPending = true;
                inheritedEndingHandled = false;
                Debug.Log($"[SceneController] 恢復繼承的 EndingType: {currentEndingType}");
            }
            PlayerPrefs.DeleteKey("InheritedEndingType");
        }

        if (currentScene == sceneAName)
        {
            // 允許在初始化時即使 newState == currentState 也強制執行 SetState
            allowSetStateWhenSame = true;
            SetupSceneA();

            // 如果剛剛有繼承的 EndingType，直接進入 ScoreDisplay
            if (hadInheritedEnding)
            {
                SetState(GameState.Ending);
                // 標記為已處理，避免之後的 InitializeScene 覆寫
                inheritedEndingHandled = true;
                inheritedEndingPending = false;
            }

            allowSetStateWhenSame = false;
        }
        else if (currentScene == sceneBName)
        {
            SetupSceneB();
        }
    }

    /// <summary>
    /// 設置場景A (準備 + 前導階段)
    /// </summary>
    void SetupSceneA()
    {
        Debug.Log("[SceneController] === 場景A初始化 ===");
        HideAllUI();
        
        // 進入初始狀態 Preparation（若有繼承的 Ending 或已處理則不要覆寫）
        if (!inheritedEndingPending && !inheritedEndingHandled)
        {
            SetState(GameState.Preparation);
        }

        // 啟用場景A物件
        foreach (var obj in sceneAObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        // 禁用場景B物件
        foreach (var obj in sceneBObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        // 禁用遊戲邏輯
        rhythmGame = FindObjectOfType<ScratchRhythmGame>();
        if (rhythmGame != null)
            rhythmGame.enabled = false;
        UpdateCurrentState();
    }

    /// <summary>
    /// 設置場景B (前導 + 遊戲 + 收尾階段)
    /// </summary>
    void SetupSceneB()
    {
        HideAllUI();
        Debug.Log("[SceneController] === 場景B初始化 ===");

        // 進入初始狀態 Tutorial
        // SetState(GameState.Tutorial);

        // 啟用場景B物件
        foreach (var obj in sceneBObjects)
        {
            if (obj != null)
                obj.SetActive(true);
        }

        // 禁用場景A物件
        foreach (var obj in sceneAObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        // 初始化遊戲邏輯（但暫不啟用）
        // rhythmGame = FindObjectOfType<ScratchRhythmGame>();
        // if (rhythmGame != null)
        //     rhythmGame.enabled = false;

        StartCoroutine(tcFromOtherScene());
        UpdateCurrentState();
    }

    /// <summary>
    /// 設置遊戲狀態（根據state決定開啟哪些物件和UI）
    /// </summary>
    public void SetState(GameState newState)
    {
        // 如果正在過渡中，拒絕狀態變更
        if (isTransitioning)
            return;

        // 預設情況下若 newState 與 currentState 相同則忽略。
        // 在初始化期間 allowSetStateWhenSame=true 時，強制執行以確保初始化行為能被觸發。
        if (!allowSetStateWhenSame && currentState == newState)
            return;

        currentState = newState;
        Debug.Log($"[SceneController] 狀態變更: {currentState}");

        // 先隱藏所有UI
        HideAllUI();
        TransitionController tc = FindObjectOfType<TransitionController>();


        // 根據新狀態決定啟用的物件和UI
        switch (newState)
        {
            case GameState.Preparation:
                StartCoroutine(tcFromOtherScene());
                BGMController.Instance.Resume();
                BGMController.Instance.PlayMenuBGM();
                BGMController.Instance.RestoreVolume();
                Debug.Log("[SceneController] === 準備階段 ===");
                if (preparationUI != null)
                    preparationUI.SetActive(true);
                // if(tc.canvas != null)
                //     tc.canvas.gameObject.SetActive(false);
                if (rhythmGame != null)
                    rhythmGame.enabled = false;
                break;

            case GameState.PreparationLoading:
                Debug.Log("[SceneController] === 準備Loading ===");
                StartCoroutine(ShowLoadingTransition(GameState.Prologue));
                break;

            case GameState.Prologue:
                Debug.Log("[SceneController] === 前導階段 ===");
                
                // ★ 將 BGM 音量調為 0
                if (BGMController.Instance != null)
                {
                    BGMController.Instance.Mute();
                }
                
                if (prologueUI != null)
                {
                    prologueUI.SetActive(true);
                    // 尋找並播放 Video Player
                    StartCoroutine(PlayPrologueVideo());
                }
                
                if (rhythmGame != null)
                    rhythmGame.enabled = false; // 前導階段不運行遊戲邏輯
                break;

            case GameState.PrologueLoading:
                Debug.Log("[SceneController] === 前導Loading ===");
                
                // ★ 恢復 BGM 音量
                if (BGMController.Instance != null)
                {
                    BGMController.Instance.SetVolume(0.3f);
                }
                
                StartCoroutine(ShowLoadingTransition(GameState.Tutorial));
                break;

            case GameState.Tutorial:
                Debug.Log("[SceneController] === 教學階段 ===");
                // if(currentScene == sceneAName)
                // {

                // }
                if (tutorialUI != null)
                    tutorialUI.SetActive(true); // 教學也用前導UI
                if (rhythmGame != null)
                    rhythmGame.enabled = false; // 教學不真正開始遊戲
                break;

            case GameState.TutorialLoading:
                Debug.Log("[SceneController] === 教學Loading ===");
                gameUI.gameObject.SetActive(true);
                rhythmGame.scoreText.text = 0.ToString();

                StartCoroutine(ShowLoadingTransition(GameState.Gameplay));
                break;

            case GameState.Gameplay:
                Debug.Log("[SceneController] === 遊戲體驗階段 ===");
                
                // ★ 重新查找 rhythmGame 確保有效引用
                if (rhythmGame == null)
                {
                    rhythmGame = FindObjectOfType<ScratchRhythmGame>();
                    Debug.Log("[SceneController] 重新查找 RhythmGame");
                }
                
                // ★ 第一步：先初始化 RhythmGame 的分數和 Combo（確保不繼承教學階段的數據）
                if (rhythmGame != null)
                {
                    rhythmGame.InitializeGameScore();
                    Debug.Log("[SceneController] RhythmGame 分數已初始化");
                }
                
                // ★ 第二步：強制重置計分板（確保 score 從 0 開始）
                if(scoreingame != null)
                {
                    scoreingame.InitializeScoreboard(forceReset: true);
                    Debug.Log("[SceneController] 計分板已強制重置");
                }
                
                if (gameplayUI != null)
                {
                    gameplayUI.SetActive(true);                    
                    // ★ 開啟所有 GameplayUI 的子物件，除了方向提示 Canvas
                    foreach (Transform child in gameplayUI.transform)
                    {
                        child.gameObject.SetActive(true);
                    }
                }
                

                if (rhythmGame != null)
                {
                    rhythmGame.enabled = true; // 啟用遊戲邏輯
                    rhythmGame.currentState = ScratchRhythmGame.GameState.WaitingForStart;
                    
                    Debug.Log($"[SceneController] RhythmGame currentState 已設置為 {rhythmGame.currentState}");
                }
                else
                {
                    Debug.LogError("[SceneController] RhythmGame 為 null，無法設置狀態！");
                }
                BGMController.Instance.PlayGameplayBGM();
                StartCoroutine(StartGame());
                break;

            case GameState.GameplayLoading:
                Debug.Log("[SceneController] === 遊戲Loading ===");
                if (rhythmGame != null)
                    rhythmGame.enabled = false;
                SetState(GameState.ScoreDisplay);
                break;

            case GameState.ScoreDisplay:
                Debug.Log("[SceneController] === 顯示分數階段 ===");
                if (scoreUI != null)
                {
                    scoreUI.SetActive(true);
                }
                if(gameplayUI != null)
                {
                    gameplayUI.SetActive(true);
                    foreach (Transform child in gameplayUI.transform)
                    {
                        if(child.gameObject.name != "Backgrounds")
                            child.gameObject.SetActive(false);
                        else
                            child.gameObject.SetActive(true);
                    }
                }
                // BGMController.Instance.PlayMenuBGM();

                if(scoreingame != null)
                {
                    scoreingame.UpdateUI();
                }
                if (rhythmGame != null){
                    rhythmGame.enabled = false;
                    rhythmGame.introAnimationObject.SetActive(false);
                }
                break;
            case GameState.ScoreDisplayLoading:
                Debug.Log("[SceneController] === 分數展示Loading ===");
                StartCoroutine(ShowLoadingTransition(GameState.Ending));
                break;

            case GameState.Ending:
                if(currentScene == sceneBName)
                {
                    // StartCoroutine(tcFromOtherScene());
                    PlayerPrefs.SetInt("InheritedEndingType", (int)currentEndingType);
                    PlayerPrefs.SetString(GameState.Ending.ToString(), currentState.ToString());
                    PlayerPrefs.Save();
                    Debug.Log($"[SceneController] 儲存 EndingType 給場景A: {currentEndingType}");
                    SceneManager.LoadScene(sceneAName);
                    return;
                }
                StartCoroutine(tcFromOtherScene());
                BGMController.Instance.Pause();
                Debug.Log("[SceneController] === 收尾階段 ===");
                // 根據Ending類型顯示對應的UI
                if (currentEndingType == EndingType.Good)
                {
                    Debug.Log("[SceneController] Good Ending");
                    if (goodEndingUI != null){
                        goodEndingUI.SetActive(true);
                        StartCoroutine(PlayGoodEnding());
                    }
                    if (badEndingUI != null){
                        badEndingUI.SetActive(false);
                    }
                }
                else
                {
                    Debug.Log("[SceneController] Bad Ending");
                    if (badEndingUI != null)
                    {
                        badEndingUI.SetActive(true);
                        StartCoroutine(PlayBadEnding());
                    }
                    if (goodEndingUI != null)
                        goodEndingUI.SetActive(false);
                }
                if (rhythmGame != null)
                    rhythmGame.enabled = false;
                break;

            case GameState.EndingLoading:
                Debug.Log("[SceneController] === 收尾Loading ===");
                StartCoroutine(ShowLoadingTransition(GameState.Preparation));
                break;
        }
    }

    /// <summary>
    /// 隱藏所有UI
    /// </summary>
    void HideAllUI()
    {
        // if (loadingUI != null)
        //     loadingUI.SetActive(false);
        if (preparationUI != null)
            preparationUI.SetActive(false);
        if (prologueUI != null)
            prologueUI.SetActive(false);
        if (tutorialUI != null)
            tutorialUI.SetActive(false);
        if (gameplayUI != null)
            gameplayUI.SetActive(false);
        if (scoreUI != null)
        {
            scoreUI.SetActive(false);
        }
        if (goodEndingUI != null)
            goodEndingUI.SetActive(false);
        if (badEndingUI != null)
            badEndingUI.SetActive(false);
    }

    /// <summary>
    /// 顯示過渡loading，完成後自動進入下一個狀態
    /// </summary>
    IEnumerator ShowLoadingTransition(GameState nextState)
    {
        GameObject beforeTransition = null;
        GameObject afterTransition = null;

        if(nextState == GameState.Prologue)
        {
            beforeTransition = preparationUI;
            afterTransition = prologueUI;
        }
        else if(nextState == GameState.Tutorial)
        {
            beforeTransition = prologueUI;
            // afterTransition = tutorialUI;
        }
        else if(nextState == GameState.Gameplay)
        {
            beforeTransition = tutorialUI;
            afterTransition = gameUI.gameObject;
        }
        else if(nextState == GameState.ScoreDisplay)
        {
            beforeTransition = gameUI.gameObject;
            afterTransition = scoreUI; // 多個UI無法指定
        }
        else if(nextState == GameState.Ending)
        {
            beforeTransition = scoreUI; // 多個UI無法指定
            if (currentEndingType == EndingType.Good)
                afterTransition = goodEndingUI;
            else
                afterTransition = badEndingUI;
        }
        else if(nextState == GameState.Preparation)
        {
            if(currentScene == sceneAName)
            {
                beforeTransition = (currentEndingType == EndingType.Good) ? goodEndingUI : badEndingUI;
                afterTransition = preparationUI;
            }
        }
        isTransitioning = true;
        beforeTransition.SetActive(true);

        // 嘗試使用 TransitionController 的 maskAnimator 來播放 Shrink/Expand
        TransitionController tc = FindObjectOfType<TransitionController>();

        if (tc != null && tc.maskAnimator != null)
        {
            tc.maskAnimator.gameObject.SetActive(true);
            tc.maskAnimator.SetTrigger("Shrink");
            yield return new WaitForSeconds(tc.shrinkTime);
            beforeTransition.SetActive(false);

        }
        if(nextState == GameState.Tutorial)
        {
            // 這兩個階段不顯示 afterTransition UI
            SceneManager.LoadScene(sceneBName);
            yield return null;
        }
        if(nextState == GameState.Ending)
        {
            PlayerPrefs.SetInt("InheritedEndingType", (int)currentEndingType);
            PlayerPrefs.SetString(GameState.Ending.ToString(), currentState.ToString());
            PlayerPrefs.Save();
            Debug.Log($"[SceneController] 儲存 EndingType 給場景A: {currentEndingType}");
            SceneManager.LoadScene(sceneAName);
            yield return null;
        }

        Debug.Log($"[SceneController] Loading中... 將進入 {nextState}");
        // 等待邏輯上的 loading 時間
        yield return new WaitForSeconds(1.0f);
        afterTransition.SetActive(true);


        // loading 結束，觸發 Expand 動畫
        if (tc != null && tc.maskAnimator != null)
        {
            tc.maskAnimator.SetTrigger("Expand");
            // 等待 expand 動畫，如果 TransitionController 有設定的 expandTime 則等該時間
            // 等待 TransitionController 的 expand 動畫時間
            blockoverlayswitch = true;
            isTransitioning = false;
            SetState(nextState);
            yield return new WaitForSeconds(1.0f);
            blockoverlayswitch = false;
            // 動畫結束後可選擇關閉遮罩物件（保留可視化）
            tc.maskAnimator.gameObject.SetActive(false);
        }
        

        // 自動進入下一個狀態
        
    }
    IEnumerator tcFromOtherScene()
    {
        TransitionController tc = FindObjectOfType<TransitionController>();

        if (tc != null && tc.maskAnimator != null)
        {
            tc.maskAnimator.gameObject.SetActive(true);
            tc.maskAnimator.SetTrigger("Expand");
            // 等待 expand 動畫，如果 TransitionController 有設定的 expandTime 則等該時間
            // 等待 TransitionController 的 expand 動畫時間
            yield return new WaitForSeconds(tc.expandTime);
            // 動畫結束後可選擇關閉遮罩物件（保留可視化）
            tc.maskAnimator.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 自動進入下一個狀態
    /// </summary>
    void GoToNextState()
    {
        GameState nextState = currentState;

        // 狀態流程：Preparation → PreparationLoading → Prologue → PrologueLoading → Gameplay → GameplayLoading → Ending → EndingLoading → Preparation
        switch (currentState)
        {
            case GameState.Preparation:
                nextState = GameState.PreparationLoading;
                break;
            case GameState.PreparationLoading:
                // 已由Loading自動進入Prologue
                break;
            case GameState.Prologue:
                nextState = GameState.PrologueLoading;
                break;
            case GameState.PrologueLoading:
                // 已由Loading自動進入Gameplay
                break;
            case GameState.Gameplay:
                nextState = GameState.GameplayLoading;
                break;
            case GameState.GameplayLoading:
                // 已由Loading自動進入Ending
                break;
            case GameState.ScoreDisplay:
                nextState = GameState.ScoreDisplayLoading;
                break;
            case GameState.ScoreDisplayLoading:
                // 已由Loading自動進入Ending
                break;
            case GameState.Ending:
                nextState = GameState.EndingLoading;
                break;
            case GameState.EndingLoading:
                // 已由Loading自動進入Preparation
                break;
            case GameState.Tutorial:
                nextState = GameState.TutorialLoading;
                break;
            case GameState.TutorialLoading:
                // 已由Loading自動進入Gameplay
                break;
        }

        if (nextState != currentState)
        {
            SetState(nextState);
        }
    }

    /// <summary>
    /// 設置Ending類型 (Good 或 Bad)
    /// </summary>
    public void SetEndingType(EndingType endingType)
    {
        currentEndingType = endingType;
        Debug.Log($"[SceneController] Ending類型已設置: {currentEndingType}");
    }

    /// <summary>
    /// 獲取當前Ending類型
    /// </summary>
    public EndingType GetEndingType()
    {
        return currentEndingType;
    }

    /// <summary>
    /// 獲取當前狀態
    /// </summary>
    public GameState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// 獲取當前場景名稱
    /// </summary>
    public string GetCurrentScene()
    {
        return currentScene;
    }

    /// <summary>
    /// 檢查是否在場景A
    /// </summary>
    public bool IsInSceneA()
    {
        return currentScene == sceneAName;
    }

    /// <summary>
    /// 檢查是否在場景B
    /// </summary>
    public bool IsInSceneB()
    {
        return currentScene == sceneBName;
    }

    /// <summary>
    /// 在 Prologue 階段播放 Video Player
    /// </summary>
    IEnumerator PlayPrologueVideo()
    {
        // 延遲 2 秒
        yield return new WaitForSeconds(1.0f);
        
        // 在 prologueUI 中尋找 VideoPlayer 組件
        if (prologueUI != null)
        {
            UnityEngine.Video.VideoPlayer videoPlayer = prologueUI.GetComponentInChildren<UnityEngine.Video.VideoPlayer>();
            
            if (videoPlayer != null)
            {
                Debug.Log("[SceneController] 找到 Video Player，開始播放");
                videoPlayer.Play();
                
                // 等待影片實際開始播放（給一些時間讓 VideoPlayer 初始化）
                yield return new WaitForSeconds(0.5f);
                
                // 等待影片播放完成
                float duration = (float)videoPlayer.clip.length;
                yield return new WaitForSeconds(duration);
                
                // 影片播放完成後，延遲 1 秒
                yield return new WaitForSeconds(1f);
                
                // 自動進入下一個狀態
                Debug.Log("[SceneController] Prologue 影片播放完成，自動進入下一個狀態");
                GoToNextState();
            }
            else
            {
                Debug.LogWarning("[SceneController] 在 prologueUI 中未找到 Video Player 組件");
            }
        }
    }
    IEnumerator PlayGoodEnding()
    {
        // 延遲 1 秒
        yield return new WaitForSeconds(1f);
        
        // 在 goodEndingUI 中尋找 VideoPlayer 組件
        if (goodEndingUI != null)
        {
            UnityEngine.Video.VideoPlayer videoPlayer = goodEndingUI.GetComponentInChildren<UnityEngine.Video.VideoPlayer>();
            
            if (videoPlayer != null)
            {
                Debug.Log("[SceneController] 找到 Video Player，開始播放");
                videoPlayer.Play();
                
                // 等待影片實際開始播放（給一些時間讓 VideoPlayer 初始化）
                yield return new WaitForSeconds(0.5f);
                
                // 等待影片播放完成
                float duration = (float)videoPlayer.clip.length;
                yield return new WaitForSeconds(duration);
                
                // // 影片播放完成後，延遲 1 秒
                // yield return new WaitForSeconds(1f);
                
                // 自動進入下一個狀態
                Debug.Log("[SceneController] Good Ending 影片播放完成，自動進入下一個狀態");
                GoToNextState();
            }
            else
            {
                Debug.LogWarning("[SceneController] 在 goodEndingUI 中未找到 Video Player 組件");
            }
        }
    }
    IEnumerator PlayBadEnding()
    {
        // 延遲 1 秒
        yield return new WaitForSeconds(1f);
        
        // 在 badEndingUI 中尋找 VideoPlayer 組件
        if (badEndingUI != null)
        {
            UnityEngine.Video.VideoPlayer videoPlayer = badEndingUI.GetComponentInChildren<UnityEngine.Video.VideoPlayer>();
            
            if (videoPlayer != null)
            {
                Debug.Log("[SceneController] 找到 Video Player，開始播放");
                videoPlayer.Play();
                
                // 等待影片實際開始播放（給一些時間讓 VideoPlayer 初始化）
                yield return new WaitForSeconds(0.5f);
                
                // 等待影片播放完成
                float duration = (float)videoPlayer.clip.length;
                yield return new WaitForSeconds(duration);
                
                // // 影片播放完成後，延遲 1 秒
                // yield return new WaitForSeconds(1f);
                
                // 自動進入下一個狀態
                Debug.Log("[SceneController] Bad Ending 影片播放完成，自動進入下一個狀態");
                GoToNextState();
            }
            else
            {
                Debug.LogWarning("[SceneController] 在 badEndingUI 中未找到 Video Player 組件");
            }
        }
    }
    
    /// <summary>
    /// 更新控制器列表 - 獲取所有連接的 Switch 控制器
    /// </summary>
    void UpdateControllerList()
    {
        allControllers.Clear();
        
        // 使用 InputSystem 查找所有 SwitchControllerHID 設備
        foreach (var device in UnityEngine.InputSystem.InputSystem.devices)
        {
            if (device is SwitchControllerHID controller)
            {
                allControllers.Add(controller);
            }
        }
        
        Debug.Log($"[SceneController] 找到 {allControllers.Count} 個 Switch 控制器");
    }
    IEnumerator StartGame()
    {
        yield return new WaitForSeconds(1.0f);
        rhythmGame.startPressed = true;
    }
}
