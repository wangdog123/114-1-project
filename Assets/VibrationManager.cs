using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.Switch;

/// <summary>
/// 震動管理器 - 管理不同情況的震動回饋
/// </summary>
public class VibrationManager : MonoBehaviour
{
    [Header("控制器設定")]
    public MultiSwitchControllerManager controllerManager;
    
    [Header("震動 Profile - 擊中目標")]
    [Tooltip("Perfect 擊中的震動")]
    public VibrationProfile perfectHitProfile = new VibrationProfile(300, 0.8f, 0.6f, 0.15f);
    
    [Tooltip("Good 擊中的震動")]
    public VibrationProfile goodHitProfile = new VibrationProfile(250, 0.6f, 0.4f, 0.12f);
    
    [Tooltip("OK 擊中的震動")]
    public VibrationProfile okHitProfile = new VibrationProfile(200, 0.4f, 0.3f, 0.1f);
    
    [Header("震動 Profile - 錯誤/懲罰")]
    [Tooltip("Miss 或被飛行物打到的震動")]
    public VibrationProfile missHitProfile = new VibrationProfile(400, 1.0f, 0.8f, 0.2f);
    
    [Header("震動設定")]
    [Tooltip("是否啟用震動")]
    public bool vibrationEnabled = true;
    
    [Tooltip("全域震動強度倍數 (0-1)")]
    [Range(0f, 1f)]
    public float globalIntensity = 1.0f;
    
    private static VibrationManager instance;
    
    public static VibrationManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<VibrationManager>();
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 自動尋找控制器管理器
        if (controllerManager == null)
        {
            controllerManager = FindObjectOfType<MultiSwitchControllerManager>();
        }
        
        if (controllerManager == null)
        {
            Debug.LogWarning("[VibrationManager] 找不到 MultiSwitchControllerManager，震動功能將無法使用");
        }
    }
    
    /// <summary>
    /// Perfect 擊中震動
    /// </summary>
    [ContextMenu("測試震動/Perfect Hit")]
    public void VibrateOnPerfect(int controllerIndex = -1)
    {
        TriggerVibration(perfectHitProfile, controllerIndex);
        Debug.Log("[Vibration] Perfect Hit!");
    }
    
    /// <summary>
    /// Good 擊中震動
    /// </summary>
    [ContextMenu("測試震動/Good Hit")]
    public void VibrateOnGood(int controllerIndex = -1)
    {
        TriggerVibration(goodHitProfile, controllerIndex);
        Debug.Log("[Vibration] Good Hit!");
    }
    
    /// <summary>
    /// OK 擊中震動
    /// </summary>
    [ContextMenu("測試震動/OK Hit")]
    public void VibrateOnOK(int controllerIndex = -1)
    {
        TriggerVibration(okHitProfile, controllerIndex);
        Debug.Log("[Vibration] OK Hit!");
    }
    
    /// <summary>
    /// Miss 或被飛行物打到的震動
    /// </summary>
    [ContextMenu("測試震動/Miss")]
    public void VibrateOnMiss(int controllerIndex = -1)
    {
        TriggerVibration(missHitProfile, controllerIndex);
        Debug.Log("[Vibration] Miss/Hit by object!");
    }
    
    /// <summary>
    /// 使用自訂 Profile 觸發震動
    /// </summary>
    public void TriggerCustomVibration(VibrationProfile profile, int controllerIndex = -1)
    {
        TriggerVibration(profile, controllerIndex);
    }
    
    /// <summary>
    /// 核心震動觸發方法
    /// </summary>
    private void TriggerVibration(VibrationProfile profile, int controllerIndex)
    {
        if (!vibrationEnabled || controllerManager == null)
            return;
        
        // 應用全域強度倍數
        float lowFreq = profile.lowFrequency * globalIntensity;
        float highFreq = profile.highFrequency * globalIntensity;
        
        if (controllerIndex >= 0)
        {
            // 震動指定的控制器
            SwitchControllerHID controller = controllerManager.GetController(controllerIndex);
            if (controller != null)
            {
                StartCoroutine(VibrateController(controller, profile.frequency, lowFreq, highFreq, profile.duration));
            }
        }
        else
        {
            // 震動所有控制器
            for (int i = 0; i < controllerManager.ControllerCount; i++)
            {
                SwitchControllerHID controller = controllerManager.GetController(i);
                if (controller != null)
                {
                    StartCoroutine(VibrateController(controller, profile.frequency, lowFreq, highFreq, profile.duration));
                }
            }
        }
    }
    
    /// <summary>
    /// 控制器震動協程
    /// </summary>
    private IEnumerator VibrateController(SwitchControllerHID controller, float frequency, float lowFreq, float highFreq, float duration)
    {
        // 使用 Switch Controller 的 Rumble Profile
        SwitchControllerRumbleProfile profile = SwitchControllerRumbleProfile.CreateEmpty();
        
        // 設定高頻和低頻震動
        profile.highBandAmplitudeLeft = highFreq;
        profile.highBandAmplitudeRight = highFreq;
        profile.highBandFrequencyLeft = frequency;
        profile.highBandFrequencyRight = frequency;
        
        profile.lowBandAmplitudeLeft = lowFreq;
        profile.lowBandAmplitudeRight = lowFreq;
        profile.lowBandFrequencyLeft = frequency;
        profile.lowBandFrequencyRight = frequency;
        
        // 開始震動
        controller.Rumble(profile);
        
        // 等待震動持續時間
        yield return new WaitForSeconds(duration);
        
        // 停止震動
        controller.Rumble(SwitchControllerRumbleProfile.CreateNeutral());
    }
    
    /// <summary>
    /// 立即停止所有震動
    /// </summary>
    [ContextMenu("測試震動/停止所有震動")]
    public void StopAllVibrations()
    {
        if (controllerManager == null)
            return;
        
        StopAllCoroutines();
        
        SwitchControllerRumbleProfile neutralProfile = SwitchControllerRumbleProfile.CreateNeutral();
        
        for (int i = 0; i < controllerManager.ControllerCount; i++)
        {
            SwitchControllerHID controller = controllerManager.GetController(i);
            if (controller != null)
            {
                controller.Rumble(neutralProfile);
            }
        }
    }
    
    void OnDestroy()
    {
        StopAllVibrations();
    }
}

/// <summary>
/// 震動 Profile 設定
/// </summary>
[System.Serializable]
public class VibrationProfile
{
    [Tooltip("震動頻率 (Hz)")]
    public float frequency = 320f;
    
    [Tooltip("低頻震動強度 (0-1)")]
    [Range(0f, 1f)]
    public float lowFrequency = 0.5f;
    
    [Tooltip("高頻震動強度 (0-1)")]
    [Range(0f, 1f)]
    public float highFrequency = 0.5f;
    
    [Tooltip("震動持續時間（秒）")]
    public float duration = 0.1f;
    
    public VibrationProfile(float freq, float low, float high, float dur)
    {
        frequency = freq;
        lowFrequency = low;
        highFrequency = high;
        duration = dur;
    }
}
