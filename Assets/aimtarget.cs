// using UnityEngine;
// using Cinemachine;
// using Unity.VisualScripting;
// using System.Collections.Generic;
// using System.Reflection;
// using UnityEngine.Rendering;

// public class aimtarget : MonoBehaviour
// {
//     // 三個固定目標
//     public GameObject leftTarget;
//     public GameObject centerTarget;
//     public GameObject rightTarget;
    
//     // 視角微調用的虛擬目標
//     private GameObject lookAroundTarget;
    
//     private CinemachineCam cam;
//     public Scope scope;
    
//     // 目標選擇狀態
//     public enum TargetPosition { Center, Left, Right }
//     public TargetPosition currentTarget = TargetPosition.Center;
    
//     [Header("Camera Zoom Settings")]
//     [Tooltip("手動控制中間目標的鏡頭距離")]
//     [Range(0.5f, 10f)]
//     public float manualZoomWidthCenter = 6.5f; // 中間目標的手動 Width
    
//     [Tooltip("手動控制左右目標的鏡頭距離（限制在近距離）")]
//     [Range(1f, 2f)]
//     public float manualZoomWidthSide = 1.5f; // 左右目標的手動 Width
    
//     public bool useManualZoom = false; // 是否使用手動控制
    
//     public float zoomedInWidth = 1.0f;
//     public float zoomedOutWidth = 6.5f;
//     public bool isZooming = false; // 是否正在縮放中
//     public float zoomDuration = 0.5f; // 縮放持續時間
//     private float zoomTimer = 0f; // 縮放計時器
    
//     // 視角微調參數
//     public float lookAroundRange = 2f; // 可以偏移的最大範圍
//     public float lookAroundSensitivity = 0.05f; // 滑鼠靈敏度
//     private Vector3 currentLookOffset = Vector3.zero; // 當前視角偏移
//     private GameObject currentTargetObject; // 當前追蹤的目標物件
    
//     // 命中率系統
//     [Header("Accuracy Circle Settings")]
//     public GameObject accuracyCircle; // 命中率圓圈 UI
//     public float minCircleSize = 0.3f; // 最小圓圈大小（精準）
//     public float maxCircleSize = 1.5f; // 最大圓圈大小
    
//     [Header("Breath Speed Settings")]
//     [Tooltip("基礎呼吸速度（可調整範圍）")]
//     [Range(0.5f, 5f)]
//     public float breathSpeed = 1.5f; // 基礎呼吸速度
    
//     [Tooltip("心率對呼吸速度的影響程度 (0=無影響, 1=完全影響)")]
//     [Range(0f, 1f)]
//     public float breathSpeedHeartRateInfluence = 0.5f; // 心率影響呼吸速度的程度
    
//     [Tooltip("實際呼吸速度（受心率影響後，唯讀）")]
//     [Range(0.8f, 2f)]
//     public float adjustedBreathSpeed = 1.5f; // 調整後的呼吸速度（顯示用）
    
//     private float currentCircleSize = 1f; // 當前圓圈大小
//     private bool isCircleGrowing = true; // 圓圈是否正在變大
//     private MaterialPropertyBlock circlePropertyBlock; // 用於修改材質顏色而不創建新材質實例
    
//     // 命中率閾值
//     public float perfectHitThreshold = 0.5f; // 精準命中：圓圈小於此值
//     public float normalHitThreshold = 1.0f; // 普通命中：圓圈小於此值
//     // 大於 normalHitThreshold = 未命中
    
//     public enum HitAccuracy { Miss, Hit, Perfect }
    
//     // Depth of Field 設定（Cinemachine）
//     [Header("Depth of Field Settings - 近視模擬")]
//     [Tooltip("啟用自動視力調節（關閉則可以手動調整 focusDistance 測試模糊效果）")]
//     public bool enableAutoVisionAdjustment = true; // 是否啟用自動調節
    
//     [Tooltip("基礎對焦距離 - 平常放鬆時的視力狀態 (建議 3-6，模糊區間)")]
//     [Range(0.1f, 15f)]
//     public float focusDistance = 4f; // 基礎焦距（放鬆時看不太清楚）
    
