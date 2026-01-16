using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridManager))]
public class GridManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GridManager script = (GridManager)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Grid", GUILayout.Height(30)))
        {
            script.GenerateGridForEditor();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Clear Grid", GUILayout.Height(20)))
        {
            script.ClearGrid();
        }
    }
}