using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VibrationManager))]
public class VibrationManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        VibrationManager vibrationManager = (VibrationManager)target;
        
        // 繪製默認的 Inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        
        // Perfect Hit Profile 測試按鈕
        EditorGUILayout.LabelField("震動測試", EditorStyles.boldLabel);
        
        if (GUILayout.Button("▶ 播放 Perfect Hit 震動", GUILayout.Height(30)))
        {
            vibrationManager.VibrateOnPerfectHit();
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("▶ 播放 Great Hit 震動", GUILayout.Height(30)))
        {
            vibrationManager.VibrateOnGreatHit();
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("▶ 播放 Good Hit 震動", GUILayout.Height(30)))
        {
            vibrationManager.VibrateOnGoodHit();
        }
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("▶ 播放 Miss 震動", GUILayout.Height(30)))
        {
            vibrationManager.VibrateOnMiss();
        }
        
        EditorGUILayout.Space(10);
        
        // 停止按鈕
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("⏹ 停止所有震動", GUILayout.Height(25)))
        {
            vibrationManager.StopAllVibrations();
        }
        GUI.backgroundColor = Color.white;
    }
}
