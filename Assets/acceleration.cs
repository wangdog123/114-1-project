using System.Collections;
using System.Collections.Generic;
// using UnityEditor; // <-- 移除這一行！它會導致 Build 失敗
using UnityEngine;
using UnityEngine.InputSystem.Switch;
using UnityEngine.UIElements.Experimental; // (這行如果沒用到也可以移除)

public class Acceleration : MonoBehaviour
{
    [Header("靈敏度")]
    public float sensitivity = 5.0f;

    [Header("平滑化 (速度)")]
    [Tooltip("數值越小，移動越平滑 (但反應越慢)")]
    [Range(0.01f, 1.0f)]
    public float smoothFactor = 0.1f;

    [Header("死區")]
    [Tooltip("傾斜幅度小於此值時，視為 0")]
    public float deadZone = 0.05f;

    [Header("剎車偵測")]
    [Tooltip("偵測到反向「煞車」的靈敏度。越接近0越敏感，-0.9代表幾乎完全反向")]
    [Range(-0.9f, 0f)]
    public float brakeDetectionThreshold = -0.5f;
    
    [Header("校準 (Calibration)")]
    [Tooltip("用於校準的按鈕 (例如Joy-Con的A鍵)")]
    public KeyCode calibrationKey = KeyCode.Space;

    // 用來儲存「靜止時」的重力向量
    private Vector3 accelerationOffset = Vector3.zero;

    // 【關鍵改變】我們現在儲存並平滑「速度」，而不是「加速度」
    private Vector3 smoothedVelocity = Vector3.zero;

    void Start()
    {
        StartCoroutine(Calibrate());
        if (SwitchControllerHID.current != null)
        {
            SwitchControllerHID.current.ReadUserIMUCalibrationData();
        }
    }

    void Update()
    {
        // --- 1. 檢查校準與暫停 ---
        if ((SwitchControllerHID.current != null && SwitchControllerHID.current.buttonNorth.wasPressedThisFrame)|| SwitchControllerHID.current != null && SwitchControllerHID.current.dpad.up.wasPressedThisFrame)
        {
            StartCoroutine(Calibrate());
        }
        
        // (你的暫停邏輯... 保持不變)
        if ((SwitchControllerHID.current != null && SwitchControllerHID.current.buttonWest.wasPressedThisFrame) || (SwitchControllerHID.current != null && SwitchControllerHID.current.dpad.right.wasPressedThisFrame))
        {
            if(Time.timeScale > 0f) { Time.timeScale = 0f; }
            else { Time.timeScale = 1f; }
        }

        if (SwitchControllerHID.current == null) return;

        // --- 2. 取得「原始」加速度差值 ---
        Vector3 rawAccel = SwitchControllerHID.current.acceleration.ReadValue();
        Vector3 currentDelta = rawAccel - accelerationOffset;

        // --- 3. 映射到「玩家原始意圖」 ---
        // (使用你測試好的 y, x 軸向)
        float inputX = currentDelta.y; 
        float inputY = currentDelta.x;

        // --- 4. 應用死區 ---
        if (Mathf.Abs(inputX) < deadZone) inputX = 0;
        if (Mathf.Abs(inputY) < deadZone) inputY = 0;

        // 這是玩家「這一幀」的「原始意圖速度」
        Vector3 rawTargetVelocity = new Vector3(inputX, inputY, 0) * sensitivity;

        // --- 5. 【全新邏輯】剎車偵測 ---
        Vector3 finalTargetVelocity; // 這是我們「真正」的目標速度

        // 取得上一幀的「平滑速度」方向
        Vector3 currentVelocityDir = smoothedVelocity.normalized;
        // 取得這一幀「原始意圖」的方向
        Vector3 intentionDir = rawTargetVelocity.normalized;
        
        // 計算點積 (dot product)
        float dot = Vector3.Dot(currentVelocityDir, intentionDir);

        // 判斷：
        // 1. 我們目前有速度 (sqrMagnitude > 0.01f 避免靜止時誤判)
        // 2. 且 玩家的意圖與當前速度「相反」(dot < 臨界值)
        if (smoothedVelocity.sqrMagnitude > 0.01f && dot < brakeDetectionThreshold)
        {
            // 這是一個「剎車」動作！
            // 我們的目標不是「反向移動」，而是「停下來」
            finalTargetVelocity = Vector3.zero;
        }
        else
        {
            // 這是一個正常的移動或加速
            // 我們的目標就是玩家的原始意圖
            finalTargetVelocity = rawTargetVelocity;
        }

        // --- 6. 平滑化「速度」 ---
        // 我們永遠將「當前速度」平滑地趨向「最終目標速度」
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, finalTargetVelocity, smoothFactor);

        // --- 7. 更新位置 ---
        // 我們使用平滑後的速度來移動
        transform.localPosition += smoothedVelocity * Time.deltaTime;
        if (Time.timeScale > 0f)
        {
            Debug.Log("原始加速度: " + rawAccel + "，校準偏移: " + accelerationOffset + "，加速度差值: " + currentDelta);
            Debug.Log("當前速度: " + smoothedVelocity);
        }
        
    }

    IEnumerator Calibrate()
    {
        yield return new WaitForSeconds(0.1f); // 等待一幀，確保讀取到最新數據
        if (SwitchControllerHID.current == null) 
        {
            accelerationOffset = new Vector3(0.9f, 0, 0); // 備案
        }
        else
        {
            accelerationOffset = SwitchControllerHID.current.acceleration.ReadValue();
        }
        
        Debug.Log("校準完畢！新的中心點: " + accelerationOffset);

        // 校準時，重置「平滑速度」
        smoothedVelocity = Vector3.zero;
    }
}