using System.Collections.Generic;
using System.Linq;
using TurnBasedStrategyFramework.Common.Cells;
using TurnBasedStrategyFramework.Common.Controllers;
using TurnBasedStrategyFramework.Common.Controllers.GridStates;
using TurnBasedStrategyFramework.Common.Units;
using TurnBasedStrategyFramework.Unity.Units.Abilities;
using TurnBasedStrategyFramework.Unity.Units;
using UnityEngine;
using BountyOfTheDeathfeather.CombatSystem.Commands;

namespace BountyOfTheDeathfeather.CombatSystem.Abilities
{
    /// <summary>
    /// Fire Spirit Ability (Mirashala).
    /// Applies Burning status to a single enemy target.
    /// </summary>
    public class CombatFireSpiritAbility : Ability
    {
        private HashSet<IUnit> _targetableUnits;
        private IEnumerable<IUnit> _unitsInAoe;
        private IEnumerable<TurnBasedStrategyFramework.Common.Cells.ICell> _cellsInAoe;
        private const int AbilityRange = 4; // Default range for Fire Spirit
        private const int ActionCost = 1;

        public override void OnAbilitySelected(IGridController gridController)
        {
            var enemyUnits = gridController.UnitManager.GetEnemyUnits(gridController.TurnContext.CurrentPlayer);
            
            _targetableUnits = new HashSet<IUnit>();

            foreach (var enemy in enemyUnits)
            {
                if (enemy.CurrentCell == null || UnitReference.CurrentCell == null) continue;

                int distance = enemy.CurrentCell.GetDistance(UnitReference.CurrentCell);
                if (distance <= AbilityRange)
                {
                    _targetableUnits.Add(enemy);
                }
            }
        }

        public override async void Display(IGridController gridController)
        {
            if (_targetableUnits == null) return;
            await gridController.UnitManager.MarkAsTargetable(_targetableUnits);
        }

        public override void CleanUp(IGridController gridController)
        {
            if (_targetableUnits == null) return;
            gridController.UnitManager.UnMark(_targetableUnits);
            if (_cellsInAoe != null)
            {
                gridController.CellManager.UnMark(_cellsInAoe);
                _cellsInAoe = null;
            }
            if (_unitsInAoe != null)
            {
                gridController.UnitManager.UnMark(_unitsInAoe);
                _unitsInAoe = null;
            }
        }

        public override void OnUnitClicked(IUnit unit, IGridController gridController)
        {
            if (_targetableUnits == null) return;

            if (UnitReference.ActionPoints >= ActionCost && _targetableUnits.Contains(unit))
            {
                // Execute Fire Spirit command
                UnitReference.HumanExecuteAbility(new CombatFireSpiritCommand(unit, ActionCost), gridController);
            }
            else if (gridController.UnitManager.GetFriendlyUnits(UnitReference.PlayerNumber).Contains(unit))
            {
                gridController.GridState = new GridStateUnitSelected(unit, unit.GetBaseAbilities());
            }
        }

        public override void OnUnitHighlighted(IUnit unit, IGridController gridController)
        {
            // When hovering a targetable unit, show the ability AOE (based on attacker's Explosion level)
            if (_targetableUnits == null || !_targetableUnits.Contains(unit)) return;

            int explosionLevel = 0;
            var identityComp = (UnitReference as Unit)?.GetComponent<BountyOfTheDeathfeather.CombatSystem.CombatIdentity>();
            if (identityComp != null)
            {
                explosionLevel = identityComp.FireSpiritExplosionTPInvested;
            }

            if (explosionLevel <= 0)
            {
                // single target - just mark the hovered unit as targetable
                gridController.UnitManager.MarkAsTargetable(new[] { unit });
                _unitsInAoe = new[] { unit };
                return;
            }

            // compute cells within explosion radius around the hovered unit's cell
            var targetCell = unit.CurrentCell;
            if (targetCell == null) return;

            _cellsInAoe = gridController.CellManager.GetCells()
                .Where(c => c.GetDistance(targetCell) <= explosionLevel)
                .ToList();

            gridController.CellManager.MarkAsReachable(_cellsInAoe);

            _unitsInAoe = _cellsInAoe.SelectMany(c => c.CurrentUnits).ToList();
            gridController.UnitManager.MarkAsTargetable(_unitsInAoe);
        }

        public override void OnUnitDehighlighted(IUnit unit, IGridController gridController)
        {
            if (_cellsInAoe != null)
            {
                gridController.CellManager.UnMark(_cellsInAoe);
                _cellsInAoe = null;
            }
            if (_unitsInAoe != null)
            {
                gridController.UnitManager.UnMark(_unitsInAoe);
                _unitsInAoe = null;
            }
        }

        public override void OnCellHighlighted(ICell cell, IGridController gridController)
        {
            // Show AOE preview when hovering empty cells (based on attacker's Explosion level)
            if (_targetableUnits == null) return;

            int explosionLevel = 0;
            var identityComp = (UnitReference as Unit)?.GetComponent<BountyOfTheDeathfeather.CombatSystem.CombatIdentity>();
            if (identityComp != null)
            {
                explosionLevel = identityComp.FireSpiritExplosionTPInvested;
            }

            if (explosionLevel <= 0)
            {
                // No AOE to show for single-target; do nothing
                return;
            }

            var targetCell = cell;
            if (targetCell == null) return;

            _cellsInAoe = gridController.CellManager.GetCells()
                .Where(c => c.GetDistance(targetCell) <= explosionLevel)
                .ToList();

            gridController.CellManager.MarkAsReachable(_cellsInAoe);

            _unitsInAoe = _cellsInAoe.SelectMany(c => c.CurrentUnits).ToList();
            gridController.UnitManager.MarkAsTargetable(_unitsInAoe);
        }

        public override void OnCellDehighlighted(ICell cell, IGridController gridController)
        {
            if (_cellsInAoe != null)
            {
                gridController.CellManager.UnMark(_cellsInAoe);
                _cellsInAoe = null;
            }
            if (_unitsInAoe != null)
            {
                gridController.UnitManager.UnMark(_unitsInAoe);
                _unitsInAoe = null;
            }
        }

        public override void OnCellClicked(ICell cell, IGridController gridController)
        {
            gridController.GridState = new GridStateAwaitInput();
        }

        public override bool CanPerform(IGridController gridController)
        {
            // Per COMBAT_MECHANICS.md: abilities consume AP but don't end turn if MP remains
            // Return true if unit has MP > 0 to prevent auto-ending turn when AP exhausted
            
            if (UnitReference.ActionPoints < ActionCost)
            {
                // No AP for ability, but if MP > 0, unit can still move (return true to prevent auto-finish)
                return UnitReference.MovementPoints > 0;
            }

            var enemyUnits = gridController.UnitManager.GetEnemyUnits(
                gridController.PlayerManager.GetPlayerByNumber(UnitReference.PlayerNumber));

            bool hasEnemiesInRange = enemyUnits.Any(enemy =>
                enemy.CurrentCell != null &&
                UnitReference.CurrentCell != null &&
                enemy.CurrentCell.GetDistance(UnitReference.CurrentCell) <= AbilityRange);
            
            // Return true if enemies in range OR if MP > 0 (allow movement)
            return hasEnemiesInRange || UnitReference.MovementPoints > 0;
        }
    }
}