//     [Tooltip("用力看時最清晰的焦距 (建議 7-10，稍微看清楚但還是不夠清晰)")]
//     [Range(5f, 15f)]
//     public float maxFocusDistance = 9f; // 用力看時的焦距（努力看還是模糊）
    
//     [Tooltip("放鬆/疲勞時最模糊的焦距 (建議 1-4，更模糊)")]
//     [Range(0.1f, 10f)]
//     public float minFocusDistance = 2f; // 疲勞時的焦距（放棄掙扎時）
    
//     [Tooltip("視力調節強度 - 控制用力看的效果 (0=完全放鬆, 1=很努力在看)")]
//     [Range(0f, 1f)]
//     public float visionStrainIntensity = 0.6f; // 努力看的強度
    
//     [Tooltip("視力調節速度 - 眨眼/擠眼睛的頻率 (0.05=很慢像呼吸, 0.5=正常眨眼)")]
//     [Range(0.05f, 1f)]
//     public float visionStrainSpeed = 0.15f; // 調節速度（慢慢的，不是每秒都變）
    
//     [Tooltip("視力變化平滑度 (越大越平滑，5-10 推薦)")]
//     [Range(1f, 20f)]
//     public float visionSmoothness = 8f; // 平滑度
    
//     private CinemachineVolumeSettings volumeSettings;
//     private VolumeProfile volumeProfile;
//     private VolumeComponent depthOfFieldComponent;
//     private System.Reflection.FieldInfo focusDistanceParameter;
    
//     // 心跳系統
//     [Header("Heartbeat System - 輕度影響")]
//     public BPMLISTENER bpmListener; // BPM 監聽器（自動獲取心率）
//     [Tooltip("手動心率設定 (60-120，遊戲中預期範圍)")]
//     [Range(0f, 180f)]
//     public float heartRate = 40f; // 心跳值（遊戲中大約 60-120）
    
//     [Tooltip("心率對視力的影響程度 (0=無影響, 1=心跳越快越模糊/越想看清楚)")]
//     [Range(0f, 1f)]
//     public float heartRateInfluence = 0.3f; // 心率影響（輕微）
    
//     private float heartbeatTimer = 0f;
//     private float currentFocusDistance = 4f;
//     private float targetFocusDistance = 4f;
//     private float smoothedHeartbeatEffect = 0f;
//     private float currentHeartRate = 70f;
    
//     // ===== 舊的瞄準射擊系統（已註解） =====
//     /*
//     private List<GameObject> targets = new List<GameObject>();
//     public GameObject target_notaiming;
//     public GameObject bulletPrefab;
//     public bool isAiming = false;
//     */
    
//     void Start()
//     {
//         cam = GetComponent<CinemachineCamera>();
//         scope.enabled = false;
        
//         // 初始化 MaterialPropertyBlock
//         circlePropertyBlock = new MaterialPropertyBlock();
        
//         // 創建視角微調用的虛擬目標
//         lookAroundTarget = new GameObject("LookAroundTarget");
        
//         // 設置 Follow Zoom Extension
//         SetupFollowZoom();
        
//         // 初始化 Depth of Field
//         SetupDepthOfField();
        
//         // 隱藏命中率圓圈（只在左右目標時顯示）
//         if (accuracyCircle != null)
//         {
//             accuracyCircle.SetActive(false);
//         }
        
//         // 預設追蹤中間目標
//         if (centerTarget != null)
//         {
//             cam.Target.TrackingTarget = centerTarget.transform;
//             currentTarget = TargetPosition.Center;
//             currentTargetObject = centerTarget;
//         }
        
//         // ===== 舊的初始化（已註解） =====
//         /*
//         // 初始化時收集所有目標
//         RefreshTargets();
        
//         // 一開始就選擇一個隨機目標跟隨
//         if (targets.Count > 0)
//         {
//             SelectRandomTarget();
//             cam.Target.TrackingTarget = target_notaiming.transform;
//         }
//         */
//     }
    
//     void SetupFollowZoom()
//     {
//         // 獲取或添加 Follow Zoom Extension
//         var followZoom = cam.GetComponent<CinemachineFollowZoom>();
//         if (followZoom == null)
//         {
//             followZoom = cam.AddComponent<CinemachineFollowZoom>();
//         }
        
