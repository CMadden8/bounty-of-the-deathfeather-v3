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
            // Create root GameObject for the prefab (root stays at local origin)
            GameObject cellObj = new GameObject(prefabName);

            // Create a visual cube as a child so the prefab root remains at (0,0,0)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "model";
            visual.transform.SetParent(cellObj.transform, false);

            // IMPORTANT: Position visual at grid center offset for proper alignment
            // For XZY swizzle (3D top-down), visuals should be centered at (0.5, 0, 0.5)
            visual.transform.localPosition = new Vector3(0.5f, 0f, 0.5f);
            
            // Scale to tile size (thin for ground tile, 0.9 to leave small gaps)
            visual.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);

            // Add Square component to the prefab root
            Square square = cellObj.AddComponent<Square>();
            
            // Set CellDimensions to 1x1 to match grid
            // This is read by grid generators and the brush
            var cellDimensionsField = typeof(Square).GetField("CellDimensions", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (cellDimensionsField != null)
            {
                cellDimensionsField.SetValue(square, new Vector3(1f, 1f, 1f));
            }
            
            // Add Highlighters for visual feedback; target the visual renderer
            var visualRenderer = visual.GetComponent<MeshRenderer>();
            CreateHighlighters(cellObj, square, color, visualRenderer);
            
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
            
            // Assign the shared material to the visual mesh so prefabs reference the asset directly
            visual.GetComponent<MeshRenderer>().sharedMaterial = mat;
            
            // Ensure BoxCollider exists on the visual child (CreatePrimitive added it)
            BoxCollider collider = visual.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = visual.AddComponent<BoxCollider>();
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
        
        /// <summary>
        /// Creates highlighter GameObjects for cell visual feedback.
        /// Cells need these to show reachable/path/selected states.
        /// </summary>
        private static void CreateHighlighters(GameObject cellObj, Square square, Color baseColor, MeshRenderer renderer)
        {
            // Create Highlighters container
            GameObject highlightersContainer = new GameObject("Highlighters");
            highlightersContainer.transform.SetParent(cellObj.transform, false);
            
            // renderer passed in references the visual child's MeshRenderer
            
            // UnMark - restore to the cell's base material color (not white, preserves the cell color)
            GameObject unMarkObj = CreateRendererHighlighter("UnMark", highlightersContainer, renderer, baseColor);
            
            // MarkAsHighlighted - for mouse hover (blue)
            GameObject highlightedObj = CreateRendererHighlighter("MarkAsHighlighted", highlightersContainer, renderer, new Color(0.5f, 0.7f, 1f));
            
            // MarkAsReachable - for movement range (yellow)
            GameObject reachableObj = CreateRendererHighlighter("MarkAsReachable", highlightersContainer, renderer, new Color(1f, 0.9f, 0.4f));
            
            // MarkAsPath - for movement path (green)
            GameObject pathObj = CreateRendererHighlighter("MarkAsPath", highlightersContainer, renderer, new Color(0.5f, 1f, 0.5f));
            
            // Assign highlighters to Square component via reflection (fields are serialized)
            var squareType = typeof(Square);
            var baseType = squareType.BaseType; // Cell class
            
            AssignHighlighterField(baseType, square, "_unMarkFn", unMarkObj);
            AssignHighlighterField(baseType, square, "_markAsHighlightedFn", highlightedObj);
            AssignHighlighterField(baseType, square, "_markAsReachableFn", reachableObj);
            AssignHighlighterField(baseType, square, "_markAsPathFn", pathObj);
        }
        
        private static GameObject CreateRendererHighlighter(string name, GameObject parent, MeshRenderer targetRenderer, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            
            var highlighter = obj.AddComponent<TurnBasedStrategyFramework.Unity.Highlighters.RendererHighlighter>();
            
            // Set fields via reflection
            var highlighterType = highlighter.GetType();
            var colorField = highlighterType.GetField("_color", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var rendererField = highlighterType.GetField("_renderer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (colorField != null) colorField.SetValue(highlighter, color);
            if (rendererField != null) rendererField.SetValue(highlighter, targetRenderer);
            
            return obj;
        }
        
        private static void AssignHighlighterField(System.Type cellType, Square square, string fieldName, GameObject highlighterObj)
        {
            var field = cellType.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                var highlighter = highlighterObj.GetComponent<TurnBasedStrategyFramework.Unity.Highlighters.RendererHighlighter>();
                // The field is a List<Highlighter> where Highlighter is the Unity.Highlighters base class
                var list = new System.Collections.Generic.List<TurnBasedStrategyFramework.Unity.Highlighters.Highlighter> { highlighter };
                field.SetValue(square, list);
            }
        }
    }
}
#endif
