using Agents;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AgentNavigation), true)]
[CanEditMultipleObjects]
public class AgentNavigationGUIEditor : Editor
{
    private SerializedProperty _allowRepath;
    private SerializedProperty _rePath;
    private bool _showPath = true;

    void OnEnable()
    {
        _allowRepath = serializedObject.FindProperty("allowRePath");
        _rePath = serializedObject.FindProperty("rePath");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        base.OnInspectorGUI();

        DrawRePath();
        DrawDebug();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRePath()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Pathfinding Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_allowRepath);

        if (_allowRepath.boolValue)
        {
            EditorGUILayout.PropertyField(_rePath);
        }
    }

    private void DrawDebug()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

        _showPath = EditorGUILayout.Toggle("Show Path", _showPath);
    }

    private void OnSceneGUI()
    {
        AgentNavigation agent = (AgentNavigation)target;

        Handles.color = Color.red;
        Handles.DrawWireDisc(agent.transform.position + Vector3.up, Vector3.up, agent.ChangeWaypointDistance);

        Handles.color = Color.black;
        Handles.DrawWireDisc(agent.transform.position + Vector3.up, Vector3.up, agent.StoppingDistance);

        if (!_showPath) return;
        if (!agent.HasPath) return;

        for (int i = agent.CurrentWaypoint; i < agent.WaypointsPath.Count; i++)
        {
            Handles.color = Color.black;
            Handles.DrawLine(i == agent.CurrentWaypoint ? agent.transform.position : agent.WaypointsPath[i - 1], agent.WaypointsPath[i]);
            // Handles.CubeHandleCap(0, agent.WaypointsPath[i], Quaternion.identity, 0.1f, EventType.Repaint);
            Handles.color = Color.blue;
            Handles.DrawLine(agent.transform.position, agent.WaypointsPath[agent.CurrentWaypoint]);
        }
    }
}