//         // 設置初始參數
//         followZoom.Width = 6.5f; // 正常視距
//         followZoom.Damping = 1f; // 平滑過渡
//     }
    
//     // 新增：設置 Depth of Field
//     void SetupDepthOfField()
//     {
//         // 從 Cinemachine Camera 獲取 Volume Settings
//         volumeSettings = cam.GetComponent<CinemachineVolumeSettings>();
        
//         if (volumeSettings == null)
//         {
//             Debug.LogWarning("找不到 CinemachineVolumeSettings！請在 Cinemachine Camera 上添加此組件。");
//             return;
//         }
        
//         Debug.Log("成功連接到 Cinemachine Volume Settings！");
        
//         // 獲取 Volume Profile
//         volumeProfile = volumeSettings.Profile;
        
//         if (volumeProfile == null)
//         {
//             Debug.LogWarning("Volume Settings 沒有設定 Profile！");
//             return;
//         }
        
//         Debug.Log($"成功獲取 Volume Profile: {volumeProfile.name}");
        
//         // 找到 Depth of Field 組件
//         foreach (var component in volumeProfile.components)
//         {
//             if (component.GetType().Name.Contains("DepthOfField"))
//             {
//                 depthOfFieldComponent = component;
//                 Debug.Log($"✓ 找到 Depth of Field 組件: {component.GetType().Name}");
                
//                 // 獲取 focusDistance 參數的 FieldInfo
//                 focusDistanceParameter = component.GetType().GetField("focusDistance", 
//                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                
//                 if (focusDistanceParameter != null)
//                 {
//                     Debug.Log("✓ 成功獲取 focusDistance 參數！");
//                 }
//                 else
//                 {
//                     Debug.LogWarning("✗ 找不到 focusDistance 參數");
//                 }
                
//                 break;
//             }
//         }
        
//         if (depthOfFieldComponent == null)
//         {
//             Debug.LogWarning("✗ 找不到 Depth of Field 組件");
//         }
//     }
    
//     // 新增：更新 Depth of Field（模擬近視想看清楚的掙扎）
//     void UpdateDepthOfField()
//     {
//         if (depthOfFieldComponent == null || focusDistanceParameter == null) return;
        
//         // === 模式切換：手動 vs 自動 ===
//         if (!enableAutoVisionAdjustment)
//         {
//             // 手動模式：直接使用 focusDistance 滑桿的值
//             currentFocusDistance = focusDistance;
            
//             // Debug 輸出（每秒一次）
//             if (Time.frameCount % 60 == 0)
//             {
//                 string visionState = currentFocusDistance < 4f ? "很模糊" : 
//                                      currentFocusDistance < 7f ? "模糊" : 
//                                      currentFocusDistance < 10f ? "稍微清晰" : 
//                                      currentFocusDistance < 12f ? "有點模糊" : "清晰";
//                 Debug.Log($"[手動模式] 焦距: {currentFocusDistance:F2} ({visionState})");
//             }
//         }
//         else
//         {
//             // 自動模式：視力調節模擬
            
//             // 從 BPMListener 獲取心率數據（如果有的話）
//             UpdateHeartRateFromListener();
            
//             // 計算心率對調節速度的影響（心跳快時會更頻繁想看清楚）
//             // 60 BPM = 1.0, 90 BPM = 1.5, 120 BPM = 2.0
//             float heartRateMultiplier = currentHeartRate / 60f;
            
//             // 調整後的調節頻率（慢慢的變化，不是每秒都變）
//             // 基礎 0.15 配合 70 BPM ≈ 每 5-6 秒完整循環一次
//             float adjustedSpeed = visionStrainSpeed * heartRateMultiplier * heartRateInfluence;
//             heartbeatTimer += Time.deltaTime * adjustedSpeed;
            
//             // 正弦波模擬努力看 → 放鬆 → 努力看的循環
//             // 正弦波範圍 -1 到 1，轉換為 0 到 1
//             float visionStrainWave = (Mathf.Sin(heartbeatTimer * Mathf.PI * 2f) + 1f) * 0.5f;
            
