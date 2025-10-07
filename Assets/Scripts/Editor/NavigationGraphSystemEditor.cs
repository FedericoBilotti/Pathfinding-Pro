using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

namespace NavigationGraph
{
    [CustomEditor(typeof(NavigationGraphSystem))]
    public class NavigationGraphSystemEditor : Editor
    {
        SerializedProperty boxGridProp;
        SerializedProperty graphTypeProp;
        SerializedProperty neighborsPerCellProp;
        SerializedProperty gridSizeProp;
        SerializedProperty cellSizeProp;
        //SerializedProperty maxHeightDifferenceProp;
        SerializedProperty obstacleMarginProp;
        SerializedProperty cliffMarginProp;
        SerializedProperty terrainTypesProp;
        SerializedProperty inclineLimitProp;
        SerializedProperty ignoreMaskAtCreateGridProp;
        SerializedProperty notWalkableMaskProp;
        SerializedProperty gridBakedProp;
        SerializedProperty raycastCheckTypeProp;
        SerializedProperty radiusProp;
        SerializedProperty heightProp;

        private void OnEnable()
        {
            // Serializados
            boxGridProp = serializedObject.FindProperty("_boxGrid");
            graphTypeProp = serializedObject.FindProperty("_graphType");
            neighborsPerCellProp = serializedObject.FindProperty("_neighborsPerCell");
            gridSizeProp = serializedObject.FindProperty("_gridSize");
            cellSizeProp = serializedObject.FindProperty("_cellSize");
            //maxHeightDifferenceProp = serializedObject.FindProperty("_maxHeightDifference");
            obstacleMarginProp = serializedObject.FindProperty("_obstacleMargin");
            cliffMarginProp = serializedObject.FindProperty("_cliffMargin");
            terrainTypesProp = serializedObject.FindProperty("_terrainTypes");
            inclineLimitProp = serializedObject.FindProperty("_inclineLimit");
            ignoreMaskAtCreateGridProp = serializedObject.FindProperty("_ignoreMaskAtCreateGrid");
            notWalkableMaskProp = serializedObject.FindProperty("_notWalkableMask");
            gridBakedProp = serializedObject.FindProperty("_gridBaked");
            raycastCheckTypeProp = serializedObject.FindProperty("_raycastCheckType");
            radiusProp = serializedObject.FindProperty("_radius");
            heightProp = serializedObject.FindProperty("_height");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            NavigationGraphSystem visualizer = (NavigationGraphSystem)target;

            Gizmos();
            GraphSettings();
            ObstaclesSettings();
            
            EditorGUILayout.BeginVertical("box");
            GridAsset();
            Buttons(visualizer);
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private void GridAsset()
        {
            EditorGUILayout.PropertyField(gridBakedProp);
        }

        private void Buttons(NavigationGraphSystem visualizer)
        {
            if (GUILayout.Button("Scan Grid")) { visualizer.Clear(); visualizer.Scan(); }
            if (GUILayout.Button("Delete Scan Grid")) { visualizer.Clear(); }
            if (GUILayout.Button("Bake Grid Asset")) BakeGridAsset(visualizer);
            if (GUILayout.Button("Delete Grid Asset")) DeleteGridAsset(visualizer);
        }

        private void ObstaclesSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Obstacle Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(inclineLimitProp);
            EditorGUILayout.PropertyField(ignoreMaskAtCreateGridProp);
            EditorGUILayout.PropertyField(notWalkableMaskProp);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
        }


        private void GraphSettings()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Graph Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(graphTypeProp);
            EditorGUILayout.PropertyField(neighborsPerCellProp);
            EditorGUILayout.PropertyField(gridSizeProp);
            EditorGUILayout.PropertyField(cellSizeProp);
            //EditorGUILayout.PropertyField(maxHeightDifferenceProp);
            EditorGUILayout.PropertyField(obstacleMarginProp);
            EditorGUILayout.PropertyField(cliffMarginProp);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(terrainTypesProp);
            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
        }


        private void Gizmos()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Gizmos", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(boxGridProp);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
        }


        private void OnSceneGUI()
        {
            SceneView.RepaintAll();
        }

        private void BakeGridAsset(NavigationGraphSystem visualizer)
        {
            string scenePath = SceneManager.GetActiveScene().path;
            string basePath = Path.ChangeExtension(scenePath, null);

            if (!Directory.Exists(basePath))
                Directory.CreateDirectory(basePath);

            string newPath = Path.Combine(basePath, "BakedGrid.asset");
            newPath = newPath.Replace(Application.dataPath, "Assets");

            if (string.IsNullOrEmpty(newPath))
            {
                Debug.Log("The path is Null or Empty");
                return;
            }

            GridDataAsset asset = visualizer.BakeGridAsset(newPath);
            visualizer.SetBakeGrid(asset);
        }

        private void DeleteGridAsset(NavigationGraphSystem visualizer)
        {
            if (visualizer.GridBaked == null) return;
            string path = AssetDatabase.GetAssetPath(visualizer.GridBaked);
            visualizer.DeleteGrid(path);
        }
    }
}
