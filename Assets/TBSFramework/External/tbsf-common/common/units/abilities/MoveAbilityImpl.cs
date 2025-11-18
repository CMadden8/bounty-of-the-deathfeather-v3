using System;
using System.Collections.Generic;
using System.Linq;
using TurnBasedStrategyFramework.Common.Cells;
using TurnBasedStrategyFramework.Common.Controllers;
using TurnBasedStrategyFramework.Common.Controllers.GridStates;

namespace TurnBasedStrategyFramework.Common.Units.Abilities
{
    /// <summary>
    /// Implements a movement ability for a unit.
    /// Allows a unit to move to reachable cells within its movement range, visualizing the path and handling user interactions.
    /// Supports both mouse and touch controls.
    /// </summary>
    public class MoveAbilityImpl : IAbility
    {
        public event Action<IAbility> AbilitySelected;
        public event Action<IAbility> AbilityDeselected;

        /// <summary>
        /// Indicates if a move action requires user confirmation (double-tap). Useful for mobile games to prevent accidental moves.
        /// </summary>
        public bool WithConfirmation { get; set; }
        /// <summary>
        /// Enables an optimized control scheme for touch devices, improving usability on mobile platforms.
        /// Best used in combination with <see cref="WithConfirmation"/>.
        /// </summary>
        public bool UseTouchOptimizedControls { get; set; }
        private bool _isConfirmed;
        private ICell _confirmedTarget;

        /// <summary>
        /// A collection of cells within the movement range of the unit.
        /// </summary>
        private HashSet<ICell> _cellsInMovementRange;

        /// <summary>
        /// The current path to the selected cell.
        /// </summary>
        private IEnumerable<ICell> _currentPath;

        /// <summary>
        /// Gets or sets the reference to the unit that owns this ability.
        /// </summary>
        public IUnit UnitReference { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveAbilityImpl"/> class with the specified unit reference.
        /// </summary>
        /// <param name="unitReference">The unit that owns this ability.</param>
        public MoveAbilityImpl(IUnit unitReference, bool withConfirmation, bool useTouchControls)
        {
            UnitReference = unitReference;
            WithConfirmation = withConfirmation;
            UseTouchOptimizedControls = useTouchControls;
        }

        /// <summary>
        /// Called when the unit associated with this ability is selected.
        /// Initializes the movement range and path variables.
        /// </summary>
        /// <param name="gridController">The grid controller.</param>
        public void OnAbilitySelected(IGridController gridController)
        {
            UnitReference.CachePaths(gridController.CellManager);
            _cellsInMovementRange = UnitReference.ActionPoints > 0 ? new HashSet<ICell>(UnitReference.GetAvailableDestinations(gridController.CellManager.GetCells())) : new HashSet<ICell>();
            _currentPath = Enumerable.Empty<ICell>();
        }

        /// <summary>
        /// Displays the movement ability on the grid, highlighting all reachable cells.
        /// </summary>
        /// <param name="gridController">The grid controller.</param>
        public async void Display(IGridController gridController)
        {
            await gridController.CellManager.MarkAsReachable(_cellsInMovementRange);
        }

        /// <summary>
        /// Cleans up any visual indicators or temporary effects related to this ability, such as removing highlighted paths.
        /// </summary>
        /// <param name="gridController">The grid controller.</param>
        public void CleanUp(IGridController gridController)
        {
            var cellsToUnmark = _cellsInMovementRange ?? new HashSet<ICell>();
            if (_currentPath != null)
            {
                cellsToUnmark = cellsToUnmark.Union(_currentPath).ToHashSet();
            }
            gridController.CellManager.UnMark(cellsToUnmark);
            _isConfirmed = false;
            _confirmedTarget = null;
        }

        /// <summary>
        /// Called when this ability is deselected, resetting the movement range and path variables.
        /// </summary>
        /// <param name="gridController">The grid controller.</param>
        public void OnAbilityDeselected(IGridController gridController)
        {
            _cellsInMovementRange = new HashSet<ICell>();
            _currentPath = Enumerable.Empty<ICell>();
        }

        /// <summary>
        /// Called when a cell is clicked while this ability is active.
        /// If the clicked cell is within the movement range, the unit moves to that cell.
        /// Otherwise, the grid state transitions to awaiting input.
        /// </summary>
        /// <param name="cell">The cell that was clicked.</param>
        /// <param name="gridController">The grid controller.</param>
        public void OnCellClicked(ICell cell, IGridController gridController)
        {
            UnityEngine.Debug.Log($"MoveAbilityImpl.OnCellClicked: cell={(cell as UnityEngine.Object)?.name ?? "null"}, inRange={_cellsInMovementRange.Contains(cell)}");
            
            if (!_cellsInMovementRange.Contains(cell))
            {
                // Log diagnostics explaining why the clicked cell is unreachable for debugging.
                try
                {
                    LogWhyCellUnreachable(cell, gridController);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"MoveAbilityImpl: failed to run unreachable diagnostics: {ex}");
                }

                gridController.GridState = new GridStateAwaitInput();
                return;
            }

            if (UseTouchOptimizedControls)
            {
                gridController.CellManager.MarkAsReachable(_currentPath);
                _currentPath = UnitReference.FindPath(cell, gridController.CellManager);
                gridController.CellManager.MarkAsPath(_currentPath, UnitReference.CurrentCell);
            }

            if (WithConfirmation && (!_isConfirmed || !_confirmedTarget.Equals(cell)))
            {
                _isConfirmed = true;
                _confirmedTarget = cell;
                return;
            }

            if (!_currentPath.Any())
            {
                _currentPath = UnitReference.FindPath(cell, gridController.CellManager);
            }

            UnitReference.HumanExecuteAbility(new MoveCommand(UnitReference.CurrentCell, cell, _currentPath), gridController);
        }

