using UnityEditor;
using UnityEngine;

namespace NavigationGraph
{
    [CustomEditor(typeof(NavigationGraphSystem))]
    public class NavigationGraphSystemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            NavigationGraphSystem visualizer = (NavigationGraphSystem)target;

            DrawCheckType(visualizer);

            GUILayout.Space(10);

            if (GUILayout.Button("Scan Grid"))
            {
                visualizer.Clear();
                visualizer.Scan();
            }

            if (GUILayout.Button("Destroy Grid"))
            {
                visualizer.Clear();
            }

            if (GUILayout.Button("Bake Grid Asset"))
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Baked Grid Asset",
                    "NewBakedGrid",
                    "asset",
                    "Please enter a file name to save the grid asset to",
                    "Assets/BakedGrids"
                );

                if (!string.IsNullOrEmpty(path))
                {
                    GridDataAsset asset = visualizer.BakeGridAsset(path);
                    visualizer.SetBakeGrid(asset);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            SceneView.RepaintAll();
        }

        private void DrawCheckType(NavigationGraphSystem visualizer)
        {
            if (visualizer.RaycastCheckType == RaycastType.Capsule)
            {
                visualizer.Radius = EditorGUILayout.FloatField("Radius", visualizer.Radius);
                visualizer.Height = EditorGUILayout.FloatField("Height", visualizer.Height);
            }
            else if (visualizer.RaycastCheckType == RaycastType.Sphere)
            {
                visualizer.Radius = EditorGUILayout.FloatField("Radius", visualizer.Radius);
            }
        }
    }
}