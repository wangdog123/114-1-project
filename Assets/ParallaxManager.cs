using System.Collections;
using UnityEngine;
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

    // Store initial scales and distances for each layer
    private Vector3[] _initialScales;
    private float[] _initialDistances;
    private bool _isInitialized = false;

    private Coroutine _cameraMoveCoroutine;

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

    private IEnumerator MoveCameraCoroutine(Vector3 targetPosition, float duration)
    {
        if (cam == null) yield break;

        float elapsedTime = 0f;
        Vector3 startingPosition = cam.transform.position;

        while (elapsedTime < duration)
        {
            // Use Lerp to interpolate the position
            cam.transform.position = Vector3.Lerp(startingPosition, targetPosition, elapsedTime / duration);
            
            // Increment the elapsed time
            elapsedTime += Time.deltaTime;
            
            // Wait for the next frame
            yield return null;
        }

        // Ensure the camera reaches the exact target position at the end
        cam.transform.position = targetPosition;
        _cameraMoveCoroutine = null;
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