//             // 在最模糊（放鬆/疲勞）和稍清晰（用力看）之間插值
//             // 0 = minFocusDistance (最模糊，放棄了)
//             // 1 = maxFocusDistance (用力看，稍微清楚一點但還是看不太清)
//             float targetFocus = Mathf.Lerp(minFocusDistance, maxFocusDistance, visionStrainWave);
            
//             // 根據「努力程度」混合基礎焦距和調節焦距
//             // visionStrainIntensity = 0：完全放鬆，維持基礎模糊
//             // visionStrainIntensity = 1：一直在努力想看清楚
//             targetFocusDistance = Mathf.Lerp(focusDistance, targetFocus, visionStrainIntensity);
            
//             // 第一層平滑：讓目標變化更緩和（模擬眼睛調節不是瞬間的）
//             smoothedHeartbeatEffect = Mathf.Lerp(smoothedHeartbeatEffect, targetFocusDistance, Time.deltaTime * 2f);
            
//             // 第二層平滑：最終應用到焦距（讓整體感覺很柔和）
//             float smoothSpeed = Time.deltaTime * visionSmoothness;
//             currentFocusDistance = Mathf.Lerp(currentFocusDistance, smoothedHeartbeatEffect, smoothSpeed);
            
//             // Debug 輸出（每秒一次）
//             if (Time.frameCount % 60 == 0)
//             {
//                 string heartRateSource = (bpmListener != null && bpmListener.bpmText != null) ? "自動" : "手動";
//                 string visionState = currentFocusDistance < 4f ? "很模糊(疲勞)" : 
//                                      currentFocusDistance < 7f ? "模糊(放鬆)" : 
//                                      currentFocusDistance < 10f ? "努力在看(稍清晰)" : "用力看(仍有點模糊)";
//                 Debug.Log($"[自動模式] 心率: {currentHeartRate:F1} BPM ({heartRateSource}) | 焦距: {currentFocusDistance:F2} ({visionState}) | 努力程度: {visionStrainIntensity:F2}");
//             }
//         }
        
//         // 最終限制在合理範圍內
//         currentFocusDistance = Mathf.Clamp(currentFocusDistance, 0.1f, 15f);
        
//         try
//         {
//             // 獲取 focusDistance 參數對象
//             var focusDistanceObj = focusDistanceParameter.GetValue(depthOfFieldComponent);
            
//             if (focusDistanceObj != null)
//             {
//                 var paramType = focusDistanceObj.GetType();
                
//                 // 設定 overrideState 為 true
//                 var overrideStateField = paramType.GetField("m_OverrideState", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
//                 if (overrideStateField == null)
//                 {
//                     overrideStateField = paramType.GetField("overrideState", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
//                 }
                
//                 if (overrideStateField != null)
//                 {
//                     overrideStateField.SetValue(focusDistanceObj, true);
//                 }
                
//                 // 設定 value
//                 var valueField = paramType.GetField("m_Value", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
//                 if (valueField == null)
//                 {
//                     valueField = paramType.GetField("value", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
//                 }
                
//                 if (valueField != null)
//                 {
//                     valueField.SetValue(focusDistanceObj, currentFocusDistance);
                    
//                     if (Time.frameCount % 60 == 0)
//                     {
//                         Debug.Log($"✓ 成功設定 focusDistance = {currentFocusDistance:F2}");
//                     }
//                 }
//                 else
//                 {
//                     if (Time.frameCount % 60 == 0)
//                     {
//                         Debug.LogWarning($"✗ 找不到 value 字段。參數類型: {paramType.Name}");
//                     }
//                 }
//             }
//         }
//         catch (System.Exception e)
//         {
//             if (Time.frameCount % 60 == 0)
//             {
//                 Debug.LogWarning($"設定 Focus Distance 時發生錯誤: {e.Message}");
//             }
//         }
//     }
    
//     // 新增：從 BPMListener 更新心率
//     void UpdateHeartRateFromListener()
//     {
//         // 如果有 BPMListener 且有顯示文字
//         if (bpmListener != null && bpmListener.bpmText != null)
//         {
//             string bpmText = bpmListener.bpmText.text;
            
