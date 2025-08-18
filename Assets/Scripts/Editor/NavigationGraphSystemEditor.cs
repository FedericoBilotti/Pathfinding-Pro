using UnityEditor;
using UnityEngine;

namespace NavigationGraph
{
    [CustomEditor(typeof(NavigationGraphSystem))]
    public class NavigationGraphSystemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
             DrawDefaultInspector();

            NavigationGraphSystem visualizer = (NavigationGraphSystem)target;

            GUILayout.Space(10);

            if (GUILayout.Button("Scan Grid"))
            {
                visualizer.Clear();
                visualizer.Scan();
            }
        }
    }
}