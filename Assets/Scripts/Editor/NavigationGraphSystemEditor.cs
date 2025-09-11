using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

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

            ScanGrid(visualizer);
            DeleteScanGrid(visualizer);
            BakeGridAsset(visualizer);
            DeleteGridAsset(visualizer);

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

        private void ScanGrid(NavigationGraphSystem visualizer)
        {
            if (GUILayout.Button("Scan Grid"))
            {
                visualizer.Clear();
                visualizer.Scan();
            }
        }

        private void DeleteScanGrid(NavigationGraphSystem visualizer)
        {
            if (GUILayout.Button("Delete Scan Grid"))
            {
                visualizer.Clear();
            }
        }

        private void BakeGridAsset(NavigationGraphSystem visualizer)
        {
            if (GUILayout.Button("Bake Grid Asset"))
            {
                string scenePath = SceneManager.GetActiveScene().path;
                string basePath = Path.ChangeExtension(scenePath, null);

                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);

                string newPath = EditorUtility.SaveFilePanelInProject(
                    "Save Baked Grid Asset",
                    "Baked Grid",
                    "asset",
                    "Please enter a file name to save the grid asset to",
                    scenePath
                );

                if (!string.IsNullOrEmpty(newPath))
                {
                    GridDataAsset asset = visualizer.BakeGridAsset(newPath);
                    visualizer.SetBakeGrid(asset);
                }
            }
        }

        private void DeleteGridAsset(NavigationGraphSystem visualizer)
        {
            if (GUILayout.Button("Delete Grid Asset"))
            {
                if (visualizer.GridBaked == null) return;

                string path = AssetDatabase.GetAssetPath(visualizer.GridBaked);
                visualizer.DeleteGrid(path);
            }
        }
    }
}