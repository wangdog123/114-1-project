using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 劃痕方向圖示
/// 顯示在 UI 上，表示玩家需要揮動的方向
/// </summary>
[RequireComponent(typeof(Image))]
public class SlashIcon : MonoBehaviour
{
    [Header("=== 方向圖示 ===")]
    [Tooltip("向上的劃痕圖示")]
    public Sprite upSprite;
    
    [Tooltip("向下的劃痕圖示")]
    public Sprite downSprite;
    
    [Tooltip("向左的劃痕圖示")]
    public Sprite leftSprite;
    
    [Tooltip("向右的劃痕圖示")]
    public Sprite rightSprite;
    
    [Tooltip("斜向的劃痕圖示（可選，會自動旋轉）")]
    public Sprite diagonalSprite;
    
    [Header("=== 顏色設定 ===")]
    public Color normalColor = Color.white;
    public Color hiddenColor = new Color(1, 1, 1, 0.3f);
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    
    private Image image;
    private SwingDetector.SwingDirection direction;
    
    void Awake()
    {
        image = GetComponent<Image>();
    }
    
    /// <summary>
    /// 設定方向並顯示對應的圖示
    /// </summary>
    public void SetDirection(SwingDetector.SwingDirection dir)
    {
        direction = dir;
        
        // 根據方向設定圖示和旋轉
        switch (direction)
        {
            case SwingDetector.SwingDirection.Up:
                if (upSprite != null) image.sprite = upSprite;
                image.transform.rotation = Quaternion.Euler(0, 0, 0);
                break;
                
            case SwingDetector.SwingDirection.Down:
                if (downSprite != null) image.sprite = downSprite;
                else if (upSprite != null) image.sprite = upSprite;
                image.transform.rotation = Quaternion.Euler(0, 0, 180);
                break;
                
            case SwingDetector.SwingDirection.Left:
                if (leftSprite != null) image.sprite = leftSprite;
                else if (rightSprite != null) image.sprite = rightSprite;
                image.transform.rotation = Quaternion.Euler(0, 0, 90);
                break;
                
            case SwingDetector.SwingDirection.Right:
                if (rightSprite != null) image.sprite = rightSprite;
                image.transform.rotation = Quaternion.Euler(0, 0, -90);
                break;
                
            // 斜向
            case SwingDetector.SwingDirection.UpRight:
                if (diagonalSprite != null) image.sprite = diagonalSprite;
                else if (upSprite != null) image.sprite = upSprite;
                image.transform.rotation = Quaternion.Euler(0, 0, -45);
                break;
                
            case SwingDetector.SwingDirection.UpLeft:
                if (diagonalSprite != null) image.sprite = diagonalSprite;
                else if (upSprite != null) image.sprite = upSprite;
                image.transform.rotation = Quaternion.Euler(0, 0, 45);
                break;
                
            case SwingDetector.SwingDirection.DownLeft:
                if (diagonalSprite != null) image.sprite = diagonalSprite;
                else if (downSprite != null) image.sprite = downSprite;
                image.transform.rotation = Quaternion.Euler(0, 0, 135);
                break;
                
            case SwingDetector.SwingDirection.DownRight:
                if (diagonalSprite != null) image.sprite = diagonalSprite;
                else if (downSprite != null) image.sprite = downSprite;
                image.transform.rotation = Quaternion.Euler(0, 0, -135);
                break;
        }
        
        // 如果沒有設定 Sprite，就用箭頭文字代替
        if (image.sprite == null)
        {
            // 可以在這裡加入 Text 組件來顯示文字
            Text text = GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = GetDirectionArrow(direction);
            }
        }
        
        image.color = normalColor;
    }
    
    /// <summary>
    /// 取得方向的箭頭符號
    /// </summary>
    string GetDirectionArrow(SwingDetector.SwingDirection dir)
    {
        switch (dir)
        {
            case SwingDetector.SwingDirection.Up: return "↑";
            case SwingDetector.SwingDirection.Down: return "↓";
            case SwingDetector.SwingDirection.Left: return "←";
            case SwingDetector.SwingDirection.Right: return "→";
            case SwingDetector.SwingDirection.UpRight: return "↗";
            case SwingDetector.SwingDirection.UpLeft: return "↖";
            case SwingDetector.SwingDirection.DownRight: return "↘";
            case SwingDetector.SwingDirection.DownLeft: return "↙";
            default: return "?";
        }
    }
    
    /// <summary>
    /// 隱藏圖示（變半透明）
    /// </summary>
    public void Hide()
    {
        image.color = hiddenColor;
    }
    
    /// <summary>
    /// 顯示圖示
    /// </summary>
    public void Show()
    {
        image.color = normalColor;
    }
    
    /// <summary>
    /// 標記為正確
    /// </summary>
    public void MarkAsCorrect()
    {
        image.color = correctColor;
    }
    
    /// <summary>
    /// 標記為錯誤
    /// </summary>
    public void MarkAsWrong()
    {
        image.color = wrongColor;
    }
}
