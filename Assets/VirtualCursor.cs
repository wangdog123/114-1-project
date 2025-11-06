using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualCursor : MonoBehaviour
{
    public RectTransform cursorUI; // UI光标对象
    public float moveSpeed = 500f; // 移动速度
    public Canvas canvas; // Canvas 引用（用於座標轉換）
    public test joyConController; // Joy-Con 控制器引用（用於重置陀螺儀追蹤）
    
    private Vector2 cursorPosition; // 光標位置（限制在螢幕內）
    private RectTransform canvasRectTransform;

    void Start()
    {
        if (cursorUI == null)
        {
            Debug.LogError("请在 Inspector 中设置 Cursor UI!");
            return;
        }
        
        // 自動獲取 Canvas
        if (canvas == null)
        {
            canvas = cursorUI.GetComponentInParent<Canvas>();
        }
        
        if (canvas != null)
        {
            canvasRectTransform = canvas.GetComponent<RectTransform>();
        }

        // 初始化光标位置到屏幕中心
        cursorPosition = new Vector2(Screen.width / 2, Screen.height / 2);
        UpdateCursorPosition();
        
        // 隐藏系统鼠标
        Cursor.visible = false;
        
        Debug.Log($"[VirtualCursor] Canvas: {canvas?.name}, Canvas Size: {canvasRectTransform?.sizeDelta}");
    }

    void OnDestroy()
    {
        // 恢复系统鼠标
        Cursor.visible = true;
    }

    public void MoveCursor(float deltaX, float deltaY)
    {
        // ★ 更新光標位置（限制在螢幕內）
        cursorPosition.x += deltaX;
        cursorPosition.y += deltaY;
        cursorPosition.x = Mathf.Clamp(cursorPosition.x, 0, Screen.width);
        cursorPosition.y = Mathf.Clamp(cursorPosition.y, 0, Screen.height);

        UpdateCursorPosition();
    }

    void UpdateCursorPosition()
    {
        if (cursorUI != null)
        {
            cursorUI.position = cursorPosition;
        }
    }

    public Vector2 GetCursorPosition()
    {
        return cursorPosition;
    }
    
    // 重置位置（當揮動結束時呼叫）
    public void ResetActualPosition()
    {
        Debug.Log($"[VirtualCursor] 重置位置: {cursorPosition}");
    }
    
    // 重置光標到螢幕中心 (960, 540)
    public void ResetToOrigin()
    {
        cursorPosition = new Vector2(960, 540);
        UpdateCursorPosition();
        
        // 重置 Joy-Con 陀螺儀追蹤狀態
        if (joyConController != null)
        {
            joyConController.ResetGyroTracking();
        }
        
        Debug.Log($"[VirtualCursor] 光標重置到螢幕中心 (960, 540)");
    }
    
    // 獲取 Canvas 空間的光標位置（用於與目標比較距離）
    public Vector2 GetCursorPositionInCanvas()
    {
        if (cursorUI != null && canvasRectTransform != null)
        {
            // ★ 使用螢幕內的光標位置轉換為 Canvas 座標
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                cursorPosition,
                canvas.worldCamera,
                out localPoint
            );
            return localPoint;
        }
        
        Debug.LogWarning("[VirtualCursor] cursorUI or canvas is null!");
        return Vector2.zero;
    }
}
