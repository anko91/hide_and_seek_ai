using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NavigationArea))]
public class NavigationAreaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Rebuild Area"))
        {
            var area = target as NavigationArea;
            area.RebuildArea();
            Debug.Log("Area rebuilded");
        }
    }
}
