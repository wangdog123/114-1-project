using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ParallaxManager : MonoBehaviour
{
    public Camera cam;
    public Transform[] layers;

    [Header("Camera Movement")]
    public Transform cameraTargetTransform;
    public float cameraMoveDuration = 5.0f;
    [Range(0, 100)]
    public float stopDistancePercent = 80f; // Percentage of the Z-axis distance to travel. 80% means stopping with 20% distance remaining.
    public bool moveOnStart = false;

    [Header("Dynamic Scaling")]
    [Range(0, 1)]
    public float dynamicScaleFactor = 0.1f; // How much the scale changes based on distance. 0 = no change, 1 = full dynamic scaling.

    [Header("Head Bob & Footsteps")]
    public bool enableHeadBob = true;
    public float bobFrequency = 6f;      // 晃動頻率（腳步速度）
    public float bobAmplitude = 0.05f;   // 晃動幅度
    public bool enableFootsteps = true;
    public System.Collections.Generic.List<AudioClip> footstepSounds; // 腳步聲音效列表
    [Range(0f, 1f)] public float footstepVolume = 0.5f;

    [Header("Hit Stop Settings")]
    public float hitStopDuration = 0.5f; // 被打到時停止的時間
    private float currentHitStopTimer = 0f;

    private float bobTimer = 0f;
    private bool stepPlayed = false;
    private AudioSource footstepAudioSource;
    private Volume dizzyVolume;
    [Header("--- 暈眩效果設定 ---")]
    [Tooltip("暈眩持續時間 (秒)")]
    public float dizzyDuration = 2.0f;

    [Tooltip("整體晃動速度 (頻率)：數值越高晃得越快")]
    public float dizzySpeed = 0.8f;

    [Header("--- 晃動強度設定 (角度) ---")]
    [Tooltip("X軸晃動強度 (上下看)：模擬點頭/抬頭的晃動幅度")]
    public float dizzyStrengthX = 8.0f;

    [Tooltip("Y軸晃動強度 (左右看)：模擬搖頭的晃動幅度")]
    public float dizzyStrengthY = 5.0f;

    [Header("--- 其他設定 ---")]
    [Tooltip("淡入時間 (秒)")]
    public float fadeInDuration = 0.2f;
    [Tooltip("結束後的恢復時間 (秒)")]
    public float recoveryDuration = 0.5f;
    [Tooltip("暈眩圓周振幅的小型隨機抖動 (度數)，用於避免完全固定但不改變主要幅度)")]
    public float amplitudeJitter = 0.3f;

    // Store initial scales and distances for each layer
    private Vector3[] _initialScales;
    private float[] _initialDistances;
    private bool _isInitialized = false;

    private Coroutine _cameraMoveCoroutine;
    private bool isDizzy = false; // 是否正在暈眩中

#if UNITY_EDITOR
    // Editor-only variables for camera movement
    private bool _isEditorMoving = false;
    private double _editorMoveStartTime;
    private Vector3 _editorStartPosition;
    private Vector3 _editorTargetPosition;

    // For tracking layer changes in editor
    private Vector3[] _lastLayerPositions;
    private Vector3[] _lastLayerScales;



    void OnEnable()
    {
        // Subscribe to the editor update loop
        EditorApplication.update += EditorUpdate;
        InitializeLastLayerStates();
    }

    void OnDisable()
    {
        // Unsubscribe to prevent errors when the object is deselected or the scene changes
        EditorApplication.update -= EditorUpdate;
    }

    private void InitializeLastLayerStates()
    {
        if (layers == null) return;

        _lastLayerPositions = new Vector3[layers.Length];
        _lastLayerScales = new Vector3[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] != null)
            {
                _lastLayerPositions[i] = layers[i].position;
                // Don't track scale changes since we control them dynamically
            }
        }
        dizzyVolume = FindObjectOfType<Volume>();
    }

    private bool HaveLayersChanged()
    {
        if (layers == null) return false;

        // If the number of layers has changed, we need to re-initialize and update.
        if (_lastLayerPositions == null || _lastLayerPositions.Length != layers.Length)
        {
            InitializeLastLayerStates();
            return true; // Treat as changed
        }

        // Only check for position changes, not scale (since scale is dynamically controlled)
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] != null)
            {
                if (layers[i].position != _lastLayerPositions[i])
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// This method is called by the custom editor script to start the movement.
    /// </summary>
    public void StartEditorCameraMove()
    {
        if (_isEditorMoving || Application.isPlaying)
        {
            return;
        }

        if (cam == null || cameraTargetTransform == null)
        {
            Debug.LogWarning("Camera or Target Transform is not set. Cannot move camera in editor.", this);
            return;
        }

        _editorStartPosition = cam.transform.position;
        Vector3 targetPos = cameraTargetTransform.position;
        
        // Calculate the final Z position based on the percentage
        float startZ = _editorStartPosition.z;
        float finalZ = startZ + (targetPos.z - startZ) * (stopDistancePercent / 100f);
        
        _editorTargetPosition = new Vector3(targetPos.x, targetPos.y, finalZ);
        _editorMoveStartTime = EditorApplication.timeSinceStartup;
        _isEditorMoving = true;
    }
    public void ResetEditorCameraPosition()
    {
        _isEditorMoving = false;
        cam.transform.position = _editorStartPosition;
        SceneView.RepaintAll();
    }

    private void EditorUpdate()
    {
        // --- Live Parallax Update Logic ---
        // Only re-initialize if layers themselves have changed (position/scale), not during camera movement
        if (!_isEditorMoving && !_isInitialized)
        {
            InitializeParallax();
            InitializeLastLayerStates();
        }
        else if (!_isEditorMoving && HaveLayersChanged())
        {
            // If layers changed, re-initialize
            InitializeParallax();
            InitializeLastLayerStates();
        }

        // Always update dynamic scaling in editor to reflect camera position changes
        if (!Application.isPlaying)
        {
            UpdateDynamicScaling();
        }

        // --- Camera Movement Logic ---
        if (!_isEditorMoving || Application.isPlaying)
        {
            return;
        }

        double elapsedTime = EditorApplication.timeSinceStartup - _editorMoveStartTime;
        float progress = (float)(elapsedTime / cameraMoveDuration);

        if (progress < 1.0f)
        {
            cam.transform.position = Vector3.Lerp(_editorStartPosition, _editorTargetPosition, progress);
            // Force the scene view to repaint to show the animation
            SceneView.RepaintAll();
        }
        else
        {
            cam.transform.position = _editorTargetPosition;
            _isEditorMoving = false;
            // Don't re-initialize here, just let UpdateDynamicScaling continue to work
            Debug.Log("Editor camera move finished.");
        }
    }
#endif

    void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
        
        footstepAudioSource = GetComponent<AudioSource>();
        if (footstepAudioSource == null)
            footstepAudioSource = gameObject.AddComponent<AudioSource>();
            
        InitializeParallax();

        if (Application.isPlaying && moveOnStart)
        {
            StartCameraMove();
        }
    }

    void Update()
    {
        // Continuously update parallax scaling based on camera distance
        if (Application.isPlaying)
        {
            UpdateDynamicScaling();
        }

        // For testing in Play Mode: Press 'M' to trigger the camera move.
        if (Input.GetKeyDown(KeyCode.M))
        {
            StartCameraMove();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Reset camera position to the start position
            if (cam != null)
            {
                cam.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
                Debug.Log("Camera position reset to start.");
            }
        }
    }

    /// <summary>
    /// Starts moving the camera to the target position over the specified duration.
    /// </summary>
    public void StartCameraMove()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Camera movement can only be initiated in Play Mode.");
            return;
        }

        if (cameraTargetTransform == null)
        {
            Debug.LogWarning("Camera target transform is not set. Cannot start camera move.", this);
            return;
        }

        // Calculate the final destination with the percentage-based Z offset
        Vector3 startPosition = cam.transform.position;
        Vector3 targetPosition = cameraTargetTransform.position;
        float finalZ = startPosition.z + (targetPosition.z - startPosition.z) * (stopDistancePercent / 100f);
        Vector3 finalTargetPosition = new Vector3(targetPosition.x, targetPosition.y, finalZ);

        // If a move is already in progress, stop it first.
        if (_cameraMoveCoroutine != null)
        {
            StopCoroutine(_cameraMoveCoroutine);
        }
        _cameraMoveCoroutine = StartCoroutine(MoveCameraCoroutine(finalTargetPosition, cameraMoveDuration));
    }

    /// <summary>
    /// 觸發被打到時的停止效果
    /// </summary>
    public void TriggerHitStop()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Hit Stop can only be triggered in Play Mode.");
            return;
        }

        // 如果正在暈眩中，忽略新的 Hit Stop
        if (isDizzy)
            return;

        Debug.Log("[ParallaxManager] 觸發 Hit Stop - 開始暈眩效果");

        // 設置暈眩狀態（不停止相機移動協程，讓它自己處理）
        isDizzy = true;

        // 開始暈眩效果
        StartCoroutine(DizzyEffect());
    }

    /// <summary>
    /// 暈眩效果協程 - 讓鏡頭畫圈模擬暈眩感
    /// </summary>
    private IEnumerator DizzyEffect()
    {
        // 1. 紀錄原始旋轉角度
        Quaternion originalRotation = cam.transform.rotation;
        Vector3 originalEuler = originalRotation.eulerAngles;

        float startTime = Time.time;
        
        Debug.Log($"[ParallaxManager] 開始暈眩 (速度:{dizzySpeed}, X強度:{dizzyStrengthX}, Y強度:{dizzyStrengthY})");

        // ================= 階段一：暈眩晃動 =================
        while (Time.time - startTime < dizzyDuration)
        {
            float elapsed = Time.time - startTime;
            
            // 計算淡入強度 (0 ~ 1)，讓暈眩有個開始的過程
            float masterIntensity = Mathf.Clamp01(elapsed / fadeInDuration);

            // 更新後處理權重
            if (dizzyVolume != null) dizzyVolume.weight = masterIntensity;

            // 使用穩定的圓周旋轉 (sin/cos) 映射到角度 (pitch/yaw)，並只加入小型的
            // additive jitter 而不會改變主要振幅，確保整體幅度穩定一致。
            float baseAngleSpeed = dizzySpeed; // 角速度基礎（rad/s）
            // 累積角度驅動圓周運動
            float baseAngle = Time.time * baseAngleSpeed;

            // 穩定振幅 (不受噪聲大幅影響)
            float ampX = dizzyStrengthX * masterIntensity;
            float ampY = dizzyStrengthY * masterIntensity;

            // 圓周運動映射到角度
            float rotX = Mathf.Sin(baseAngle) * ampX;
            float rotY = Mathf.Cos(baseAngle) * ampY;

            // 小型 additive jitter (不要放大振幅，只作微調)
            float jitterX = (Mathf.PerlinNoise(Time.time * 1.1f, 0f) - 0.5f) * 2f * amplitudeJitter;
            float jitterY = (Mathf.PerlinNoise(0f, Time.time * 1.3f) - 0.5f) * 2f * amplitudeJitter;

            // 應用到 camera（保留 Z 不變）
            cam.transform.rotation = Quaternion.Euler(
                originalEuler.x + rotX + jitterX,
                originalEuler.y + rotY + jitterY,
                originalEuler.z
            );

            yield return null;
        }

        // ================= 階段二：平滑恢復 =================
        Debug.Log("[ParallaxManager] 暈眩結束，開始回正...");
        
        float recoveryStart = Time.time;
        Quaternion endDizzyRot = cam.transform.rotation; // 記住暈眩最後一刻的角度
        float startVolumeWeight = (dizzyVolume != null) ? dizzyVolume.weight : 0f;

        while (Time.time - recoveryStart < recoveryDuration)
        {
            float t = (Time.time - recoveryStart) / recoveryDuration;
            
            // 使用 SmoothStep (S型曲線) 讓回正過程頭尾慢、中間快，比較自然
            t = Mathf.SmoothStep(0f, 1f, t); 

            // 使用 Slerp 平滑轉回原始角度
            cam.transform.rotation = Quaternion.Slerp(endDizzyRot, originalRotation, t);

            // 淡出 Volume
            if (dizzyVolume != null)
                dizzyVolume.weight = Mathf.Lerp(startVolumeWeight, 0f, t);

            yield return null;
        }

        // ================= 階段三：確保歸位 =================
        if (dizzyVolume != null) dizzyVolume.weight = 0.0f;
        cam.transform.rotation = originalRotation;
        isDizzy = false;
        
        Debug.Log("[ParallaxManager] 視線完全恢復");
    }

    private IEnumerator MoveCameraCoroutine(Vector3 targetPosition, float duration)
    {
        if (cam == null) yield break;

        float elapsedTime = 0f;
        float movementProgressTime = 0f; // 追蹤實際移動的時間進度
        Vector3 startingPosition = cam.transform.position;
        
        // Reset bob timer
        bobTimer = 0f;
        stepPlayed = false;
        currentHitStopTimer = 0f;

        while (elapsedTime < duration)
        {
            // 總時間總是流逝 (維持總時長不變)
            elapsedTime += Time.deltaTime;

            if (isDizzy)
            {
                // 暈眩中：不增加移動進度，不更新位置，但總時間繼續計算
                // 這樣最終會因為 movementProgressTime < duration 而到不了終點
            }
            else
            {
                // 只有沒暈眩時才增加移動進度
                movementProgressTime += Time.deltaTime;

                // 使用 movementProgressTime 來計算位置
                // 這樣如果中間有停頓，最終位置就會不到達終點 (movementProgressTime < duration)
                Vector3 currentPos = Vector3.Lerp(startingPosition, targetPosition, movementProgressTime / duration);
                
                // Apply Head Bob
                if (enableHeadBob)
                {
                    bobTimer += Time.deltaTime * bobFrequency;
                    float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude;
                    currentPos.y += bobOffset;
                    
                    // Handle Footsteps
                    if (enableFootsteps)
                    {
                        HandleFootsteps();
                    }
                }
                
                cam.transform.position = currentPos;
            }
            
            // Wait for the next frame
            yield return null;
        }

        // 時間到，結束移動。
        // 注意：不強制設定為 targetPosition，因為如果中間有暈眩，玩家應該到不了終點。
        _cameraMoveCoroutine = null;
    }
    
    private void HandleFootsteps()
    {
        if (footstepSounds == null || footstepSounds.Count == 0) return;

        // 當 Sin 波接近谷底 (-1) 時播放腳步聲
        float cyclePos = Mathf.Sin(bobTimer);
        
        // 閾值設為 -0.9，表示接近最低點（腳步落地）
        if (cyclePos < -0.9f && !stepPlayed)
        {
            PlayFootstep();
            stepPlayed = true;
        }
        // 當波形回升超過 -0.5 時重置標記，準備下一次腳步
        else if (cyclePos > -0.5f)
        {
            stepPlayed = false;
        }
    }

    private void PlayFootstep()
    {
        if (footstepSounds.Count == 0) return;
        
        // 隨機選擇一個腳步聲
        int index = Random.Range(0, footstepSounds.Count);
        AudioClip clip = footstepSounds[index];
        
        if (clip != null && footstepAudioSource != null)
        {
            // 稍微隨機化音高，增加自然感
            footstepAudioSource.pitch = Random.Range(0.9f, 1.1f);
            footstepAudioSource.PlayOneShot(clip, footstepVolume);
        }
    }

    void OnValidate()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
        // Allows for live preview in the editor when values are changed.
        if (Application.isEditor && !Application.isPlaying)
        {
            InitializeParallax();
        }
    }

    void InitializeParallax()
    {
        if (cam == null || layers == null || layers.Length == 0)
        {
            return;
        }

        _initialScales = new Vector3[layers.Length];
        _initialDistances = new float[layers.Length];

        // The first layer is the reference layer with scale (1, 1, 1)
        Transform referenceLayer = layers[0];
        float referenceDistance = Mathf.Abs(referenceLayer.position.z - cam.transform.position.z);
        
        if (referenceDistance <= 0)
        {
            Debug.LogWarning("Reference layer's distance to camera is zero or negative. Cannot calculate parallax.", referenceLayer);
            return;
        }

        // Set the first layer to scale (1, 1, 1)
        _initialScales[0] = new Vector3(1f, 1f, referenceLayer.localScale.z);
        _initialDistances[0] = referenceDistance;
        referenceLayer.localScale = _initialScales[0];

        // Calculate the height of the frustum at the reference distance
        float referenceFrustumHeight = 2.0f * referenceDistance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        
        // The base height is the frustum height at reference distance (for scale 1)
        float baseHeight = referenceFrustumHeight;

        // Calculate and store the initial scale for all other layers
        for (int i = 1; i < layers.Length; i++)
        {
            Transform layer = layers[i];
            if (layer == null) continue;

            float distance = Mathf.Abs(layer.position.z - cam.transform.position.z);
            _initialDistances[i] = distance;
            
            if (distance <= 0)
            {
                Debug.LogWarning("Layer is at the same position as the camera. Skipping scale calculation.", layer);
                continue;
            }

            // Calculate the frustum height at this layer's distance
            float frustumHeightAtDistance = 2.0f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            
            // Scale needed to fill the screen: frustumHeight at this distance / frustumHeight at reference distance
            float requiredScale = frustumHeightAtDistance / baseHeight;

            _initialScales[i] = new Vector3(requiredScale, requiredScale, layer.localScale.z);
            layer.localScale = _initialScales[i];
        }

        _isInitialized = true;
    }

    void UpdateDynamicScaling()
    {
        if (!_isInitialized || cam == null || layers == null || layers.Length == 0)
        {
            return;
        }

        for (int i = 0; i < layers.Length; i++)
        {
            Transform layer = layers[i];
            if (layer == null || _initialDistances[i] <= 0) continue;

            float currentDistance = Mathf.Abs(layer.position.z - cam.transform.position.z);
            
            if (currentDistance <= 0) continue;

            // Calculate the ratio of current distance to initial distance
            float distanceRatio = currentDistance / _initialDistances[i];
            
            // Apply dynamic scaling based on the factor
            // If dynamicScaleFactor = 0, scale stays at initial
            // If dynamicScaleFactor = 1, scale changes fully with distance
            // If dynamicScaleFactor = 0.1, scale changes by 10% of the distance change
            float scaleMultiplier = 1.0f + (distanceRatio - 1.0f) * dynamicScaleFactor;
            
            Vector3 newScale = _initialScales[i] * scaleMultiplier;
            layer.localScale = new Vector3(newScale.x, newScale.y, layer.localScale.z);
        }
    }

    void CalculateParallax()
    {
        // This method is kept for backward compatibility and editor updates
        InitializeParallax();
    }
}
