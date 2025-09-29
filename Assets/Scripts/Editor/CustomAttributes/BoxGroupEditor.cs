using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(MonoBehaviour), true)]
public class BoxGroupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        Dictionary<string, List<SerializedProperty>> groups = new();

        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;

        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;

            var attr = GetBoxGroupAttribute(property);
            if (attr != null)
            {
                if (!groups.ContainsKey(attr.GroupName))
                    groups[attr.GroupName] = new List<SerializedProperty>();

                groups[attr.GroupName].Add(property.Copy());
            }
            else
            {
                EditorGUILayout.PropertyField(property, true);
            }
        }

        foreach (var kvp in groups)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(kvp.Key, EditorStyles.boldLabel);

            foreach (var p in kvp.Value)
                EditorGUILayout.PropertyField(p, true);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private BoxGroupAttribute GetBoxGroupAttribute(SerializedProperty property)
    {
        var targetType = property.serializedObject.targetObject.GetType();
        var field = targetType.GetField(property.name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        if (field == null) return null;
        var attrs = field.GetCustomAttributes(typeof(BoxGroupAttribute), true);
        if (attrs.Length > 0) return attrs[0] as BoxGroupAttribute;
        return null;
    }
}
