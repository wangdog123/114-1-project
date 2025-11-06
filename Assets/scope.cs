using UnityEngine;

public class Scope : MonoBehaviour
{
    [Header("滑鼠控制設置")]
    public float sensitivity = 2f;           // 滑鼠靈敏度
    public float moveRange = 5f;             // 移動範圍限制
    public Transform target;                 // 追蹤目標（玩家）

    void Update()
    {
        // 獲取滑鼠輸入
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 計算移動向量
        Vector3 moveDirection = new Vector3(mouseX, mouseY, 0);
        
        // 限制移動範圍
        moveDirection = Vector3.ClampMagnitude(moveDirection, moveRange);

        // 更新位置
        if (target != null)
        {
            transform.position = target.position + moveDirection * sensitivity;
        }
        else
        {
            transform.position += moveDirection * sensitivity * Time.deltaTime;
        }
    }
}