//             // 嘗試解析 BPM 數值
//             if (float.TryParse(bpmText, out float bpmValue))
//             {
//                 // 如果成功解析且數值合理（40-180 之間）
//                 if (bpmValue >= 40f && bpmValue <= 180f)
//                 {
//                     // 平滑過渡到新的心率值
//                     currentHeartRate = Mathf.Lerp(currentHeartRate, bpmValue, Time.deltaTime * 2f);
//                     return;
//                 }
//             }
//         }
        
//         // 如果沒有 BPMListener 或無法獲取數據，使用手動設定的心率
//         currentHeartRate = Mathf.Lerp(currentHeartRate, heartRate, Time.deltaTime * 2f);
//     }

//     // Update is called once per frame
//     void Update()
//     {
//         // 檢查縮放狀態
//         CheckZoomStatus();
        
//         // 新的目標切換系統
//         HandleTargetSelection();
        
//         // 手動控制鏡頭距離（縮放動畫結束後生效）
//         if (useManualZoom && !isZooming)
//         {
//             var followZoom = cam.GetComponent<CinemachineFollowZoom>();
//             if (followZoom != null)
//             {
//                 // 根據目標位置使用不同的手動值
//                 if (currentTarget == TargetPosition.Center)
//                 {
//                     followZoom.Width = manualZoomWidthCenter; // 中間用遠距離
//                 }
//                 else
//                 {
//                     followZoom.Width = manualZoomWidthSide; // 左右用近距離（限制 1-2）
//                 }
//             }
//         }
        
//         // 處理視角微調
//         HandleLookAround();
        
//         // 更新命中率圓圈
//         UpdateAccuracyCircle();
        
//         // 更新 Depth of Field（受心跳影響）
//         UpdateDepthOfField();
        
//         // ===== 舊的瞄準射擊系統（已註解） =====
//         /*
//         if (!isAiming && !isZooming && Input.GetMouseButton(0))
//         {
//             // 開始瞄準
//             StartAiming();
//         }
//         else if (isAiming && Input.GetMouseButtonUp(0))
//         {
//             // 停止瞄準
//             StopAiming();
//         }
        
//         if (target_notaiming == null) //未加入onhit測試
//         {
//             RefreshTargets();
//             SelectRandomTarget();
//             cam.Target.TrackingTarget = target_notaiming.transform;
//         }
//         */
//     }
    
//     // 新增：處理視角微調
//     void HandleLookAround()
//     {
//         // 只有在左右目標且不在縮放中時才能微調視角
//         if (currentTarget != TargetPosition.Center && !isZooming && currentTargetObject != null)
//         {
//             // 獲取滑鼠移動
//             float mouseX = Input.GetAxis("Mouse X") * lookAroundSensitivity;
//             float mouseY = Input.GetAxis("Mouse Y") * lookAroundSensitivity;
            
//             // 累加偏移量
//             currentLookOffset.x += mouseX;
//             currentLookOffset.y += mouseY;
            
//             // 限制偏移範圍
//             currentLookOffset.x = Mathf.Clamp(currentLookOffset.x, -lookAroundRange, lookAroundRange);
//             currentLookOffset.y = Mathf.Clamp(currentLookOffset.y, -lookAroundRange, lookAroundRange);
            
//             // 更新虛擬目標位置
//             lookAroundTarget.transform.position = currentTargetObject.transform.position + currentLookOffset;
//         }
//     }
    
//     // 新增：處理目標選擇
//     void HandleTargetSelection()
//     {
//         // 按左鍵切換到左邊目標
//         if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
//         {
//             SwitchToTarget(TargetPosition.Left);
//         }
//         // 按右鍵切換到右邊目標
//         else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
//         {
//             SwitchToTarget(TargetPosition.Right);
//         }
        
//         // 攻擊（用空白鍵模擬）
//         if (Input.GetKeyDown(KeyCode.Space))
//         {
//             Attack();
//         }
//     }
    
