using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    [Header("場景控制")]
    public KeyCode nextStateKey = KeyCode.Space; // 按此鍵進入下一個狀態
    public string sceneAName = "Scene A";
    public string sceneBName = "Scene B";
    public List<GameObject> sceneAObjects = new List<GameObject>();

    [Header("場景B物件 (遊戲場景)")]
    public List<GameObject> sceneBObjects = new List<GameObject>();

    [Header("UI控制")]
    public GameObject preparationUI;    // 準備階段UI (主視覺/Live2D)
    public GameObject prologueUI;       // 前導階段UI (動畫、劇情、教學)
    public GameObject gameplayUI;       // 遊戲階段UI
    public List<GameObject> scoreUI;          // 分數展示UI (積分、排行榜)
    public GameObject goodEndingUI;     // Good Ending UI (結局動畫/劇情)
    public GameObject badEndingUI;      // Bad Ending UI (結局動畫/劇情)
    public GameObject loadingUI;        // 偽loading動畫
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

    private GameState currentState = GameState.Preparation;
    private string currentScene = "";
    private ScratchRhythmGame rhythmGame;
    private bool isTransitioning = false; // 是否在過渡中

    void Start()
    {
        // 讀取當前場景名稱
        currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[SceneController] 當前場景: {currentScene}");

        // 初始化
        InitializeScene();

        // 場景加載事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        // 持續檢查當前state，確保UI和物件狀態正確
        UpdateCurrentState();

        // 按 Space 進入下一個狀態
        if (Input.GetKeyDown(nextStateKey))
        {
            GoToNextState();
        }
    }

    /// <summary>
    /// 持續檢查並更新當前state的UI和物件狀態
    /// </summary>
    void UpdateCurrentState()
    {
        // 先隱藏所有UI
        HideAllUI();

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
                if (prologueUI != null)
                    prologueUI.SetActive(true);
                break;

            case GameState.TutorialLoading:
                if (loadingUI != null)
                    loadingUI.SetActive(true);
                break;

            case GameState.Gameplay:
                if (gameplayUI != null)
                    gameplayUI.SetActive(true);
                break;

            case GameState.GameplayLoading:
                if (loadingUI != null)
                    loadingUI.SetActive(true);
                break;

            case GameState.ScoreDisplay:
                if (scoreUI != null && scoreUI.Count > 0)
                    foreach (var ui in scoreUI)
                        ui.SetActive(true);
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
        if (currentScene == sceneAName)
        {
            SetupSceneA();
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
        
        // 進入初始狀態 Preparation
        SetState(GameState.Preparation);

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
    }

    /// <summary>
    /// 設置場景B (前導 + 遊戲 + 收尾階段)
    /// </summary>
    void SetupSceneB()
    {
        Debug.Log("[SceneController] === 場景B初始化 ===");

        // 進入初始狀態 Prologue
        SetState(GameState.Prologue);

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
        rhythmGame = FindObjectOfType<ScratchRhythmGame>();
    }

    /// <summary>
    /// 設置遊戲狀態（根據state決定開啟哪些物件和UI）
    /// </summary>
    public void SetState(GameState newState)
    {
        if (currentState == newState || isTransitioning)
            return;

        currentState = newState;
        Debug.Log($"[SceneController] 狀態變更: {currentState}");

        // 先隱藏所有UI
        HideAllUI();

        // 根據新狀態決定啟用的物件和UI
        switch (newState)
        {
            case GameState.Preparation:
                Debug.Log("[SceneController] === 準備階段 ===");
                if (preparationUI != null)
                    preparationUI.SetActive(true);
                if (rhythmGame != null)
                    rhythmGame.enabled = false;
                break;

            case GameState.PreparationLoading:
                Debug.Log("[SceneController] === 準備Loading ===");
                StartCoroutine(ShowLoadingTransition(GameState.Prologue));
                break;

            case GameState.Prologue:
                Debug.Log("[SceneController] === 前導階段 ===");
                if (prologueUI != null)
                    prologueUI.SetActive(true);
                if (rhythmGame != null)
                    rhythmGame.enabled = false; // 前導階段不運行遊戲邏輯
                break;

            case GameState.PrologueLoading:
                Debug.Log("[SceneController] === 前導Loading ===");
                StartCoroutine(ShowLoadingTransition(GameState.Gameplay));
                break;

            case GameState.Tutorial:
                Debug.Log("[SceneController] === 教學階段 ===");
                if (prologueUI != null)
                    prologueUI.SetActive(true); // 教學也用前導UI
                if (rhythmGame != null)
                    rhythmGame.enabled = false; // 教學不真正開始遊戲
                break;

            case GameState.TutorialLoading:
                Debug.Log("[SceneController] === 教學Loading ===");
                StartCoroutine(ShowLoadingTransition(GameState.Gameplay));
                break;

            case GameState.Gameplay:
                Debug.Log("[SceneController] === 遊戲體驗階段 ===");
                if (gameplayUI != null)
                    gameplayUI.SetActive(true);
                if (rhythmGame != null)
                    rhythmGame.enabled = true; // 啟用遊戲邏輯
                break;

            case GameState.GameplayLoading:
                Debug.Log("[SceneController] === 遊戲Loading ===");
                if (rhythmGame != null)
                    rhythmGame.enabled = false;
                StartCoroutine(ShowLoadingTransition(GameState.ScoreDisplay));
                break;

            case GameState.ScoreDisplay:
                Debug.Log("[SceneController] === 顯示分數階段 ===");
                if (scoreUI != null)
                {
                    foreach (var ui in scoreUI)
                    {
                        if (ui != null)
                            ui.SetActive(true);
                    }
                }
                if (rhythmGame != null)
                    rhythmGame.enabled = false;
                break;
            case GameState.ScoreDisplayLoading:
                Debug.Log("[SceneController] === 分數展示Loading ===");
                StartCoroutine(ShowLoadingTransition(GameState.Ending));
                break;

            case GameState.Ending:
                Debug.Log("[SceneController] === 收尾階段 ===");
                // 根據Ending類型顯示對應的UI
                if (currentEndingType == EndingType.Good)
                {
                    Debug.Log("[SceneController] Good Ending");
                    if (goodEndingUI != null)
                        goodEndingUI.SetActive(true);
                    if (badEndingUI != null)
                        badEndingUI.SetActive(false);
                }
                else
                {
                    Debug.Log("[SceneController] Bad Ending");
                    if (badEndingUI != null)
                        badEndingUI.SetActive(true);
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
        if (preparationUI != null)
            preparationUI.SetActive(false);
        if (prologueUI != null)
            prologueUI.SetActive(false);
        if (gameplayUI != null)
            gameplayUI.SetActive(false);
        if (scoreUI != null)
        {
            foreach (var ui in scoreUI)
            {
                if (ui != null)
                    ui.SetActive(false);
            }
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
        isTransitioning = true;

        // 顯示loading UI
        if (loadingUI != null)
            loadingUI.SetActive(true);

        Debug.Log($"[SceneController] Loading中... 將進入 {nextState}");
        yield return new WaitForSeconds(loadingDuration);

        // 隱藏loading UI
        if (loadingUI != null)
            loadingUI.SetActive(false);

        isTransitioning = false;

        // 自動進入下一個狀態
        SetState(nextState);
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
}
