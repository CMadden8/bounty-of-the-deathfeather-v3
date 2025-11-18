using UnityEngine;
using TurnBasedStrategyFramework.Unity.Cells;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.Rendering;

namespace CombatPOC.Editor
{
    /// <summary>
    /// Editor utility to create a simple 3D cube cell prefab for RegularCellManager.
    /// Usage: Tools → Combat POC → Create Simple 3D Cell Prefab
    /// </summary>
    public class SimpleCubeCellCreator
    {
        [MenuItem("Tools/Combat POC/Create Simple 3D Cell Prefab (Green)")]
        public static void CreateSimple3DCellPrefabGreen()
        {
            CreateSimple3DCellPrefab(new Color(0.3f, 0.7f, 0.3f), "SimpleCubeCell_Green");
        }

        [MenuItem("Tools/Combat POC/Create Simple 3D Cell Prefab (Blue)")]
        public static void CreateSimple3DCellPrefabBlue()
        {
            CreateSimple3DCellPrefab(new Color(0.3f, 0.5f, 0.8f), "SimpleCubeCell_Blue");
        }

        [MenuItem("Tools/Combat POC/Create Simple 3D Cell Prefab (Gray)")]
        public static void CreateSimple3DCellPrefabGray()
        {
            CreateSimple3DCellPrefab(new Color(0.5f, 0.5f, 0.5f), "SimpleCubeCell_Gray");
        }

        private static void CreateSimple3DCellPrefab(Color color, string prefabName)
        {
            // Create root GameObject
            GameObject cellObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cellObj.name = prefabName;
            
            // IMPORTANT: Position at grid center offset for proper alignment
            // For XZY swizzle (3D top-down), cells should be centered at (0.5, 0, 0.5)
            cellObj.transform.position = new Vector3(0.5f, 0f, 0.5f);
            
            // Scale to tile size (thin for ground tile, 0.9 to leave small gaps)
            cellObj.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);
            
            // Add Square component
            Square square = cellObj.AddComponent<Square>();
            
            // Set CellDimensions to 1x1 to match grid
            // This is read by grid generators and the brush
            var cellDimensionsField = typeof(Square).GetField("CellDimensions", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (cellDimensionsField != null)
            {
                cellDimensionsField.SetValue(square, new Vector3(1f, 1f, 1f));
            }
            
            // Create or update a material that matches the active render pipeline
            var renderPipeline = GraphicsSettings.currentRenderPipeline;
            Shader shader = null;
            if (renderPipeline == null)
            {
                shader = Shader.Find("Standard");
            }
            else
            {
                shader = renderPipeline.defaultShader;
                if (shader == null && renderPipeline.GetType().Name.Contains("Universal"))
                {
                    shader = Shader.Find("Universal Render Pipeline/Lit");
                }
            }
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            }
            if (shader == null)
            {
                Debug.LogError("SimpleCubeCellCreator: Unable to locate a compatible shader. Prefab will render magenta.");
            }
            
            // Save material as an asset so it persists across prefab reloads
            string materialPath = $"Assets/Scenes/CombatPOC/Prefabs/Materials/{prefabName}_Mat.mat";
            string materialDir = System.IO.Path.GetDirectoryName(materialPath);
            if (!System.IO.Directory.Exists(materialDir))
            {
                System.IO.Directory.CreateDirectory(materialDir);
            }
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (mat == null)
            {
                mat = new Material(shader == null ? Shader.Find("Standard") : shader);
                AssetDatabase.CreateAsset(mat, materialPath);
            }
            else if (shader != null)
            {
                mat.shader = shader;
            }
            mat.color = color;
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            
            // Assign the shared material so prefabs reference the asset directly
            cellObj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            
            // Ensure BoxCollider exists (CreatePrimitive adds it)
            BoxCollider collider = cellObj.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = cellObj.AddComponent<BoxCollider>();
            }
            
            // Save as prefab
            string prefabPath = $"Assets/Scenes/CombatPOC/Prefabs/{prefabName}.prefab";
            
            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(cellObj, prefabPath);
            
            // Clean up temp GameObject
            GameObject.DestroyImmediate(cellObj);
            
            // Refresh asset database to ensure material is linked
            AssetDatabase.Refresh();
            
            // Select and ping the new prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            
            Debug.Log($"Created 3D cell prefab at: {prefabPath} with material at: {materialPath}");
        }
    }
}
#endif