//     // 新增：切換目標
//     void SwitchToTarget(TargetPosition newTarget)
//     {
//         if (currentTarget == TargetPosition.Center && !isZooming)
//         {
//             currentTarget = newTarget;
//             GameObject targetObj = (newTarget == TargetPosition.Left) ? leftTarget : rightTarget;
            
//             if (targetObj != null)
//             {
//                 currentTargetObject = targetObj;
                
//                 // 重置視角偏移
//                 currentLookOffset = Vector3.zero;
                
//                 // 設定虛擬目標位置
//                 lookAroundTarget.transform.position = targetObj.transform.position;
                
//                 // 先移動鏡頭到虛擬目標
//                 cam.Target.TrackingTarget = lookAroundTarget.transform;
                
//                 // 開始縮放
//                 isZooming = true;
//                 zoomTimer = 0f;
                
//                 // 放大到指定的 zoomedInWidth（切換時先用自動值）
//                 var followZoom = cam.GetComponent<CinemachineFollowZoom>();
//                 if (followZoom != null)
//                 {
//                     followZoom.Width = zoomedInWidth; // 拉近
//                     // 同步更新左右目標的手動值，避免縮放完被拉回舊值
//                     manualZoomWidthSide = zoomedInWidth;
//                 }
                
//                 Debug.Log($"切換到 {newTarget} 目標，可以用滑鼠微調視角");
//             }
//         }
//     }
    
//     // 新增：攻擊
//     void Attack()
//     {
//         if (currentTarget != TargetPosition.Center && !isZooming)
//         {
//             // 判斷命中率
//             HitAccuracy accuracy = GetCurrentAccuracy();
            
//             string accuracyText = "";
//             switch (accuracy)
//             {
//                 case HitAccuracy.Perfect:
//                     accuracyText = "精準命中！🎯";
//                     break;
//                 case HitAccuracy.Hit:
//                     accuracyText = "命中！";
//                     break;
//                 case HitAccuracy.Miss:
//                     accuracyText = "未命中...";
//                     break;
//             }
            
//             Debug.Log($"攻擊 {currentTarget} 目標！{accuracyText} (圓圈大小: {currentCircleSize:F2})");
            
//             // 這裡可以根據命中率做不同的處理
//             // 例如：造成不同傷害、播放不同音效等
            
//             // 攻擊後回到中間
//             ReturnToCenter();
//         }
//     }
    
//     // 新增：獲取當前命中率
//     HitAccuracy GetCurrentAccuracy()
//     {
//         if (currentCircleSize <= perfectHitThreshold)
//         {
//             return HitAccuracy.Perfect; // 精準命中
//         }
//         else if (currentCircleSize <= normalHitThreshold)
//         {
//             return HitAccuracy.Hit; // 普通命中
//         }
//         else
//         {
//             return HitAccuracy.Miss; // 未命中
//         }
//     }
    
//     // 新增：更新命中率圓圈
//     void UpdateAccuracyCircle()
//     {
//         // 只有在左右目標且不在縮放中時才顯示和更新圓圈
//         if (currentTarget != TargetPosition.Center && !isZooming)
//         {
//             if (accuracyCircle != null)
//             {
//                 // 顯示圓圈
//                 if (!accuracyCircle.activeSelf)
//                 {
//                     accuracyCircle.SetActive(true);
//                     // 重置圓圈大小
//                     currentCircleSize = maxCircleSize;
//                     isCircleGrowing = false;
//                 }
                
//                 // 更新圓圈大小（呼吸效果，受心率影響）
//                 // 計算心率對呼吸速度的影響倍數（60 BPM = 1.0x, 120 BPM = 2.0x）
//                 float heartRateMultiplier = Mathf.Lerp(1f, currentHeartRate / 60f, breathSpeedHeartRateInfluence);
//                 adjustedBreathSpeed = breathSpeed * heartRateMultiplier; // 更新顯示欄位
                
//                 // Debug 輸出（每秒一次）
//                 if (Time.frameCount % 60 == 0)
//                 {
//                     Debug.Log($"[圓圈呼吸] 心率: {currentHeartRate:F1} BPM | 倍數: {heartRateMultiplier:F2}x | " +
//                               $"基礎速度: {breathSpeed:F2} | 調整後速度: {adjustedBreathSpeed:F2} | " +
//                               $"影響程度: {breathSpeedHeartRateInfluence:F2}");
//                 }
                
