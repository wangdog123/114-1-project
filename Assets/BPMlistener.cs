using UnityEngine;
using System.Net;
using System.IO;
using System.Threading;
using System;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BPMLISTENER : MonoBehaviour
{
    public string url = "http://localhost:8080/stream";
    public TextMeshProUGUI bpmText;
    
    // 用于存储待更新的BPM值的队列
    private Queue<string> bpmQueue = new Queue<string>();
    private readonly object queueLock = new object();
    
    // 添加線程控制變量
    private Thread sseThread;
    private volatile bool isRunning = true;
    private volatile bool shouldReconnect = false;

    void Start()
    {
        bpmText.text = "等待連接...";
        StartSSEConnection();
    }

    void Update()
    {
        // 在主线程中处理队列中的BPM更新
        lock (queueLock)
        {
            while (bpmQueue.Count > 0)
            {
                string bpm = bpmQueue.Dequeue();
                bpmText.text = bpm;
            }
        }
            if (Input.GetKeyDown(KeyCode.R))
        {
            RetryConnection();
        }
    }

    void ListenSSE()
    {
        try
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = 5000; // 5秒超時

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                // 連接成功
                lock (queueLock)
                {
                    bpmQueue.Enqueue("連接成功");
                }
                Debug.Log("心率監測器連接成功！");
                
                string line;
                while (isRunning && (line = reader.ReadLine()) != null)
                {
                    if (!isRunning) break; // 檢查是否應該停止
                    
                    if (line.StartsWith("data:"))
                    {
                        string json = line.Substring(5).Trim();
                        HeartRateData data = JsonUtility.FromJson<HeartRateData>(json);
                        Debug.Log("心率: " + data.value);
                        
                        // 將BPM值添加到队列中，等待主线程处理
                        lock (queueLock)
                        {
                            bpmQueue.Enqueue(data.value.ToString());
                        }
                    }
                }
            }
        }
        catch (WebException e)
        {
            if (isRunning) // 只有在正常運行時才記錄錯誤
            {
                Debug.LogError("心率監測器連接失敗: " + e.Message);
                lock (queueLock)
                {
                    bpmQueue.Enqueue("連接失敗");
                }
            }
        }
        catch (Exception e)
        {
            if (isRunning) // 只有在正常運行時才記錄錯誤
            {
                Debug.LogError("SSE連接錯誤: " + e.Message);
                lock (queueLock)
                {
                    bpmQueue.Enqueue("錯誤");
                }
            }
        }
    }

    // 當物件被銷毀時停止線程
    void OnDestroy()
    {
        StopSSE();
    }

    // 當應用程式關閉時停止線程
    void OnApplicationQuit()
    {
        StopSSE();
    }

    // 停止SSE連接的方法
    void StopSSE()
    {
        isRunning = false;
        
        if (sseThread != null && sseThread.IsAlive)
        {
            // 等待線程結束，最多等待2秒
            if (!sseThread.Join(2000))
            {
                Debug.LogWarning("SSE線程未能正常結束，強制終止");
                sseThread.Abort(); // 強制終止線程（不推薦，但作為最後手段）
            }
        }
    }

    // 啟動SSE連接的方法
    public void StartSSEConnection()
    {
        // 如果已經在運行，先停止
        if (sseThread != null && sseThread.IsAlive)
        {
            StopSSE();
        }
        
        // 重置狀態
        isRunning = true;
        lock (queueLock)
        {
            bpmQueue.Clear();
        }
        
        // 啟動新的SSE線程
        sseThread = new Thread(ListenSSE);
        sseThread.IsBackground = true;
        sseThread.Start();
        
        bpmText.text = "連接中...";
        Debug.Log("開始連接心率監測器...");
    }

    // 重新連接SSE（公開方法，可以從Unity Button調用）
    public void RetryConnection()
    {
        Debug.Log("重新連接心率監測器...");
        StartSSEConnection();
    }

    // 手動停止SSE的方法（如果需要）
    public void StopSSEConnection()
    {
        StopSSE();
        bpmText.text = "已停止";
        Debug.Log("已停止心率監測");
    }
}

[Serializable]
public class HeartRateData
{
    public int value;
    public bool contact;
    public long timestamp;
}