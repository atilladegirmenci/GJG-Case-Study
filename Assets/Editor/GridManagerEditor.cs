using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GridManager script = (GridManager)target;


        GUILayout.Space(20);

        if (GUILayout.Button("Force Deadlock", GUILayout.Height(50)))
        {
            script.ForceDeadlockPattern();
        }
    }
}