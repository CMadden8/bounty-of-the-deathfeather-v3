#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AssignUnitMaterials
{
    [MenuItem("Tools/Combat POC/Assign Simple Unit Materials")]
    public static void Assign()
    {
        string materialsFolder = "Assets/Scenes/CombatPOC/Prefabs/Materials";
        string redPath = System.IO.Path.Combine(materialsFolder, "SimpleMaterialRed.mat");
        string bluePath = System.IO.Path.Combine(materialsFolder, "SimpleMaterialBlue.mat");

        Material redMat = AssetDatabase.LoadAssetAtPath<Material>(redPath);
        if (redMat == null)
        {
            redMat = new Material(Shader.Find("Standard")) { name = "SimpleMaterialRed" };
            redMat.color = new Color(0.83f, 0.21f, 0.21f);
            AssetDatabase.CreateAsset(redMat, redPath);
            AssetDatabase.SaveAssets();
        }

        Material blueMat = AssetDatabase.LoadAssetAtPath<Material>(bluePath);
        if (blueMat == null)
        {
            blueMat = new Material(Shader.Find("Standard")) { name = "SimpleMaterialBlue" };
            blueMat.color = new Color(0.3f, 0.5f, 0.8f);
            AssetDatabase.CreateAsset(blueMat, bluePath);
            AssetDatabase.SaveAssets();
        }

        AssignMaterialToPrefab("Assets/Scenes/CombatPOC/Prefabs/Units/Unit_Enemy.prefab", redMat);
        AssignMaterialToPrefab("Assets/Scenes/CombatPOC/Prefabs/Units/Unit_Player.prefab", blueMat);

        AssetDatabase.Refresh();
        Debug.Log("Assigned materials to Unit prefabs.");
    }

    private static void AssignMaterialToPrefab(string prefabPath, Material mat)
    {
        if (!System.IO.File.Exists(prefabPath))
        {
            Debug.LogWarning($"Prefab not found: {prefabPath}");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        var renderer = root.GetComponentInChildren<Renderer>(true);
        if (renderer == null)
        {
            Debug.LogWarning($"No Renderer found on prefab: {prefabPath}");
            PrefabUtility.UnloadPrefabContents(root);
            return;
        }

        renderer.sharedMaterial = mat;
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log($"Set material on {prefabPath}");
    }
}
#endif
