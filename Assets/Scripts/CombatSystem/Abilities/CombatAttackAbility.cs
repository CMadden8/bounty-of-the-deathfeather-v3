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
    /// Combat-integrated attack ability that uses CombatIdentity and DamageResolver.
    /// Extends TBSF AttackAbility to integrate with our combat system.
    /// </summary>
    public class CombatAttackAbility : Ability
    {
        private HashSet<IUnit> _attackableUnits;

        public override void Initialize(IGridController gridController)
        {
            base.Initialize(gridController);
        }

        public override void OnAbilitySelected(IGridController gridController)
        {
            var enemyUnits = gridController.UnitManager.GetEnemyUnits(gridController.TurnContext.CurrentPlayer);
            
            // Filter for units within attack range using CombatIdentity.AttackRange
            _attackableUnits = new HashSet<IUnit>();

            var attackerUnit = UnitReference as TurnBasedStrategyFramework.Unity.Units.Unit;
            if (attackerUnit == null) return;

            var attackerIdentity = attackerUnit.GetComponent<CombatIdentity>();
            if (attackerIdentity == null)
            {
                Debug.LogWarning($"[CombatAttackAbility] {attackerUnit.name} missing CombatIdentity");
                return;
            }

            int attackRange = attackerIdentity.AttackRange;

            foreach (var enemy in enemyUnits)
            {
                if (enemy.CurrentCell == null || UnitReference.CurrentCell == null) continue;

                int distance = enemy.CurrentCell.GetDistance(UnitReference.CurrentCell);
                if (distance <= attackRange)
                {
                    _attackableUnits.Add(enemy);
                }
            }
        }

        public override async void Display(IGridController gridController)
        {
            if (_attackableUnits == null) return;
            await gridController.UnitManager.MarkAsTargetable(_attackableUnits);
        }

        public override void CleanUp(IGridController gridController)
        {
            if (_attackableUnits == null) return;
            gridController.UnitManager.UnMark(_attackableUnits);
        }

        public override void OnUnitClicked(IUnit unit, IGridController gridController)
        {
            if (_attackableUnits == null) return;

            if (UnitReference.ActionPoints > 0 && _attackableUnits.Contains(unit))
            {
                // Execute combat attack using CombatAttackCommand
                UnitReference.HumanExecuteAbility(new CombatAttackCommand(unit), gridController);
            }
            else if (gridController.UnitManager.GetFriendlyUnits(UnitReference.PlayerNumber).Contains(unit))
            {
                // Clicked friendly unit - switch to that unit
                gridController.GridState = new GridStateUnitSelected(unit, unit.GetBaseAbilities());
            }
        }

        public override void OnCellClicked(ICell cell, IGridController gridController)
        {
            // Clicked empty cell - deselect ability
            gridController.GridState = new GridStateAwaitInput();
        }

        public override bool CanPerform(IGridController gridController)
        {
            if (UnitReference.ActionPoints <= 0)
            {
                return false;
            }

            // Check if there are any enemy units within attack range
            var attackerUnit = UnitReference as TurnBasedStrategyFramework.Unity.Units.Unit;
            if (attackerUnit == null) return false;

            var attackerIdentity = attackerUnit.GetComponent<CombatIdentity>();
            if (attackerIdentity == null) return false;

            int attackRange = attackerIdentity.AttackRange;

            var enemyUnits = gridController.UnitManager.GetEnemyUnits(
                gridController.PlayerManager.GetPlayerByNumber(UnitReference.PlayerNumber));

            return enemyUnits.Any(enemy =>
                enemy.CurrentCell != null &&
                UnitReference.CurrentCell != null &&
                enemy.CurrentCell.GetDistance(UnitReference.CurrentCell) <= attackRange);
        }

        public override void OnUnitHighlighted(IUnit unit, IGridController gridController)
        {
            // Optional: Show predicted damage when hovering over enemy
        }

        public override void OnUnitDehighlighted(IUnit unit, IGridController gridController)
        {
            // Optional: Hide predicted damage
        }

        public override void OnUnitDestroyed(IGridController gridController)
        {
            // Cleanup if unit is destroyed
        }

        public override void OnTurnStart(IGridController gridController)
        {
            // Reset ability state at turn start if needed
        }

        public override void OnTurnEnd(IGridController gridController)
        {
            // Cleanup at turn end if needed
        }
    }
}
