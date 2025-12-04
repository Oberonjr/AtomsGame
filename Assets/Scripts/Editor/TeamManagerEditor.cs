using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(TeamManager))]
public class TeamManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();

        // Draw default inspector
        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();

            // Sync colors after any change
            TeamManager manager = (TeamManager)target;
            SyncTeamColors(manager);
        }

        // Add a manual sync button
        EditorGUILayout.Space();
        if (GUILayout.Button("Sync Team Colors"))
        {
            TeamManager manager = (TeamManager)target;
            SyncTeamColors(manager);
        }
    }

    private void SyncTeamColors(TeamManager manager)
    {
        if (manager == null || manager.Teams == null) return;

        foreach (var team in manager.Teams)
        {
            if (team != null)
            {
                // Use reflection to get private fields
                var areaField = typeof(Team).GetField("_area", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var colorField = typeof(Team).GetField("_teamColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (areaField != null && colorField != null)
                {
                    TeamArea area = areaField.GetValue(team) as TeamArea;
                    Color teamColor = (Color)colorField.GetValue(team);

                    if (area != null)
                    {
                        area.SetTeamColor(teamColor);
                        EditorUtility.SetDirty(area);
                    }
                }
            }
        }
    }
}