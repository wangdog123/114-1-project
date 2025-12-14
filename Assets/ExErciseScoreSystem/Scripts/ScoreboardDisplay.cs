using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
public enum SortOrder
{
    Ascending,
    Descending
}
public class ScoreboardDisplay : MonoBehaviour
{
    static public ScoreboardDisplay Instance;
    public SortOrder sortOrder = SortOrder.Descending;
    public int rankAmount = 5;
    [SerializeField] private TextMeshProUGUI textMesh;
    // Start is called before the first frame update

    void Start()
    {
        UpdateScoreBoard();
    }

    // Update is called once per frame

    public void UpdateScoreBoard() //use this <-
    {
        // 檢查必要的組件是否已初始化
        if (textMesh == null)
        {
            Debug.LogError("[ScoreboardDisplay] textMesh 為 null，請在 Inspector 中指派 TextMeshProUGUI 組件");
            return;
        }

        if (GoogleSheetDataHandler.Instance == null)
        {
            Debug.LogError("[ScoreboardDisplay] GoogleSheetDataHandler.Instance 為 null，無法更新記分板");
            return;
        }

        StartCoroutine(IUpdateScoreBoard());
    }

    private IEnumerator IUpdateScoreBoard() //Get data, sort data and display on textmesh in coroutine
    {
        List<IList<object>> scoreList = new List<IList<object>>();
        Thread t = new Thread(() =>
        {
            try
            {
                scoreList = GoogleSheetDataHandler.Instance.GetScoreData();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ScoreboardDisplay] 獲取分數數據失敗: {e.Message}");
            }
        });
        t.Start();
        yield return new WaitUntil(() => t.IsAlive == false);

        if (scoreList == null || scoreList.Count == 0)
        {
            Debug.LogWarning("[ScoreboardDisplay] 沒有取得任何分數數據");
            textMesh.text = "暫無記錄";
            yield break;
        }

        if(sortOrder == SortOrder.Descending)
        {
            scoreList = scoreList.OrderByDescending(r=>r[2]).ToList();
        }
        else if(sortOrder == SortOrder.Ascending)
        {
            scoreList = scoreList.OrderBy(r=>r[2]).ToList();
        }
        string str = "";
        int amount = (scoreList.Count() > rankAmount) ? rankAmount : scoreList.Count();
        for(int i = 0; i < amount; i++)
        {
            str += $"{i+1}. {scoreList[i][1]} | {scoreList[i][2]}\n"; //String format. If you want to change the look, edit this line
        }
        textMesh.text = str;
        yield break;
    }
}
