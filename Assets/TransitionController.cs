using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

public class TransitionController : MonoBehaviour
{
    public static TransitionController Instance;

    [Header("UI 與動畫")]
    public Canvas canvas;                // Mask_Canvas
    public Animator maskAnimator;        // 遮罩動畫
    public float shrinkTime = 1f;        // 縮小動畫時間
    public float expandTime = 1f;        // 放大動畫時間

    private bool isSwitching = false;    // 避免重複切場景

    private void Awake()
    {
        // 單例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 一開始隱藏遮罩
        maskAnimator.gameObject.SetActive(false);
    }

    /// <summary>
    /// 對外公開，用來切換場景
    /// </summary>
    public void ChangeScene(string sceneName)
    {
        if (!isSwitching)
            StartCoroutine(PlayTransition(sceneName));
    }

    IEnumerator PlayTransition(string sceneName)
    {
        isSwitching = true;

        // 🔹 開啟遮罩物件
        maskAnimator.gameObject.SetActive(true);

        // 🔹 播放縮小動畫
        maskAnimator.SetTrigger("Shrink");
        yield return new WaitForSeconds(shrinkTime);

        // 🔹 使用 LoadSceneAsync（非同步）
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // 等待載入到 90%（Unity 的 async 特性）
        while (op.progress < 0.9f)
            yield return null;

        // 🔹 允許啟動場景
        op.allowSceneActivation = true;

        // 必須等待下一幀，讓新場景真正載入完成
        yield return null;

        // 🔹 重新綁定 Canvas Camera（很重要）
        ResetCanvasCamera();

        yield return new WaitForSeconds(1);

        // 🔹 播放放大動畫
        maskAnimator.SetTrigger("Expand");
        yield return new WaitForSeconds(expandTime);

        // 🔹 關閉遮罩，回復正常畫面
        maskAnimator.gameObject.SetActive(false);
        isSwitching = false;
    }

    /// <summary>
    /// 每次切換場景後重新找 MainCamera 並設定到 canvas
    /// </summary>
    private void ResetCanvasCamera()
    {
        Camera newCam = Camera.main;

        if (newCam != null)
        {
            canvas.worldCamera = newCam;
        }
        else
        {
            Debug.LogWarning("⚠ 找不到 MainCamera，Canvas Camera 未重新指定！");
        }
    }
}
