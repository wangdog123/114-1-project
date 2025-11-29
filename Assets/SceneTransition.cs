using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public Animator maskAnimator;   // 連結 Mask_Canvas 的 Animator
    public float shrinkTime = 1f;   // 縮小動畫的長度
    public float expandTime = 1f;   // 放大動畫的長度

    public void ChangeScene(string sceneName)
    {
        StartCoroutine(PlayTransition(sceneName));
    }

    IEnumerator PlayTransition(string sceneName)
    {
        // 1️⃣ 播放縮小動畫
        maskAnimator.SetTrigger("Shrink");
        yield return new WaitForSeconds(shrinkTime);

        // 2️⃣ 換場
        SceneManager.LoadScene(sceneName);
        yield return null; // 保證下一場景載入完成

        // 3️⃣ 播放放大動畫
        maskAnimator.SetTrigger("Expand");
        yield return new WaitForSeconds(expandTime);
    }
}