        private void LogWhyCellUnreachable(ICell target, IGridController gridController)
        {
            var cellManager = gridController.CellManager;
            UnitReference.CachePaths(cellManager);

            // If FindPath returns any cells, check if it's affordable
            var path = UnitReference.FindPath(target, cellManager);
            float pathCost = 0f;
            bool pathExists = path != null && System.Linq.Enumerable.Any(path);
            
            if (pathExists)
            {
                var prev = UnitReference.CurrentCell;
                foreach (var step in path)
                {
                    pathCost += UnitReference.GetMovementCost(prev, step);
                    prev = step;
                }
            }

            // Log whether path exists and whether it's affordable
            if (pathExists)
            {
                bool affordable = pathCost <= UnitReference.MovementPoints;
                UnityEngine.Debug.Log($"MoveAbilityImpl: Path to '{(target as UnityEngine.Object)?.name ?? target.GetHashCode().ToString()}' EXISTS. Cost={pathCost}, MovementPoints={UnitReference.MovementPoints}, Affordable={affordable}");
                if (affordable)
                {
                    UnityEngine.Debug.LogWarning("MoveAbilityImpl: Cell is affordable but not in _cellsInMovementRange - possible caching issue!");
                    return;
                }
                UnityEngine.Debug.Log("MoveAbilityImpl: Cell is TOO FAR - path costs more than available movement points.");
            }
            else
            {
                UnityEngine.Debug.Log($"MoveAbilityImpl: NO PATH exists to target '{(target as UnityEngine.Object)?.name ?? target.GetHashCode().ToString()}' from unit '{(UnitReference as UnityEngine.Object)?.name ?? "unknown"}'. MovementPoints={UnitReference.MovementPoints}");
            }

            // Basic target info
            try { UnityEngine.Debug.Log($"Target.IsTaken={target.IsTaken}, MovementCost={target.MovementCost}"); } catch {}

            // Inspect neighbours to see why no edge exists to the target
            var neighbours = target.GetNeighbours(cellManager);
            int nCount = 0;
            int nInGraph = 0;
            foreach (var n in neighbours)
            {
                nCount++;
                string nName = (n as UnityEngine.Object)?.name ?? n.GetHashCode().ToString();
                string nCoords = $"({n.GridCoordinates.x}, {n.GridCoordinates.y})";
                bool traversableToTarget = false;
                try { traversableToTarget = UnitReference.IsCellTraversable(n, target); } catch {}
                bool neighbourTaken = false;
                try { neighbourTaken = n.IsTaken; } catch {}
                
                // Check if this neighbour is reachable from unit's current position
                bool neighbourReachable = false;
                if (_cellsInMovementRange != null && _cellsInMovementRange.Contains(n))
                {
                    neighbourReachable = true;
                    nInGraph++;
                }
                
                UnityEngine.Debug.Log($"Neighbour[{nCount}] '{nName}' at {nCoords}: IsTaken={neighbourTaken}, TraversableToTarget={traversableToTarget}, ReachableFromUnit={neighbourReachable}");
            }
            UnityEngine.Debug.Log($"MoveAbilityImpl: target has {nCount} neighbours, {nInGraph} are reachable from unit. TARGET COORDS: ({target.GridCoordinates.x}, {target.GridCoordinates.y})");

            // Check graph edges from unit's perspective
            var graph = UnitReference.GetGraphEdges(cellManager);
            UnityEngine.Debug.Log($"MoveAbilityImpl: graph contains {graph.Count} source nodes. Target present as source: {graph.ContainsKey(target)}");
        }

