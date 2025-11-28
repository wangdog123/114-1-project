using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class switchScene : MonoBehaviour
{
    [Header("場景切換設置")]
    [Tooltip("要切換到的場景名稱")]
    public string sceneNameToLoad;
    
    [Tooltip("按下此按鍵切換場景")]
    public KeyCode triggerKey = KeyCode.Space;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            LoadTargetScene();
        }
    }

    public void LoadTargetScene()
    {
        if (!string.IsNullOrEmpty(sceneNameToLoad))
        {
            Debug.Log($"正在切換到場景: {sceneNameToLoad}");
            SceneManager.LoadScene(sceneNameToLoad);
        }
        else
        {
            Debug.LogWarning("未設置場景名稱 (Scene Name)！");
        }
    }
}
