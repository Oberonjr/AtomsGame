using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TeamArea))]
public class TeamAreaEditor : Editor
{
    private TeamArea teamArea;
    private SerializedProperty radiusProperty;
    private SerializedProperty teamColorProperty;
    private SerializedProperty showGizmoProperty;
    private SerializedProperty showInnerAreaProperty;
    private SerializedProperty showBorderProperty;
    private SerializedProperty borderThicknessProperty;
    
    // Runtime visualization properties
    private SerializedProperty circleSpritePrefabProperty;
    private SerializedProperty showRuntimeCircleProperty;

    private void OnEnable()
    {
        teamArea = target as TeamArea;

        // Cache serialized properties
        radiusProperty = serializedObject.FindProperty("Radius");
        teamColorProperty = serializedObject.FindProperty("_teamColor");
        showGizmoProperty = serializedObject.FindProperty("ShowGizmo");
        showInnerAreaProperty = serializedObject.FindProperty("ShowInnerArea");
        showBorderProperty = serializedObject.FindProperty("ShowBorder");
        borderThicknessProperty = serializedObject.FindProperty("BorderThickness");
        
        // Runtime visualization properties
        circleSpritePrefabProperty = serializedObject.FindProperty("CircleSpritePrefab");
        showRuntimeCircleProperty = serializedObject.FindProperty("ShowRuntimeCircle");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(5);

        // Visual Settings (Editor Gizmos)
        EditorGUILayout.LabelField("Editor Gizmo Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(showGizmoProperty);

        if (showGizmoProperty.boolValue)
        {
            EditorGUILayout.PropertyField(showInnerAreaProperty);
            EditorGUILayout.PropertyField(showBorderProperty);

            if (showBorderProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(borderThicknessProperty);
                EditorGUI.indentLevel--;
            }
        }

        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);

        // Runtime Visualization Settings
        EditorGUILayout.LabelField("Runtime Visualization", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(showRuntimeCircleProperty, new GUIContent("Show in Play Mode"));
        
        if (showRuntimeCircleProperty.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(circleSpritePrefabProperty, new GUIContent("Circle Sprite Prefab"));
            
            if (circleSpritePrefabProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Leave empty to auto-generate a circle sprite at runtime.", MessageType.Info);
            }
            
            EditorGUI.indentLevel--;
        }

        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);

        // Area Settings
        EditorGUILayout.LabelField("Area Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(radiusProperty);

        // Display team color as read-only if subscribed to a team
        bool hasSubscribedTeam = teamArea != null && 
            teamArea.GetType().GetField("_subscribedTeam", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(teamArea) != null;
        
        GUI.enabled = !hasSubscribedTeam;
        EditorGUILayout.PropertyField(teamColorProperty, new GUIContent("Team Color"));
        GUI.enabled = true;

        if (hasSubscribedTeam)
        {
            EditorGUILayout.HelpBox("Team color is controlled by the assigned Team in TeamManager.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("This color will be used for both editor gizmos and runtime visualization circles during Prep phase.", MessageType.Info);
        }

        EditorGUI.indentLevel--;

        EditorGUILayout.Space(5);

        // Apply changes
        serializedObject.ApplyModifiedProperties();

        // Force scene view update when values change
        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI()
    {
        if (teamArea == null || !teamArea.ShowGizmo) return;

        // Draw move handle first (in world space)
        EditorGUI.BeginChangeCheck();
        Vector3 newPosition = Handles.PositionHandle(teamArea.transform.position, teamArea.transform.rotation);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(teamArea.transform, "Move Team Area");
            teamArea.transform.position = newPosition;
        }

        // Draw radius handle (in local space)
        Handles.color = teamArea.TeamColor;
        EditorGUI.BeginChangeCheck();
        float newRadius = Handles.RadiusHandle(teamArea.transform.rotation, teamArea.transform.position, teamArea.Radius);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(teamArea, "Change Team Area Radius");
            teamArea.Radius = Mathf.Max(0.1f, newRadius);
        }
    }
}