        /// <summary>
        /// Called when a cell is highlighted while this ability is active.
        /// Updates the current path to the selected cell and marks it on the grid.
        /// </summary>
        /// <param name="cell">The cell that was highlighted.</param>
        /// <param name="gridController">The grid controller.</param>
        public void OnCellHighlighted(ICell cell, IGridController gridController)
        {
            if (UseTouchOptimizedControls) return;
            if (_cellsInMovementRange.Contains(cell))
            {
                _currentPath = UnitReference.FindPath(cell, gridController.CellManager);
                gridController.CellManager.MarkAsPath(_currentPath, UnitReference.CurrentCell);
            }
        }

        /// <summary>
        /// Called when a cell is dehighlighted while this ability is active.
        /// Unmarks the current path and re-highlights reachable cells.
        /// </summary>
        /// <param name="cell">The cell that was dehighlighted.</param>
        /// <param name="gridController">The grid controller.</param>
        public void OnCellDehighlighted(ICell cell, IGridController gridController)
        {
            if (UseTouchOptimizedControls)
            {
                if (_currentPath.Contains(cell))
                {
                    gridController.CellManager.MarkAsPath(_currentPath, UnitReference.CurrentCell);
                }
                else if (_cellsInMovementRange.Contains(cell))
                {
                    gridController.CellManager.MarkAsReachable(cell);
                }
            }
            else if (_cellsInMovementRange.Contains(cell) && _currentPath != null)
            {
                gridController.CellManager.UnMark(_currentPath);
                gridController.CellManager.MarkAsReachable(_currentPath.Where(c => _cellsInMovementRange.Contains(c)));
            }
        }

        /// <summary>
        /// Called when a unit is clicked while this ability is active.
        /// If the clicked unit is a playable unit, transitions to the unit selected state.
        /// </summary>
        /// <param name="unit">The unit that was clicked.</param>
        /// <param name="gridController">The grid controller.</param>
        public void OnUnitClicked(IUnit unit, IGridController gridController)
        {
            if (gridController.TurnContext.PlayableUnits().Contains(unit))
            {
                gridController.GridState = new GridStateUnitSelected(unit, unit.GetBaseAbilities());
            }
        }

        /// <summary>
        /// Determines whether the unit can perform the movement ability.
        /// Returns true if the unit has action points and there are available cells to move to; otherwise, false.
        /// </summary>
        /// <param name="gridController">The grid controller.</param>
        /// <returns>True if movement can be performed; otherwise, false.</returns>
        public bool CanPerform(IGridController gridController)
        {
            return UnitReference.ActionPoints > 0 && UnitReference.GetAvailableDestinations(gridController.CellManager.GetCells()).Any();
        }

        public void Initialize(IGridController gridController)
        {
        }

        public void OnUnitHighlighted(IUnit unit, IGridController gridController)
        {
        }

        public void OnUnitDehighlighted(IUnit unit, IGridController gridController)
        {
        }

        public void OnUnitDestroyed(IGridController gridController)
        {
        }

        public void OnTurnStart(IGridController gridController)
        {
        }

        public void OnTurnEnd(IGridController gridController)
        {
        }
        public void InvokeAbilitySelected()
        {
            AbilitySelected.Invoke(this);
        }

        public void InvokeAbilityDeselected()
        {
            AbilityDeselected.Invoke(this);
        }
    }
}