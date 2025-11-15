using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ParallaxManager))]
public class ParallaxManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields
        DrawDefaultInspector();

        // Get a reference to the ParallaxManager script
        ParallaxManager manager = (ParallaxManager)target;

        // Add a button to the inspector
        if (!Application.isPlaying)
        {
            if (GUILayout.Button("Preview Camera Movement"))
            {
                // Call the public method on the manager script
                manager.StartEditorCameraMove();
            }
            if (GUILayout.Button("Reset Camera"))
            {
                // Call the public method on the manager script
                manager.ResetEditorCameraPosition();
            }
        }
    }
}