//                 if (isCircleGrowing)
//                 {
//                     currentCircleSize += adjustedBreathSpeed * Time.deltaTime;
//                     if (currentCircleSize >= maxCircleSize)
//                     {
//                         currentCircleSize = maxCircleSize;
//                         isCircleGrowing = false;
//                     }
//                 }
//                 else
//                 {
//                     currentCircleSize -= adjustedBreathSpeed * Time.deltaTime;
//                     if (currentCircleSize <= minCircleSize)
//                     {
//                         currentCircleSize = minCircleSize;
//                         isCircleGrowing = true;
//                     }
//                 }
                
//                 // 應用到圓圈的 Scale
//                 accuracyCircle.transform.localScale = Vector3.one * currentCircleSize;
                
//                 // 可選：根據大小改變顏色
//                 UpdateCircleColor();
//             }
//         }
//         else
//         {
//             // 隱藏圓圈
//             if (accuracyCircle != null && accuracyCircle.activeSelf)
//             {
//                 accuracyCircle.SetActive(false);
//             }
//         }
//     }
    
//     // 新增：根據圓圈大小更新顏色
//     void UpdateCircleColor()
//     {
//         if (accuracyCircle == null) return;
        
//         var spriteRenderer = accuracyCircle.GetComponent<SpriteRenderer>();
//         var image = accuracyCircle.GetComponent<UnityEngine.UI.Image>();
        
//         Color targetColor;
        
//         // 根據當前命中率設定顏色
//         HitAccuracy accuracy = GetCurrentAccuracy();
//         switch (accuracy)
//         {
//             case HitAccuracy.Perfect:
//                 targetColor = Color.green; // 綠色 = 精準
//                 break;
//             case HitAccuracy.Hit:
//                 targetColor = Color.yellow; // 黃色 = 命中
//                 break;
//             case HitAccuracy.Miss:
//                 targetColor = Color.red; // 紅色 = 未命中
//                 break;
//             default:
//                 targetColor = Color.white;
//                 break;
//         }
        
//         // 應用顏色
//         if (spriteRenderer != null)
//         {
//             // 使用 MaterialPropertyBlock 來設定顏色（避免創建材質實例）
//             circlePropertyBlock.SetColor("_Color", targetColor);
//             spriteRenderer.SetPropertyBlock(circlePropertyBlock);
//         }
//         if (image != null)
//         {
//             // UI Image 直接修改顏色
//             image.color = targetColor;
//         }
//     }
    
//     // 新增：回到中間
//     void ReturnToCenter()
//     {
//         currentTarget = TargetPosition.Center;
        
//         if (centerTarget != null)
//         {
//             currentTargetObject = centerTarget;
            
//             // 重置視角偏移
//             currentLookOffset = Vector3.zero;
            
//             // 隱藏命中率圓圈
//             if (accuracyCircle != null)
//             {
//                 accuracyCircle.SetActive(false);
//             }
            
//             // 移動鏡頭回中間
//             cam.Target.TrackingTarget = centerTarget.transform;
            
//             // 開始縮放
//             isZooming = true;
//             zoomTimer = 0f;
            
//             // 恢復到指定的 zoomedOutWidth（切換時先用自動值）
//             var followZoom = cam.GetComponent<CinemachineFollowZoom>();
//             if (followZoom != null)
//             {
//                 followZoom.Width = zoomedOutWidth; // 恢復正常視距
//                 // 同步更新中間目標的手動值，避免縮放完被拉回舊值
//                 manualZoomWidthCenter = zoomedOutWidth;
//             }
            
//             Debug.Log("回到中間目標");
//         }
//     }
    
//     // ===== 舊的瞄準射擊方法（已註解） =====
//     /*
//     void StartAiming()
//     {
//         isAiming = true;
//         isZooming = true;
//         zoomTimer = 0f; // 重置計時器
        
//         scope.gameObject.transform.position = target_notaiming.transform.position;
//         scope.enabled = true;

//         cam.Target.TrackingTarget = target_aiming.transform;
        
