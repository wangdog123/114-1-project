using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Tutorial : MonoBehaviour
{
    [Header("References")]
    public SceneController sceneController;
    public ScratchRhythmGame rhythmGame;

    [Header("UI Elements")]
    public Canvas textCanvas;          // ★ 文字顯示用的 Canvas（需要包含 centerText 和 textBackground）
    public TextMeshProUGUI centerText; // 用於顯示 "Are you ready?", "Calibration" 等
    public GameObject textBackground;  // ★ 文字背景
    public GameObject focusPanel;      // 第三幕的聚焦 Panel
    public TextMeshProUGUI focusText;  // 聚焦 Panel 上的文字
    public Image darkOverlay;          // 用於淡入淡出

    [Header("Tutorial State")]
    public bool isTutorialCompleted = false; // ★ Tutorial 是否完成的旗標
    public bool isWaitingForPlayerChoice = false; // ★ 是否正在等待玩家選擇（重複練習）

    [Header("Stage Objects")]
    public List<GameObject> calibrationObjects; // 第二幕：校正物件
    public List<GameObject> gameplayObjects;    // 第三、四幕：遊戲物件

    [Header("Game UI Control")]
    public List<GameObject> gameUIElementsToActive; // ★ Tutorial 期間需要開啟的 GameUI 子物件

    [Header("動畫引導")]
    public Animator swingAnimator;     // ★ Swing 動畫 (單音符階段)
    public Animator caliAnimator;      // ★ Calibration 動畫 (校正階段)

    [Header("Stage Texts")]
    [TextArea] public string stage1Text = "Are you ready?";
    [TextArea] public string stage2Text = "請校正 Joy-Con\n(平放於桌面)";
    [TextArea] public string stage3Text = "試著擊中音符！";
    [TextArea] public string stage4Text = "練習模式\n跟著節奏！";
    [TextArea] public string stage5Text = "準備好了嗎？\n遊戲開始！";

    [Header("Settings")]
    public float fadeInDuration = 1.0f;
    public float stayDuration = 2.0f;
    public float fadeOutDuration = 1.0f;
    public float charTypingInterval = 0.05f; // ★ 逐字稿每個字的顯示間隔（秒）
    public float postTextPauseDuration = 2.0f; // ★ 文字顯示完後的停頓時間（秒）
    private MultiSwitchControllerManager multiSwitchControllerManager;
    public bool playerMadeChoice = false;
    public bool chooseRepeat = false;

    private void OnEnable()
    {
        if (sceneController == null) sceneController = FindObjectOfType<SceneController>();
        if (rhythmGame == null) rhythmGame = FindObjectOfType<ScratchRhythmGame>();

        isTutorialCompleted = false; // ★ 重置旗標
        StartCoroutine(TutorialSequence());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        // 確保時間恢復
        Time.timeScale = 1f;
        if (focusPanel != null) focusPanel.SetActive(false);
        if (centerText != null) centerText.gameObject.SetActive(false);
        if (textBackground != null) textBackground.SetActive(false);
        
        // ★ 關閉所有動畫引導
        StopAllAnimations();
        
        // 確保物件狀態重置（可選，視需求而定）
        ToggleObjects(calibrationObjects, false);
        ToggleObjects(gameplayObjects, false);

        // ★ 恢復 GameUI 完整狀態
        RestoreGameUI();
    }

    IEnumerator TutorialSequence()
    {
        // 初始化：關閉特定物件
        ToggleObjects(calibrationObjects, false);
        ToggleObjects(gameplayObjects, false);

        // ★ 先關閉文字 Canvas，等轉場結束
        if (textCanvas != null)
        {
            textCanvas.gameObject.SetActive(false);
        }

        // ★ 設置 GameUI：開啟父物件，關閉所有子物件，只開啟指定子物件
        SetupGameUIForTutorial();
        
        // ★ 停頓 2 秒等轉場結束
        yield return new WaitForSeconds(2.0f);

        // === 第一幕：顯示準備文字 (淡入淡出) ===
        // 需求：遊戲場景(SceneController已處理)、文字、底色
        yield return StartCoroutine(ShowTextSequence(stage1Text, 2.0f));

        // === 第二幕：校正 Joy-Con ===
        // 需求：文字、底色、校正物件
        ToggleObjects(calibrationObjects, true);
        
        // ★ 播放 Calibration 動畫
        PlayAnimation("cali");
        
        // ★ 開啟文字背景和逐字稿
        if (textBackground != null) textBackground.SetActive(true);
        if (centerText != null)
        {
            centerText.gameObject.SetActive(true);
            yield return StartCoroutine(TypeOutText(stage2Text));
        }
        
        // ★ 等待校正完成（文字保持顯示）
        yield return StartCoroutine(WaitForCalibration());
        
        // 校正完成後才關閉文字和動畫
        if (centerText != null) centerText.gameObject.SetActive(false);
        if (textBackground != null) textBackground.SetActive(false);
        ToggleObjects(calibrationObjects, false);
        
        // ★ 關閉 Calibration 動畫
        StopAnimation("cali");

        // ★ 在進入第三幕前，確保圓圈指示器全部隱藏
        if (rhythmGame != null)
        {
            if (rhythmGame.perfectCircle != null)
                rhythmGame.perfectCircle.gameObject.SetActive(false);
            if (rhythmGame.timingCircle != null)
                rhythmGame.timingCircle.gameObject.SetActive(false);
        }

        yield return new WaitForSecondsRealtime(1.0f);

        // === 第三幕：單一音符練習 (暫停教學) ===
        // 需求：文字、底色、遊戲物件
        ToggleObjects(gameplayObjects, true);
        yield return StartCoroutine(SingleNotePractice());

        // === 第四幕：實際演練 (Easy Round) ===
        // 需求：遊戲物件 (延續第三幕)
        yield return StartCoroutine(PracticeRound());

        // === 第五幕：準備開始 ===
        // 需求：文字、底色 (關閉遊戲物件)
        yield return new WaitForSecondsRealtime(1.0f);
        ToggleObjects(gameplayObjects, false);
        
        // ★ 在第五幕前隱藏所有圓圈指示器
        if (rhythmGame != null)
        {
            if (rhythmGame.perfectCircle != null)
                rhythmGame.perfectCircle.gameObject.SetActive(false);
            if (rhythmGame.timingCircle != null)
                rhythmGame.timingCircle.gameObject.SetActive(false);
        }
        
        // ★ 顯示第五幕文字（不自動關閉）
        if (textBackground != null)
        {
            textBackground.SetActive(true);
        }
        if (centerText != null)
        {
            centerText.gameObject.SetActive(true);
            yield return StartCoroutine(TypeOutText(stage5Text));
        }
        
        // ★ 隱藏 perfectCircle 在顯示文字期間
        if (rhythmGame != null && rhythmGame.perfectCircle != null)
        {
            rhythmGame.perfectCircle.gameObject.SetActive(false);
        }
        yield return new WaitForSeconds(1.5f);
        sceneController.SetState(SceneController.GameState.TutorialLoading);
        
        // ★ 文字保持顯示，等待玩家準備
        // 可以在這裡加入按鈕或等待輸入的邏輯
        Debug.Log("[Tutorial] 第五幕文字顯示完畢，等待玩家按下開始");
        
        // ★ 設置旗標，讓 SceneController 可以接收按鍵
        isTutorialCompleted = true;
        Debug.Log("[Tutorial] Tutorial 已完成，等待 SceneController 處理按鍵");
        
        // 交還控制權（文字保持顯示）
        // if (sceneController != null)
        // {
        //     // ★ 在交還控制權前，恢復 GameUI 完整狀態
        //     RestoreGameUI();
        //     sceneController.SetState(SceneController.GameState.Gameplay);
        // }
    }

    // ★ 設置 GameUI 為 Tutorial 模式
    void SetupGameUIForTutorial()
    {
        // ★ 開啟 Tutorial Canvas 和它的所有子物件


        if (sceneController != null && sceneController.gameplayUI != null)
        {
            // 1. 確保 GameUI 父物件開啟
            sceneController.gameplayUI.SetActive(true);
            
            // 2. 關閉所有子物件
            foreach (Transform child in sceneController.gameplayUI.transform)
            {
                child.gameObject.SetActive(false);
            }
            
            // 3. 開啟指定的子物件
            if (gameUIElementsToActive != null)
            {
                foreach (var obj in gameUIElementsToActive)
                {
                    if (obj != null) obj.SetActive(true);
                }
            }

            // ★★★ 解決方案：初始化指示器（全部隱藏）★★★
            if (rhythmGame != null)
            {
                if (rhythmGame.directionIndicatorImage != null)
                {
                    // 雖然它會被 HideDirectionIndicator() 關閉，但要確保父物件是開啟的
                    rhythmGame.directionIndicatorImage.transform.parent.gameObject.SetActive(true);
                    rhythmGame.directionIndicatorImage.gameObject.SetActive(false); // 預設先關閉
                }
                if (rhythmGame.timingCircle != null)
                {
                    rhythmGame.timingCircle.gameObject.SetActive(false); // ★ 預設隱藏外圈
                }
                if (rhythmGame.perfectCircle != null)
                {
                    rhythmGame.perfectCircle.gameObject.SetActive(false); // ★ 預設隱藏內圈
                }
            }

            if (textCanvas != null)
            {
                textCanvas.gameObject.SetActive(true);
            }
        }
    }

    // ★ 恢復 GameUI 為 Gameplay 模式
    void RestoreGameUI()
    {
        // ★ 關閉 Tutorial Canvas
        if (textCanvas != null)
        {
            textCanvas.gameObject.SetActive(false);
        }

        // if (sceneController != null && sceneController.gameplayUI != null)
        // {
        //     // 開啟所有子物件
        //     foreach (Transform child in sceneController.gameplayUI.transform)
        //     {
        //         child.gameObject.SetActive(true);
        //     }
        // }
    }

    // ★ 新增方法：等待玩家校正完成
    IEnumerator WaitForCalibration()
    {
        if (rhythmGame == null)
        {
            yield break;
        }

        // 持續檢查直到所有連接的手把都校正完成或超時
        float calibrationTimeout = 999f; // 最多等30秒
        float elapsedTime = 0f;

        while (elapsedTime < calibrationTimeout)
        {
            // 調用 rhythmGame 的校正檢查方法
            if (rhythmGame.AreAllConnectedControllersCalibrated())
            {
                Debug.Log("[Tutorial] 校正完成！");
                break;
            }

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        if (elapsedTime >= calibrationTimeout)
        {
            Debug.LogWarning("[Tutorial] 校正超時，自動繼續");
        }
    }

    void ToggleObjects(List<GameObject> objects, bool active)
    {
        if (objects == null) return;
        foreach (var obj in objects)
        {
            if (obj != null) obj.SetActive(active);
        }
    }

    IEnumerator ShowTextSequence(string text, float duration)
    {
        // ★ 開啟背景
        if (textBackground != null)
        {
            textBackground.SetActive(true);
        }

        // ★ 開啟文字和逐字稿顯示
        if (centerText != null)
        {
            centerText.gameObject.SetActive(true);
            yield return StartCoroutine(TypeOutText(text));
        }

        // ★ 文字顯示完後停頓 2 秒
        yield return new WaitForSecondsRealtime(postTextPauseDuration);

        // ★ 關閉文字和背景
        if (centerText != null)
        {
            centerText.gameObject.SetActive(false);
        }
        if (textBackground != null)
        {
            textBackground.SetActive(false);
        }
    }

    // ★ 逐字稿顯示
    IEnumerator TypeOutText(string text)
    {
        centerText.text = "";
        foreach (char c in text)
        {
            centerText.text += c;
            yield return new WaitForSecondsRealtime(charTypingInterval);
        }
    }

    IEnumerator SingleNotePractice()
    {
        if (rhythmGame == null) yield break;

        // 設置狀態為 Tutorial，允許 Slash
        rhythmGame.currentState = ScratchRhythmGame.GameState.Tutorial;
        
        // ★ 設置單音符教學模式
        rhythmGame.isSingleNoteTutorial = true;

        // ★ 顯示提示文字前先隱藏圓圈指示器
        if (rhythmGame != null)
        {
            if (rhythmGame.perfectCircle != null)
                rhythmGame.perfectCircle.gameObject.SetActive(false);
            if (rhythmGame.timingCircle != null)
                rhythmGame.timingCircle.gameObject.SetActive(false);
        }

        // ★ 顯示提示文字和背景，使用逐字稿
        if (textBackground != null)
        {
            textBackground.SetActive(true);
        }
        if (centerText != null)
        {
            centerText.gameObject.SetActive(true);
            yield return StartCoroutine(TypeOutText(stage3Text));
        }

        // ★ 停頓 2 秒後再生成音符
        yield return new WaitForSecondsRealtime(postTextPauseDuration);

        // 關閉文字和背景
        if (centerText != null) centerText.gameObject.SetActive(false);
        if (textBackground != null) textBackground.SetActive(false);

        // 生成一個向右的音符，飛行時間 3 秒
        float flyTime = 3.0f;
        var target = rhythmGame.SpawnTutorialTarget(ScratchRhythmGame.SlashDirection.DownLeft, flyTime);
        
        // ★ 文字關閉後啟動圓圈指示器（會同時顯示內外圈）
        if (rhythmGame != null)
        {
            rhythmGame.ActivateTimingIndicator(flyTime);
        }
        
        // ★ 立即啟動圓圈指示器（SpawnTutorialTarget 已經處理方向提示）
        if (rhythmGame != null)
        {
            // 這裡不需要再手動調用 ActivateTimingIndicator，因為 SpawnTutorialTarget 內部已經處理了
            // 但為了保險起見，我們確保它被正確設置
            // rhythmGame.ActivateTimingIndicator(flyTime); 
        }

        // 等待直到接近 Perfect Timing (例如剩餘 0.5 秒)
        float arrivalTime = target.flyingStartTime + target.flyingDuration;
        
        while (Time.time < arrivalTime - 0.2f)
        {
            if (target == null || target.isHit) break;
            yield return null;
        }

        if (target != null && !target.isHit)
        {
            // ★ 停止物件飛行，但不暫停時間
            target.isPaused = true;
            
            // ★ 播放 Swing 動畫
            PlayAnimation("swing");
            
            // 顯示 Focus Panel
            if (focusPanel != null) focusPanel.SetActive(true);
            if (focusText != null) focusText.text = "就是現在！\n向左下揮動！";

            // // ★ 等待玩家打擊或超時 (5秒 Realtime)
            // float focusWaitTime = 0f;
            // while (focusWaitTime < 5.0f && (target == null || !target.isHit))
            // {
            //     focusWaitTime += Time.unscaledDeltaTime;
            //     yield return null;
            // }

            // // 隱藏 Focus Panel
            // if (focusPanel != null) focusPanel.SetActive(false);
            
            // ★ 恢復物件飛行（如果沒被擊中）
            // if (target != null && !target.isHit)
            // {
            //     target.isPaused = false;
            // }
        }

        // 等待目標消失或被擊中
        while (target != null && !target.isHit && !target.isMissed)
        {
            rhythmGame.timingCircle.color = Color.green; // 持續顯示圓圈
            yield return null;
        }
        
        // ★ 停止 Swing 動畫
        StopAnimation("swing");
        
        if(target != null && target.isHit) 
            focusPanel.SetActive(false);

        // ★ 隱藏指示器
        if (rhythmGame != null)
        {
            rhythmGame.HideDirectionIndicator();
            rhythmGame.StopTimingIndicator();
        }

        // 稍微等待一下
        yield return new WaitForSeconds(1.0f);
    }

    IEnumerator PracticeRound()
    {
        if (rhythmGame == null) yield break;
        yield return new WaitForSeconds(2.0f);

        if (centerText != null)
        {
            centerText.gameObject.SetActive(true);
        }
        if (textBackground != null)
        {
            textBackground.SetActive(true);
            yield return StartCoroutine(TypeOutText(stage4Text));
        }
        
        yield return new WaitForSeconds(2.0f);
        
        // ★ 隱藏文字和背景
        if (centerText != null)
        {
            centerText.gameObject.SetActive(false);
        }
        if (textBackground != null)
        {
            textBackground.SetActive(false);
        }
        
        // ★ 隱藏 perfectCircle 在練習期間
        if (rhythmGame != null && rhythmGame.perfectCircle != null)
        {
            rhythmGame.perfectCircle.gameObject.SetActive(false);
        }

        // ★ 重複練習循環
        bool shouldContinuePractice = true;
        while (shouldContinuePractice)
        {
            // 設置教學模式標記，防止自動循環
            rhythmGame.isTutorialMode = true;
            
            // ★ 設置 Tutorial 狀態
            rhythmGame.currentState = ScratchRhythmGame.GameState.Tutorial;

            // 訂閱回合結束事件
            bool roundFinished = false;
            System.Action onRoundEnd = () => { 
                roundFinished = true;
                Debug.Log("[Tutorial] 收到回合結束事件，設置 roundFinished = true");
            };
            rhythmGame.OnRoundComplete += onRoundEnd;

            Debug.Log("[Tutorial] 開始 Easy Round");
            // 開始 Easy Round
            rhythmGame.StartNewRound("easy", 60f); // 慢一點的 BPM

            Debug.Log("[Tutorial] 等待回合結束...");
            // 等待回合結束
            int frameCount = 0;
            while (!roundFinished)
            {
                frameCount++;
                if (frameCount % 60 == 0) // 每秒輸出一次
                {
                    Debug.Log($"[Tutorial] 仍在等待回合結束... roundFinished={roundFinished}");
                }
                yield return null;
            }
            // shouldContinuePractice = false; // 預設不再重複
            
            Debug.Log("[Tutorial] 回合結束循環退出，繼續執行");

            // ★ 隱藏指示器
            if (rhythmGame != null)
            {
                rhythmGame.HideDirectionIndicator();
                rhythmGame.StopTimingIndicator();
            }

            // 取消訂閱
            rhythmGame.OnRoundComplete -= onRoundEnd;
            rhythmGame.isTutorialMode = false;
            
            Debug.Log("[Tutorial] 第四幕練習回合結束");
            
            // ★ 顯示詢問對話框：是否再挑戰一次
            yield return new WaitForSeconds(0.5f);
            
            // ★ 重置選擇狀態
            playerMadeChoice = false;
            chooseRepeat = false;
            isWaitingForPlayerChoice = true; // ★ 允許 SceneController 接收按鍵
            
            if (textBackground != null)
                textBackground.SetActive(true);
            if (centerText != null)
            {
                centerText.gameObject.SetActive(true);
                yield return StartCoroutine(TypeOutText("再挑戰一次?\n(A=是 / Y=否)"));
            }
            
            // ★ 等待玩家按鍵選擇（SceneController 會設置這些變數）
            while (!playerMadeChoice)
            {
                // 鍵盤輸入（備用）
                if(Input.GetKeyDown(KeyCode.Space))
                {
                    chooseRepeat = true;
                    playerMadeChoice = true;
                    Debug.Log("[Tutorial] 玩家按下 Space，選擇再挑戰一次");
                }
                else if(Input.GetKeyDown(KeyCode.Return))
                {
                    chooseRepeat = false;
                    playerMadeChoice = true;
                    Debug.Log("[Tutorial] 玩家按下 Return，選擇不再挑戰");
                }
                yield return null;
            }
            
            Debug.Log($"[Tutorial] 玩家選擇完成：chooseRepeat = {chooseRepeat}");
            
            // ★ 隱藏對話框
            if (centerText != null)
                centerText.gameObject.SetActive(false);
            if (textBackground != null)
                textBackground.SetActive(false);
            
            isWaitingForPlayerChoice = false; // ★ 關閉按鍵接收
            shouldContinuePractice = chooseRepeat;
        }
    }

    /// <summary>
    /// 播放指定動畫
    /// </summary>
    void PlayAnimation(string animationType)
    {
        if (animationType == "swing" && swingAnimator != null)
        {
            swingAnimator.gameObject.SetActive(true);
            swingAnimator.SetTrigger("Play");
            Debug.Log("[Tutorial] 播放 Swing 動畫");
        }
        else if (animationType == "cali" && caliAnimator != null)
        {
            caliAnimator.gameObject.SetActive(true);
            caliAnimator.SetTrigger("Play");
            Debug.Log("[Tutorial] 播放 Calibration 動畫");
        }
    }

    /// <summary>
    /// 停止指定動畫
    /// </summary>
    void StopAnimation(string animationType)
    {
        if (animationType == "swing" && swingAnimator != null)
        {
            swingAnimator.SetTrigger("Stop");
            swingAnimator.gameObject.SetActive(false);
            Debug.Log("[Tutorial] 停止 Swing 動畫");
        }
        else if (animationType == "cali" && caliAnimator != null)
        {
            caliAnimator.SetTrigger("Stop");
            caliAnimator.gameObject.SetActive(false);
            Debug.Log("[Tutorial] 停止 Calibration 動畫");
        }
    }

    /// <summary>
    /// 關閉所有動畫引導
    /// </summary>
    void StopAllAnimations()
    {
        if (swingAnimator != null)
        {
            swingAnimator.gameObject.SetActive(false);
        }
        if (caliAnimator != null)
        {
            caliAnimator.gameObject.SetActive(false);
        }
        Debug.Log("[Tutorial] 關閉所有動畫引導");
    }
}
