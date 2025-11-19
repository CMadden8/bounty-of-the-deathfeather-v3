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
    /// Ice Spirit Ability (Mirashala).
    /// Creates an ice tile area (radius 3) and applies Freezing status.
    /// Targets a cell.
    /// </summary>
    public class CombatIceSpiritAbility : Ability
    {
        private const int AbilityRange = 4; // Casting range
        private const int EffectRadius = 3; // AOE radius
        private const int ActionCost = 1;
        
        private HashSet<ICell> _validTargetCells;

        public override void OnAbilitySelected(IGridController gridController)
        {
            _validTargetCells = new HashSet<ICell>();
            var currentCell = UnitReference.CurrentCell;
            if (currentCell == null) return;

            // Find all cells within casting range
            foreach (var cell in gridController.CellManager.GetCells())
            {
                if (cell.GetDistance(currentCell) <= AbilityRange)
                {
                    _validTargetCells.Add(cell);
                }
            }
        }

        public override async void Display(IGridController gridController)
        {
            if (_validTargetCells == null) return;
            // Mark valid casting targets
            await gridController.CellManager.MarkAsReachable(_validTargetCells);
        }

        public override void CleanUp(IGridController gridController)
        {
            if (_validTargetCells == null) return;
            gridController.CellManager.UnMark(_validTargetCells);
        }

        public override void OnCellClicked(ICell cell, IGridController gridController)
        {
            if (_validTargetCells != null && _validTargetCells.Contains(cell))
            {
                if (UnitReference.ActionPoints >= ActionCost)
                {
                    UnitReference.HumanExecuteAbility(new CombatIceSpiritCommand(cell, ActionCost), gridController);
                }
            }
            else
            {
                // Deselect if clicked outside
                gridController.GridState = new GridStateAwaitInput();
            }
        }

        public override void OnUnitClicked(IUnit unit, IGridController gridController)
        {
            // If unit is on a valid cell, treat as cell click
            if (unit.CurrentCell != null && _validTargetCells != null && _validTargetCells.Contains(unit.CurrentCell))
            {
                OnCellClicked(unit.CurrentCell, gridController);
            }
            else if (gridController.UnitManager.GetFriendlyUnits(UnitReference.PlayerNumber).Contains(unit))
            {
                gridController.GridState = new GridStateUnitSelected(unit, unit.GetBaseAbilities());
            }
        }

        public override void OnCellHighlighted(ICell cell, IGridController gridController)
        {
            // Optional: Show AOE preview
            // This requires unmarking previous preview and marking new one, which might be complex with async Mark methods.
            // For now, we just rely on the casting range highlight.
        }

        public override void OnCellDehighlighted(ICell cell, IGridController gridController)
        {
            // Optional: Hide AOE preview
        }

        public override bool CanPerform(IGridController gridController)
        {
            return UnitReference.ActionPoints >= ActionCost;
        }
    }
}
