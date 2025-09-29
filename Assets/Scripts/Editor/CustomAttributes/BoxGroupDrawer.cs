using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomPropertyDrawer(typeof(BoxGroupAttribute))]
public class BoxGroupDrawer : PropertyDrawer
{
    private static readonly Dictionary<string, bool> foldouts = new();

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        BoxGroupAttribute attr = (BoxGroupAttribute)attribute;
        string group = attr.GroupName;

        if (!foldouts.ContainsKey(group))
            foldouts[group] = true;

        foldouts[group] = EditorGUI.Foldout(new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            foldouts[group], group, true);

        if (foldouts[group])
        {
            EditorGUI.indentLevel++;
            Rect fieldRect = new(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUI.GetPropertyHeight(property, label, true));
            EditorGUI.PropertyField(fieldRect, property, label, true);
            EditorGUI.indentLevel--;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        BoxGroupAttribute attr = (BoxGroupAttribute)attribute;
        string group = attr.GroupName;

        if (!foldouts.ContainsKey(group) || !foldouts[group])
            return EditorGUIUtility.singleLineHeight;

        return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(property, label, true);
    }
}