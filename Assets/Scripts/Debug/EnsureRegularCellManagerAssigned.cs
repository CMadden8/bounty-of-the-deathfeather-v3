#if UNITY_EDITOR
using TurnBasedStrategyFramework.Unity.Cells;
using TurnBasedStrategyFramework.Unity.Controllers;
using UnityEditor;
using UnityEngine;

namespace BountyOfTheDeathfeather.Debugging
{
    /// <summary>
    /// Editor utility that ensures UnityGridController has a RegularCellManager assigned.
    /// Run this via menu: Tools/Combat POC/Ensure RegularCellManager Assigned
    /// </summary>
    public static class EnsureRegularCellManagerAssigned
    {
        [MenuItem("Tools/Combat POC/Fix All Scene Setup Issues")]
        public static void FixAllSceneIssues()
        {
            Debug.Log("=== Combat POC Scene Setup Auto-Fix ===");
            ValidateSceneSetup();
            Debug.Log("=== Auto-Fix Complete ===");
        }

        [MenuItem("Tools/Combat POC/Ensure RegularCellManager Assigned")]
        public static void EnsureAssignment()
        {
            var gridController = Object.FindFirstObjectByType<UnityGridController>();
            if (gridController == null)
            {
                EditorUtility.DisplayDialog("RegularCellManager Check", "No UnityGridController found in the scene.", "OK");
                return;
            }

            var cellManager = gridController.CellManager as UnityCellManager;
            if (cellManager != null)
            {
                EditorUtility.DisplayDialog("RegularCellManager Check", $"UnityGridController already has a CellManager assigned: {cellManager.GetType().Name}", "OK");
                return;
            }

            // Try to find a RegularCellManager in the scene
            var regularCellManager = Object.FindFirstObjectByType<RegularCellManager>();
            if (regularCellManager == null)
            {
                // Create one as a child of the grid controller
                var go = new GameObject("RegularCellManager");
                Undo.RegisterCreatedObjectUndo(go, "Create RegularCellManager");
                go.transform.SetParent(gridController.transform, false);
                regularCellManager = go.AddComponent<RegularCellManager>();
                Debug.Log("Created new RegularCellManager as child of UnityGridController");
            }

            // Assign it
            gridController.CellManager = regularCellManager;
            EditorUtility.SetDirty(gridController);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log($"Assigned RegularCellManager to UnityGridController.CellManager");
            EditorUtility.DisplayDialog("RegularCellManager Check", "RegularCellManager has been assigned to UnityGridController.CellManager", "OK");
        }

        [MenuItem("Tools/Combat POC/Validate Scene Setup")]
        public static void ValidateSceneSetup()
        {
            var gridController = Object.FindFirstObjectByType<UnityGridController>();
            if (gridController == null)
            {
                Debug.LogError("No UnityGridController found in scene");
                return;
            }

            bool anyChanges = false;

            // 1. Check/Fix CellManager
            var cellManager = gridController.CellManager as UnityCellManager;
            if (cellManager == null)
            {
                Debug.LogWarning("UnityGridController has no UnityCellManager assigned - attempting auto-fix...");
                
                var regularCellManager = Object.FindFirstObjectByType<RegularCellManager>();
                if (regularCellManager == null)
                {
                    var go = new GameObject("RegularCellManager");
                    Undo.RegisterCreatedObjectUndo(go, "Create RegularCellManager");
                    go.transform.SetParent(gridController.transform, false);
                    regularCellManager = go.AddComponent<RegularCellManager>();
                    Debug.Log("Created new RegularCellManager as child of UnityGridController");
                }
                
                gridController.CellManager = regularCellManager;
                anyChanges = true;
                Debug.Log("✓ Assigned RegularCellManager to UnityGridController.CellManager");
            }
            else
            {
                Debug.Log($"✓ UnityGridController.CellManager = {cellManager.GetType().Name}");
                
                var cells = cellManager.GetCells();
                if (cells == null)
                {
                    Debug.LogWarning("CellManager.GetCells() returned null (may not be initialized yet)");
                }
                else
                {
                    int cellCount = 0;
                    foreach (var cell in cells)
                    {
                        cellCount++;
                    }
                    Debug.Log($"  → CellManager has {cellCount} cells");
                }
            }

            // 2. Check/Fix UnitManager
            if (gridController.UnitManager == null)
            {
                Debug.LogWarning("UnityGridController has no UnitManager assigned - attempting auto-fix...");
                
                var unitManager = Object.FindFirstObjectByType<TurnBasedStrategyFramework.Unity.Units.UnityUnitManager>();
                if (unitManager == null)
                {
                    var go = new GameObject("UnitManager");
                    Undo.RegisterCreatedObjectUndo(go, "Create UnitManager");
                    go.transform.SetParent(gridController.transform, false);
                    unitManager = go.AddComponent<TurnBasedStrategyFramework.Unity.Units.UnityUnitManager>();
                    Debug.Log("Created new UnityUnitManager as child of UnityGridController");
                }
                
                gridController.UnitManager = unitManager;
                anyChanges = true;
                Debug.Log("✓ Assigned UnityUnitManager to UnityGridController.UnitManager");
            }
            else
            {
                Debug.Log($"✓ UnityGridController.UnitManager assigned");
            }

            // 3. Check/Fix PlayerManager
            if (gridController.PlayerManager == null)
            {
                Debug.LogWarning("UnityGridController has no PlayerManager assigned - attempting auto-fix...");
                
                var playerManager = Object.FindFirstObjectByType<TurnBasedStrategyFramework.Unity.Players.UnityPlayerManager>();
                if (playerManager == null)
                {
                    var go = new GameObject("PlayerManager");
                    Undo.RegisterCreatedObjectUndo(go, "Create PlayerManager");
                    go.transform.SetParent(gridController.transform, false);
                    playerManager = go.AddComponent<TurnBasedStrategyFramework.Unity.Players.UnityPlayerManager>();
                    Debug.Log("Created new UnityPlayerManager as child of UnityGridController");
                }
                
                gridController.PlayerManager = playerManager;
                anyChanges = true;
                Debug.Log("✓ Assigned UnityPlayerManager to UnityGridController.PlayerManager");
            }
            else
            {
                Debug.Log($"✓ UnityGridController.PlayerManager assigned");
            }

            // 4. Check/Fix TurnResolver
            if (gridController.TurnResolver == null)
            {
                Debug.LogWarning("UnityGridController has no TurnResolver assigned - attempting auto-fix...");
                
                var turnResolver = Object.FindFirstObjectByType<SubsequentTurnResolver>();
                if (turnResolver == null)
                {
                    var go = new GameObject("SubsequentTurnResolver");
                    Undo.RegisterCreatedObjectUndo(go, "Create TurnResolver");
                    go.transform.SetParent(gridController.transform, false);
                    turnResolver = go.AddComponent<SubsequentTurnResolver>();
                    Debug.Log("Created new SubsequentTurnResolver as child of UnityGridController");
                }
                
                gridController.TurnResolver = turnResolver;
                anyChanges = true;
                Debug.Log("✓ Assigned SubsequentTurnResolver to UnityGridController.TurnResolver");
            }
            else
            {
                Debug.Log($"✓ UnityGridController.TurnResolver assigned");
            }

            // Save changes if any were made
            if (anyChanges)
            {
                EditorUtility.SetDirty(gridController);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                Debug.Log("Scene setup validated and auto-fixed. Scene marked dirty - remember to save!");
            }
            else
            {
                Debug.Log("Scene setup validation complete - all managers properly assigned!");
            }
        }
    }
}
#endif
