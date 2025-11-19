using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TurnBasedStrategyFramework.Common.Cells;
using TurnBasedStrategyFramework.Common.Controllers;
using TurnBasedStrategyFramework.Common.Units;
using TurnBasedStrategyFramework.Common.Units.Abilities;
using UnityEngine;
using BountyOfTheDeathfeather.CombatSystem.Tiles;

namespace BountyOfTheDeathfeather.CombatSystem.Commands
{
    /// <summary>
    /// Command for the Ice Spirit ability.
    /// Creates an ice tile area (radius 3) and applies Freezing status to units within.
    /// </summary>
    public readonly struct CombatIceSpiritCommand : ICommand
    {
        private readonly int _targetX;
        private readonly int _targetY;
        private readonly int _actionCost;
        private const int Radius = 3;

        public CombatIceSpiritCommand(ICell targetCell, int actionCost = 1)
        {
            if (targetCell == null) throw new ArgumentNullException(nameof(targetCell));
            _targetX = targetCell.GridCoordinates.x;
            _targetY = targetCell.GridCoordinates.y;
            _actionCost = actionCost;
        }

        public CombatIceSpiritCommand(int x, int y, int actionCost = 1)
        {
            _targetX = x;
            _targetY = y;
            _actionCost = actionCost;
        }

        public async Task Execute(IUnit attacker, IGridController controller)
        {
            if (attacker == null) return;

            var cellManager = controller.CellManager;
            var targetCell = cellManager.GetCellAt(new TurnBasedStrategyFramework.Common.Utilities.Vector2IntImpl(_targetX, _targetY));

            if (targetCell == null)
            {
                Debug.LogError($"CombatIceSpiritCommand: Target cell ({_targetX},{_targetY}) not found");
                return;
            }

            Debug.Log($"[Combat] {attacker.UnitID} cast Ice Spirit at ({_targetX},{_targetY}). Radius {Radius}.");

            // Find Tile Manager
            var tileManager = UnityEngine.Object.FindFirstObjectByType<CombatTileManager>();
            if (tileManager == null)
            {
                Debug.LogError("CombatIceSpiritCommand: CombatTileManager not found in scene");
            }

            // Find all cells in radius
            var affectedCells = cellManager.GetCells()
                .Where(c => c.GetDistance(targetCell) <= Radius)
                .ToList();

            // Apply Freezing to units in affected cells
            foreach (var cell in affectedCells)
            {
                // Spawn Ice Tile visual/logic
                if (tileManager != null)
                {
                    tileManager.AddEffect(cell, TileEffectType.Ice, 3);
                }
                
                foreach (var unit in cell.CurrentUnits)
                {
                    var unityUnit = unit as TurnBasedStrategyFramework.Unity.Units.Unit;
                    if (unityUnit == null) continue;

                    var identity = unityUnit.GetComponent<CombatIdentity>();
                    if (identity != null)
                    {
                        // Apply Freezing status
                        // Stacks: 1 (or per mechanics, accumulates % stacks. We'll start with 1 stack = 10% or similar representation)
                        // Duration: 3 turns (matches tile duration)
                        var freezingStatus = new StatusEffect(CombatStatusIds.Freezing, stacks: 1, duration: 3);
                        identity.AddStatus(freezingStatus);
                        Debug.Log($"[Combat] Applied Freezing to {unityUnit.name} at ({cell.GridCoordinates.x},{cell.GridCoordinates.y})");
                    }
                }
            }

            // Decrement AP
            attacker.ActionPoints -= _actionCost;

            // Trigger animations
            await controller.UnitManager.MarkAsAttacking(attacker, null); // No specific target unit
        }

        public Task Undo(IUnit attacker, IGridController controller)
        {
            Debug.LogWarning("CombatIceSpiritCommand.Undo not implemented");
            attacker.ActionPoints += _actionCost;
            return Task.CompletedTask;
        }

        private static class SerializationKeys
        {
            public const string TargetX = "target_x";
            public const string TargetY = "target_y";
            public const string ActionCost = "action_cost";
        }

        public Dictionary<string, object> Serialize()
        {
            return new Dictionary<string, object>
            {
                { SerializationKeys.TargetX, _targetX },
                { SerializationKeys.TargetY, _targetY },
                { SerializationKeys.ActionCost, _actionCost }
            };
        }

        public ICommand Deserialize(Dictionary<string, object> actionParams, IGridController gridController)
        {
            var x = (int)actionParams[SerializationKeys.TargetX];
            var y = (int)actionParams[SerializationKeys.TargetY];
            var actionCost = (int)actionParams[SerializationKeys.ActionCost];

            return new CombatIceSpiritCommand(x, y, actionCost);
        }
    }
}