//         // 使用 Cinemachine Follow Zoom Extension
//         var followZoom = cam.GetComponent<CinemachineFollowZoom>();
//         if (followZoom != null)
//         {
//             followZoom.Width = 2f; // 調整這個數值來控制拉近程度
//         }
//     }
    
//     void StopAiming()
//     {
//         Shoot();
//         isAiming = false;
//         isZooming = true;
//         zoomTimer = 0f; // 重置計時器
        
//         scope.enabled = false;
//         cam.Target.TrackingTarget = target_notaiming.transform;
        
//         // 恢復正常距離
//         var followZoom = cam.GetComponent<CinemachineFollowZoom>();
//         if (followZoom != null)
//         {
//             followZoom.Width = 5f; // 恢復正常視距
//         }
//     }
//     */
    
//     void CheckZoomStatus()
//     {
//         if (isZooming)
//         {
//             // 使用計時器而不是檢測縮放值
//             zoomTimer += Time.deltaTime;
            
//             // 如果計時器達到設定的持續時間，認為縮放完成
//             if (zoomTimer >= zoomDuration)
//             {
//                 isZooming = false;
//                 zoomTimer = 0f;
                
//                 Debug.Log("縮放完成，可以微調視角");
//             }
//         }
//     }
    
//     // ===== 舊的目標管理方法（已註解） =====
//     /*
//     // 刷新目標列表
//     void RefreshTargets()
//     {
//         targets.Clear();
//         GameObject[] foundTargets = GameObject.FindGameObjectsWithTag("Target");
//         targets.AddRange(foundTargets);
//         Debug.Log($"找到 {targets.Count} 個目標");
//     }
    
//     // 選擇隨機目標
//     void SelectRandomTarget()
//     {
//         if (targets.Count > 0)
//         {
//             int randomIndex = Random.Range(0, targets.Count);
//             target_notaiming = targets[randomIndex];
//             Debug.Log($"選擇目標: {target_notaiming.name}");
//         }
//     }
    
//     void Shoot()
//     {
//         // 拿到 Cinemachine 的最終 Camera 狀態
//         var brain = FindFirstObjectByType<CinemachineBrain>();
//         if (brain == null) return;

//         // 使用 Cinemachine 的實際相機狀態（包含 noise 效果）
//         var outputCamera = brain.OutputCamera;
//         if (outputCamera == null) return;
        
//         float bulletSpeed = 1.0f;

//         // 槍口位置
//         Vector3 firePos = transform.position;
//         firePos.z = 0;

//         // 使用 Cinemachine 相機的實際位置和旋轉來計算射擊方向
//         Vector3 cameraPos = outputCamera.transform.position;
//         Quaternion cameraRot = outputCamera.transform.rotation;
        
//         // 在 2D 遊戲中，我們需要計算相機的右方向（考慮 noise 旋轉）
//         Vector3 cameraRight = cameraRot * Vector3.right;
        
//         // 計算從槍口到相機右方向某個距離點的射擊方向
//         // 在 2D 遊戲中，子彈應該沿著相機的右方向射出
//         Vector3 targetPoint = cameraPos + cameraRight * 10f; // 10f 是射擊距離
//         targetPoint.z = firePos.z; // 確保 Z 軸一致
        
//         Vector3 direction = (targetPoint - firePos).normalized;

//         // 生成子彈
//         GameObject bullet = Instantiate(bulletPrefab, firePos, Quaternion.identity);

//         Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
//         if (rb != null)
//         {
//             rb.linearVelocity = direction * bulletSpeed;
//         }
//     }
    
//     // 移除被擊中的目標並切換到新目標
//     public void OnTargetHit()
//     {
//         if (target_notaiming != null)
//         {
//             targets.Remove(target_notaiming);
//             Debug.Log($"移除目標: {target_notaiming.name}, 剩餘: {targets.Count}");

//             // 如果還有目標，選擇新目標
//             if (targets.Count > 0)
//             {
//                 SelectRandomTarget();
//                 cam.Target.TrackingTarget = target_notaiming.transform;
//             }
//             else
//             {
//                 Debug.Log("所有目標已被消滅！");
//             }
//         }
//     }
//     */
// }
