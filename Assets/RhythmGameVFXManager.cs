using UnityEngine;
using System.Collections;

/// <summary>
/// 節奏遊戲 VFX 管理器
/// 使用 Particle System 在目標位置播放特效
/// </summary>
public class RhythmGameVFXManager : MonoBehaviour
{
    [Header("劃動方向 VFX 子物件")]
    [Tooltip("向左劃動的劃痕特效（此 Manager 的子物件，Particle System）")]
    public ParticleSystem leftSlashVFX;
    
    [Tooltip("向右劃動的劃痕特效（此 Manager 的子物件，Particle System）")]
    public ParticleSystem rightSlashVFX;
    
    [Tooltip("左斜下抓取的劃痕特效（此 Manager 的子物件，Particle System）")]
    public ParticleSystem downLeftSlashVFX;
    
    [Tooltip("右斜下抓取的劃痕特效（此 Manager 的子物件，Particle System）")]
    public ParticleSystem downRightSlashVFX;
    
    [Header("額外特效（可選）")]
    [Tooltip("Miss 錯過特效（此 Manager 的子物件，Particle System）")]
    public ParticleSystem missVFX;
    
    [Tooltip("連擊特效（每 10 combo，此 Manager 的子物件，Particle System）")]
    public ParticleSystem comboVFX;
    
    private static RhythmGameVFXManager instance;
    
    public static RhythmGameVFXManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<RhythmGameVFXManager>();
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
    
    /// <summary>
    /// 根據劃動方向播放對應的 VFX（直接調用子物件的 Play）
    /// </summary>
    /// <param name="direction">劃動方向</param>
    /// <param name="position">播放位置（此參數保留但不使用）</param>
    public void SpawnSlashVFX(ScratchRhythmGame.SlashDirection direction, Vector3 position)
    {
        Debug.Log($"[VFX Manager] SpawnSlashVFX 被調用：方向={direction}");
        
        ParticleSystem vfxToPlay = null;
        
        switch (direction)
        {
            case ScratchRhythmGame.SlashDirection.Left:
                vfxToPlay = leftSlashVFX;
                Debug.Log($"[VFX Manager] 選擇左劃 VFX，已設置={leftSlashVFX != null}");
                break;
            case ScratchRhythmGame.SlashDirection.Right:
                vfxToPlay = rightSlashVFX;
                Debug.Log($"[VFX Manager] 選擇右劃 VFX，已設置={rightSlashVFX != null}");
                break;
            case ScratchRhythmGame.SlashDirection.DownLeft:
                vfxToPlay = downLeftSlashVFX;
                Debug.Log($"[VFX Manager] 選擇左斜下抓 VFX，已設置={downLeftSlashVFX != null}");
                break;
            case ScratchRhythmGame.SlashDirection.DownRight:
                vfxToPlay = downRightSlashVFX;
                Debug.Log($"[VFX Manager] 選擇右斜下抓 VFX，已設置={downRightSlashVFX != null}");
                break;
        }
        
        if (vfxToPlay != null)
        {
            Debug.Log($"[VFX Manager] ✓ 直接播放 Particle System：{vfxToPlay.name}");
            PlayVFX(vfxToPlay);
        }
        else
        {
            Debug.LogWarning($"[VFX Manager] ✗ {direction} 方向的劃痕 VFX 未設置！請在 Inspector 中拖入此 Manager 的子物件（Particle System）。");
        }
    }
    
    /// <summary>
    /// 直接播放 Particle System（不移動位置）
    /// </summary>
    private void PlayVFX(ParticleSystem vfx)
    {
        if (vfx == null) return;
        
        // 停止當前播放（避免重疊）
        vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        
        // 直接播放 Particle System
        vfx.Play(true);
        
        Debug.Log($"[VFX Manager] ✓ Particle System 已播放：{vfx.name}");
    }
    
    /// <summary>
    /// 播放錯過特效
    /// </summary>
    public void SpawnMissVFX(Vector3 position)
    {
        if (missVFX != null)
        {
            PlayVFX(missVFX);
        }
    }
    
    /// <summary>
    /// 播放連擊特效
    /// </summary>
    public void SpawnComboVFX(int combo, Vector3 position)
    {
        // 每 10 連擊播放一次特效
        if (combo % 10 == 0 && comboVFX != null)
        {
            PlayVFX(comboVFX);
        }
    }
}

