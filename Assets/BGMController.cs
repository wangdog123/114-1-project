using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMController : MonoBehaviour
{
    public static BGMController Instance { get; private set; }

    [Header("BGM 設定")]
    public AudioSource audioSource;
    public AudioClip menuBGM; // 選單/Prologue/Ending 等階段的 BGM
    public AudioClip gameplayBGM; // Gameplay 階段的 BGM

    private AudioClip currentBGM;
    private float originalVolume = 1f;

    void Awake()
    {
        // Singleton 模式 - 確保整個遊戲只有一個 BGMController
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[BGMController] BGM Controller 已建立並設為跨場景保留");
    }

    void OnEnable()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            originalVolume = audioSource.volume;
            currentBGM = audioSource.clip;
        }
    }

    /// <summary>
    /// 播放選單 BGM (Prologue/Ending/ScoreDisplay 等)
    /// </summary>
    public void PlayMenuBGM()
    {
        if (audioSource == null || menuBGM == null) return;

        if (currentBGM != menuBGM)
        {
            audioSource.volume = originalVolume;
            audioSource.clip = menuBGM;
            audioSource.Play();
            currentBGM = menuBGM;
            Debug.Log("[BGMController] 切換到選單 BGM");
        }
    }

    /// <summary>
    /// 播放遊戲 BGM (Gameplay)
    /// </summary>
    public void PlayGameplayBGM()
    {
        if (audioSource == null || gameplayBGM == null) return;

        if (currentBGM != gameplayBGM)
        {
            audioSource.volume = 0.2f;
            audioSource.clip = gameplayBGM;
            audioSource.Play();
            currentBGM = gameplayBGM;
            Debug.Log("[BGMController] 切換到 Gameplay BGM");
        }
    }

    /// <summary>
    /// 設定音量
    /// </summary>
    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = volume;
        }
    }

    /// <summary>
    /// 恢復原始音量
    /// </summary>
    public void RestoreVolume()
    {
        if (audioSource != null)
        {
            audioSource.volume = originalVolume;
        }
    }

    /// <summary>
    /// 靜音
    /// </summary>
    public void Mute()
    {
        SetVolume(0f);
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    public void Stop()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// 暫停播放
    /// </summary>
    public void Pause()
    {
        if (audioSource != null)
        {
            audioSource.Pause();
        }
    }

    /// <summary>
    /// 繼續播放
    /// </summary>
    public void Resume()
    {
        if (audioSource != null)
        {
            audioSource.UnPause();
        }
    }
}
