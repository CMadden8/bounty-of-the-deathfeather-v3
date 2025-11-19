#if UNITY_EDITOR
using System.Linq;
using CombatPOC.Units;
using TurnBasedStrategyFramework.Unity.Cells;
using TurnBasedStrategyFramework.Unity.Controllers;
using TurnBasedStrategyFramework.Unity.Players;
using TurnBasedStrategyFramework.Unity.Units;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CombatPOC.Editor
{
    /// <summary>
    /// Provides a one-click menu action to build the minimal MVP scene setup:
    /// - ensures required managers exist and are assigned to the grid controller
    /// - creates two placeholder players (Player 0 = Human, Player 1 = Human)
    /// - spawns two simple units and pins them to the first two painted cells
    /// </summary>
    public static class MinimalCombatSceneBuilder
    {
        private const string MenuPath = "Tools/Combat POC/Build Minimal MVP Setup";

        [MenuItem(MenuPath)]
        public static void Build()
        {
            var controller = Object.FindObjectOfType<UnityGridController>();
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Minimal MVP Setup", "No UnityGridController found in the open scene.", "OK");
                return;
            }

            var cellManager = Object.FindObjectOfType<RegularCellManager>();
            if (cellManager == null)
            {
                EditorUtility.DisplayDialog("Minimal MVP Setup", "No RegularCellManager found. Create/paint at least two cells first.", "OK");
                return;
            }

            var availableCells = cellManager.GetComponentsInChildren<Cell>().ToList();
            if (availableCells.Count < 5)
            {
                EditorUtility.DisplayDialog("Minimal MVP Setup", "Need at least five painted cells to position the default POC units (3 heroes + 2 enemies).", "OK");
                return;
            }

            Undo.IncrementCurrentGroup();
            var undoGroup = Undo.GetCurrentGroup();

            var playerManager = EnsureChildComponent<UnityPlayerManager>(controller.transform, "PlayerManager");
            var unitManager = EnsureChildComponent<UnityUnitManager>(controller.transform, "UnitManager");
            var turnResolver = EnsureChildComponent<SubsequentTurnResolver>(controller.transform, "TurnResolver");

            controller.CellManager = cellManager;
            controller.PlayerManager = playerManager;
            controller.UnitManager = unitManager;
            controller.TurnResolver = turnResolver;

            EnsurePlayer(playerManager.transform, "Player_0", 0, typeof(HumanPlayer));
            EnsurePlayer(playerManager.transform, "Player_1", 1, typeof(HumanPlayer));

            // Spawn three player heroes (Tharl, Bishep, Mirashala)
            EnsureUnit(unitManager.transform, "Tharl", 0, availableCells[0]);
            EnsureUnit(unitManager.transform, "Bishep", 0, availableCells[1]);
            EnsureUnit(unitManager.transform, "Mirashala", 0, availableCells[2]);

            // Spawn two enemy units (Groctopod, Medusa)
            EnsureUnit(unitManager.transform, "Groctopod", 1, availableCells[3]);
            EnsureUnit(unitManager.transform, "Medusa", 1, availableCells[4]);

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(playerManager);
            EditorUtility.SetDirty(unitManager);
            EditorUtility.SetDirty(turnResolver.gameObject);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("Combat POC: Minimal MVP setup complete.");
        }

        private static T EnsureChildComponent<T>(Transform parent, string childName) where T : Component
        {
            var existing = parent.GetComponentsInChildren<T>(true).FirstOrDefault();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(childName, typeof(T));
            Undo.RegisterCreatedObjectUndo(go, "Create " + childName);
            go.transform.SetParent(parent, false);
            return go.GetComponent<T>();
        }

        private static Player EnsurePlayer(Transform parent, string name, int playerNumber, System.Type playerType)
        {
            var existing = parent.GetComponentsInChildren<Player>(true)
                                  .FirstOrDefault(p => p.PlayerNumber == playerNumber);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.transform.SetParent(parent, false);
            var player = go.AddComponent(playerType) as Player;
            player.PlayerNumber = playerNumber;
            EditorUtility.SetDirty(player);
            return player;
        }

        private static SimpleUnit EnsureUnit(Transform parent, string name, int playerNumber, Cell targetCell)
        {
            // Find by name first so we can have multiple units per player
            var existing = parent.GetComponentsInChildren<SimpleUnit>(true)
                                 .FirstOrDefault(u => string.Equals(u.name, name, System.StringComparison.InvariantCultureIgnoreCase));
            if (existing != null)
            {
                // Ensure player number is correct and snap to target cell
                existing.PlayerNumber = playerNumber;
                
                // Re-apply templates to existing units to update stats
                try
                {
                    var template = BountyOfTheDeathfeather.CombatSystem.UnitTemplates.GetTemplate(name);
                    if (template != null)
                    {
                        if (template.MaxLifeHP > 0)
                        {
                            existing.Health = template.MaxLifeHP;
                            existing.MaxHealth = template.MaxLifeHP;
                        }
                        existing.ActionPoints = template.ActionPoints;
                        existing.MaxActionPoints = template.ActionPoints;
                        existing.MovementPoints = template.MovementPoints;
                        existing.MaxMovementPoints = template.MovementPoints;
                        existing.AttackFactor = template.AttackFactor;
                        existing.DefenceFactor = template.DefenceFactor;
                        existing.AttackRange = template.AttackRange;
                        
                        // Update CombatIdentity as well
                        var identity = existing.GetComponent<BountyOfTheDeathfeather.CombatSystem.CombatIdentity>();
                        if (identity == null)
                        {
                            identity = existing.gameObject.AddComponent<BountyOfTheDeathfeather.CombatSystem.CombatIdentity>();
                        }
                        identity.MaxLifeHP = template.MaxLifeHP;
                        identity.LifeHP = template.MaxLifeHP;
                        identity.ActionPoints = template.ActionPoints;
                        identity.MovementPoints = template.MovementPoints;
                        identity.AttackPower = template.AttackFactor;
                        identity.AttackRange = template.AttackRange;
                        identity.PrimaryDamageType = BountyOfTheDeathfeather.CombatSystem.DamageType.Piercing;
                        if (string.Equals(name, "Medusa", System.StringComparison.InvariantCultureIgnoreCase))
                        {
                            identity.PrimaryDamageType = BountyOfTheDeathfeather.CombatSystem.DamageType.Slashing;
                        }
                        var armour = template.Armour;
                        identity.ArmourPiercing = armour.Piercing;
                        identity.ArmourSlashing = armour.Slashing;
                        identity.ArmourBludgeoning = armour.Bludgeoning;
                        
                        EditorUtility.SetDirty(identity);
                    }
                }
                catch
                {
                    // ignore template errors
                }
                
                SnapUnitToCell(existing, targetCell);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.name = name;
            // Parent under the UnitManager so created units are registered in the hierarchy
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * 0.8f;

            var simpleUnit = go.AddComponent<SimpleUnit>();
            simpleUnit.PlayerNumber = playerNumber;
            // Default values (may be overridden by templates)
            simpleUnit.MovementPoints = 10f;
            simpleUnit.MaxMovementPoints = 10f;
            simpleUnit.ActionPoints = 2f;
            simpleUnit.MaxActionPoints = 2f;
            simpleUnit.MovementAnimationSpeed = 2f;

            // Apply named templates for the POC cast
            try
            {
                var template = BountyOfTheDeathfeather.CombatSystem.UnitTemplates.GetTemplate(name);
                if (template != null)
                {
                    if (template.MaxLifeHP > 0)
                    {
                        simpleUnit.Health = template.MaxLifeHP;
                        simpleUnit.MaxHealth = template.MaxLifeHP;
                    }
                    simpleUnit.ActionPoints = template.ActionPoints;
                    simpleUnit.MaxActionPoints = template.ActionPoints;
                    simpleUnit.MovementPoints = template.MovementPoints;
                    simpleUnit.MaxMovementPoints = template.MovementPoints;
                    simpleUnit.AttackFactor = template.AttackFactor;
                    simpleUnit.DefenceFactor = template.DefenceFactor;
                    simpleUnit.AttackRange = template.AttackRange;
                }
            }
            catch
            {
                // Templates are optional; ignore failures in editor if file not present
            }
            SnapUnitToCell(simpleUnit, targetCell);
            // Ensure the unit GameObject is parented under the UnitManager root for visibility
            go.transform.SetParent(parent, false);

            // Attach / populate CombatIdentity with template values if available
            try
            {
                var template = BountyOfTheDeathfeather.CombatSystem.UnitTemplates.GetTemplate(name);
                var identity = go.GetComponent<BountyOfTheDeathfeather.CombatSystem.CombatIdentity>();
                if (identity == null)
                {
                    identity = go.AddComponent<BountyOfTheDeathfeather.CombatSystem.CombatIdentity>();
                }

                if (template != null)
                {
                    identity.MaxLifeHP = template.MaxLifeHP;
                    identity.LifeHP = template.MaxLifeHP;
                    identity.ActionPoints = template.ActionPoints;
                    identity.MovementPoints = template.MovementPoints;
                    identity.AttackPower = template.AttackFactor;
                    identity.AttackRange = template.AttackRange;
                    // Map a naive damage type choice: heroes use Piercing by default; enemies may vary
                    identity.PrimaryDamageType = BountyOfTheDeathfeather.CombatSystem.DamageType.Piercing;
                    if (string.Equals(name, "Medusa", System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        identity.PrimaryDamageType = BountyOfTheDeathfeather.CombatSystem.DamageType.Slashing;
                    }
                    var armour = template.Armour;
                    identity.ArmourPiercing = armour.Piercing;
                    identity.ArmourSlashing = armour.Slashing;
                    identity.ArmourBludgeoning = armour.Bludgeoning;
                }
            }
            catch
            {
                // ignore
            }
            EditorUtility.SetDirty(simpleUnit);

            return simpleUnit;
        }

        private static void SnapUnitToCell(SimpleUnit unit, Cell cell)
        {
            if (cell == null || unit == null)
            {
                return;
            }

            unit.CurrentCell = cell;
            cell.IsTaken = true;
            cell.CurrentUnits.Add(unit);
            
            // Position unit at cell's center
            // Cell prefabs have root at origin and visual at (0.5, 0, 0.5)
            // WorldPosition = transform.position (root position)
            // Add CellDimensions/2 to get center, then add vertical offset
            Vector3 cellCenter = cell.transform.position + new Vector3(
                cell.CellDimensions.x * 0.5f,
                0,
                cell.CellDimensions.z * 0.5f
            );
            unit.transform.position = cellCenter + Vector3.up * 0.55f;
            
            // Mark both as dirty so Unity saves the changes
            EditorUtility.SetDirty(unit);
            EditorUtility.SetDirty(cell);
        }
    }
}
#endif
