using UnityEngine;
using UnityEngine.InputSystem.Controls;

public class BulletController : MonoBehaviour
{
    [Header("子彈設置")]
    public float damage = 1f; // 傷害值
    public string targetTag = "Target"; // 目標標籤
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > 1.5f)
        {
            Destroy(gameObject);
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        // 檢查是否擊中目標
        if (other.CompareTag(targetTag))
        {
            Debug.Log($"子彈擊中目標: {other.name}");
            
            // 可以在這裡添加擊中效果
            // 例如：播放音效、粒子效果等
            
            // 銷毀子彈
            Destroy(other.gameObject);
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        // 子彈離開邊界時銷毀
        if (other.CompareTag("Boundary"))
        {
            Destroy(gameObject);
        }
    }
}
