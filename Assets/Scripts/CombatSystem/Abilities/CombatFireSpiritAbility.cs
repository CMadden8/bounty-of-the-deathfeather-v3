using System.Collections.Generic;
using System.Linq;
using TurnBasedStrategyFramework.Common.Cells;
using TurnBasedStrategyFramework.Common.Controllers;
using TurnBasedStrategyFramework.Common.Controllers.GridStates;
using TurnBasedStrategyFramework.Common.Units;
using TurnBasedStrategyFramework.Unity.Units.Abilities;
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

        public override void OnCellClicked(ICell cell, IGridController gridController)
        {
            gridController.GridState = new GridStateAwaitInput();
        }

        public override bool CanPerform(IGridController gridController)
        {
            if (UnitReference.ActionPoints < ActionCost)
            {
                return false;
            }

            var enemyUnits = gridController.UnitManager.GetEnemyUnits(
                gridController.PlayerManager.GetPlayerByNumber(UnitReference.PlayerNumber));

            return enemyUnits.Any(enemy =>
                enemy.CurrentCell != null &&
                UnitReference.CurrentCell != null &&
                enemy.CurrentCell.GetDistance(UnitReference.CurrentCell) <= AbilityRange);
        }
    }
}
