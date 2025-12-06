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

    public bool loaded = false;    // 避免重複切場景

    private void OnEnable()
    {
        // 單例模式
        // if (Instance == null)
        // {
        //     Instance = this;
        //     DontDestroyOnLoad(gameObject);
        // }
        // else
        // {
        //     Destroy(gameObject);
        //     return;
        // }

        // // 一開始隱藏遮罩
        // maskAnimator.gameObject.SetActive(false);
        // StartCoroutine(PlayTransition());
    }

    /// <summary>
    /// 對外公開，用來切換場景
    /// </summary>
    // public void ChangeScene(string sceneName)
    // {
    //     if (!isSwitching)
    //         StartCoroutine(PlayTransition(sceneName));
    // }

    IEnumerator PlayTransition()
    {
        // 🔹 開啟遮罩物件
        maskAnimator.gameObject.SetActive(true);

        // 🔹 播放縮小動畫
        maskAnimator.SetTrigger("Shrink");

        // 必須等待下一幀，讓新場景真正載入完成
        yield return null;
        
        loaded = true;

        // 🔹 重新綁定 Canvas Camera（很重要）
        // ResetCanvasCamera();


        // 🔹 播放放大動畫
        maskAnimator.SetTrigger("Expand");
        yield return new WaitForSeconds(expandTime);

        // 🔹 關閉遮罩，回復正常畫面
        maskAnimator.gameObject.SetActive(false);
        loaded = false;
    }

    /// <summary>
    /// 每次切換場景後重新找 MainCamera 並設定到 canvas
    /// </summary>
    // private void ResetCanvasCamera()
    // {
    //     if (canvas == null)
    //     {
    //         Debug.LogWarning("[TransitionController] Canvas reference is null; cannot assign camera.");
    //         return;
    //     }

    //     // 優先使用 Camera.main (需要場景中的相機被標記為 MainCamera)
    //     Camera newCam = Camera.main;

    //     // 若 Camera.main 為 null，嘗試尋找場景中第一個啟用的 Camera
    //     if (newCam == null)
    //     {
    //         Camera[] cams = GameObject.FindObjectsOfType<Camera>();
    //         foreach (var c in cams)
    //         {
    //             if (c != null && c.gameObject.activeInHierarchy)
    //             {
    //                 newCam = c;
    //                 break;
    //             }
    //         }
    //     }

    //     if (newCam != null)
    //     {
    //         // 將 Canvas 設為 ScreenSpace-Camera 並指定 camera
    //         canvas.renderMode = RenderMode.ScreenSpaceCamera;
    //         canvas.worldCamera = newCam;

    //         // 適當設定 plane distance（可視需求調整）
    //         try
    //         {
    //             canvas.planeDistance = 1f;
    //         }
    //         catch { }

    //         Debug.Log($"[TransitionController] Assigned Canvas.worldCamera = {newCam.name} ({newCam.gameObject.scene.name})");
    //     }
    //     else
    //     {
    //         Debug.LogWarning("[TransitionController] 找不到任何相機。請確認場景中有一個啟用的 Camera，或將相機標記為 MainCamera。");
    //     }
    // }
}
