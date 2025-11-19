using UnityEngine;
using UnityEditor;
using TurnBasedStrategyFramework.Unity.Units;
using BountyOfTheDeathfeather.CombatSystem.UI;

namespace BountyOfTheDeathfeather.Editor
{
    /// <summary>
    /// Editor utility to automatically add selection borders to Unit prefabs.
    /// This creates a child GameObject with a transparent material that displays a red border when the unit is attackable.
    /// </summary>
    public static class UnitBorderSetup
    {
        [MenuItem("Tools/Combat POC/Setup Unit Selection Borders")]
        public static void SetupUnitBorders()
        {
            // Find all Unit prefabs in the project
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            int unitsProcessed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                // Check if this prefab has a Unit component
                Unit unitComponent = prefab.GetComponent<Unit>();
                if (unitComponent == null) continue;

                // Check if it already has a SelectionBorder
                Transform existingBorder = prefab.transform.Find("SelectionBorder");
                UnitSelectionBorder existingBorderComponent = prefab.GetComponent<UnitSelectionBorder>();
                
                if (existingBorder != null || existingBorderComponent != null)
                {
                    Debug.Log($"[UnitBorderSetup] Skipping {prefab.name} - already has SelectionBorder.");
                    continue;
                }

                // Instantiate prefab to modify it
                GameObject prefabInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (prefabInstance == null) continue;

                // Create the border child object
                GameObject border = CreateBorder(prefabInstance);

                // Add UnitSelectionBorder component to the root
                UnitSelectionBorder borderComponent = prefabInstance.GetComponent<UnitSelectionBorder>();
                if (borderComponent == null)
                {
                    borderComponent = prefabInstance.AddComponent<UnitSelectionBorder>();
                }

                // Assign the border object via SerializedObject (to set private fields)
                SerializedObject serializedBorder = new SerializedObject(borderComponent);
                SerializedProperty borderObjectProp = serializedBorder.FindProperty("_borderObject");
                SerializedProperty borderRendererProp = serializedBorder.FindProperty("_borderRenderer");
                
                borderObjectProp.objectReferenceValue = border;
                borderRendererProp.objectReferenceValue = border.GetComponent<Renderer>();
                serializedBorder.ApplyModifiedProperties();

                // Apply changes back to prefab
                PrefabUtility.ApplyPrefabInstance(prefabInstance, InteractionMode.AutomatedAction);
                Object.DestroyImmediate(prefabInstance);

                unitsProcessed++;
                Debug.Log($"[UnitBorderSetup] Added SelectionBorder to {prefab.name}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UnitBorderSetup] Processed {unitsProcessed} unit prefabs.");
        }

        private static GameObject CreateBorder(GameObject unitPrefab)
        {
            // Create child GameObject
            GameObject border = new GameObject("SelectionBorder");
            border.transform.SetParent(unitPrefab.transform);
            border.transform.localPosition = new Vector3(0, 0.02f, 0); // Slightly above ground
            border.transform.localRotation = Quaternion.Euler(90, 0, 0); // Rotate to lie flat
            border.transform.localScale = new Vector3(1.2f, 1.2f, 1f); // Slightly larger than unit base

            // Add LineRenderer for circular border
            LineRenderer lineRenderer = border.AddComponent<LineRenderer>();
            ConfigureLineRenderer(lineRenderer);

            // Start disabled (will be enabled when attackable)
            border.SetActive(false);

            return border;
        }

        private static void ConfigureLineRenderer(LineRenderer lineRenderer)
        {
            // Create circle points
            int segments = 32;
            float radius = 0.5f;
            lineRenderer.positionCount = segments + 1;
            lineRenderer.loop = true;

            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                lineRenderer.SetPosition(i, new Vector3(x, y, 0));
            }

            // Configure appearance
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.useWorldSpace = false;

            // Assign material
            Material borderMaterial = CreateBorderMaterial();
            lineRenderer.material = borderMaterial;
            lineRenderer.startColor = new Color(1, 0, 0, 0.8f); // Red
            lineRenderer.endColor = new Color(1, 0, 0, 0.8f);

            // Disable shadows
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }

        private static Material CreateBorderMaterial()
        {
            // Check if material already exists
            string matPath = "Assets/Materials/UnitSelectionBorder.mat";
            Material existingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existingMat != null) return existingMat;

            // Create new material for the border
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.name = "UnitSelectionBorder";
            mat.color = new Color(1, 0, 0, 0.8f); // Red, semi-transparent

            // Ensure Materials folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();

            return mat;
        }
    }
}
