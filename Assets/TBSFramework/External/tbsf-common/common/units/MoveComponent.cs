using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TurnBasedStrategyFramework.Common.Cells;
using TurnBasedStrategyFramework.Common.Pathfinding.Algorithms;

namespace TurnBasedStrategyFramework.Common.Units
{
    /// <summary>
    /// Represents the movement component of a unit, providing pathfinding, movement validation, and caching mechanisms.
    /// </summary>
    public abstract class MoveComponent
    {
        /// <summary>
        /// Reference to the unit that owns this movement component.
        /// </summary>
        protected readonly IUnit _unitReference;

        /// <summary>
        /// Caches the paths from the current cell to other cells for movement efficiency.
        /// </summary>
        private Dictionary<ICell, LinkedList<ICell>> _pathCache;

        /// <summary>
        /// Instance of the DijkstraPathfinding algorithm used for calculating paths.
        /// </summary>
        private static DijkstraPathfinding pathfinder = new DijkstraPathfinding();

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveComponent"/> class with a reference to the owning unit.
        /// </summary>
        /// <param name="unitReference">The unit that owns this movement component.</param>
        public MoveComponent(IUnit unitReference)
        {
            _unitReference = unitReference;
        }

        /// <summary>
        /// Indicates if the unit is capable of moving to the cell given as parameter.
        /// </summary>
        /// <param name="cell">The cell to check for movement eligibility.</param>
        /// <returns>True if the unit can move to the cell, otherwise false.</returns>
        public bool IsCellMovableTo(ICell cell)
        {
            return !cell.IsTaken;
        }

        /// <summary>
        /// Determines if the unit can traverse from one cell to an adjacent cell.
        /// </summary>
        /// <param name="source">The current cell the unit is in.</param>
        /// <param name="destination">The neighboring cell the unit wants to move into.</param>
        /// <returns>True if the unit can traverse from source to destination; otherwise, false.</returns>
        public bool IsCellTraversable(ICell source, ICell destination)
        {
            return !destination.IsTaken;

        }

        /// <summary>
        /// Calculates the movement cost required for the unit to move from the source cell to the destination cell.
        /// </summary>
        /// <param name="source">The starting cell.</param>
        /// <param name="destination">The cell the unit wants to move to.</param>
        /// <returns>The numeric movement cost for moving between the two cells.</returns>
        public float GetMovementCost(ICell source, ICell destination)
        {
            return destination.MovementCost;
        }

        /// <summary>
        /// Gets the available destination cells that the unit can move to based on its current movement points.
        /// </summary>
        /// <param name="cells">A collection of cells to check for potential movement.</param>
        /// <returns>An enumerable collection of cells that the unit can reach.</returns>
        public IEnumerable<ICell> GetAvailableDestinations(IEnumerable<ICell> cells)
        {
            Debug.Assert(_pathCache != null, "Path cache is null, call CachePaths first");

            foreach (var cell in cells)
            {
                if (!_unitReference.IsCellMovableTo(cell))
                {
                    continue;
                }

                if (_pathCache.TryGetValue(cell, out var path))
                {
                    float pathCost = 0f;
                    ICell previous = _unitReference.CurrentCell;

                    foreach (var current in path)
                    {
                        pathCost += _unitReference.GetMovementCost(previous, current);
                        previous = current;
                    }

                    if (pathCost <= _unitReference.MovementPoints)
                    {
                        yield return cell;
                    }
                }
            }
        }

        /// <summary>
        /// Constructs a graph representation of cells and their movement costs for pathfinding purposes.
        /// </summary>
        /// <param name="cellManager">The cell manager that provides access to all cells.</param>
        /// <returns>A dictionary representing the graph edges, with cells as keys and movement costs as values.</returns>
        public Dictionary<ICell, Dictionary<ICell, float>> GetGraphEdges(ICellManager cellManager)
        {
            var graph = new Dictionary<ICell, Dictionary<ICell, float>>();

            foreach (var source in cellManager.GetCells())
            {
                var edges = new Dictionary<ICell, float>();
                foreach (var destination in source.GetNeighbours(cellManager))
                {
                    if (_unitReference.IsCellTraversable(source, destination) || destination.Equals(_unitReference.CurrentCell))
                    {
                        edges[destination] = _unitReference.GetMovementCost(source, destination);
                    }
                }

                if (edges.Count > 0 || source == _unitReference.CurrentCell)
                {
                    graph[source] = edges;
                }
            }
            return graph;
        }

        /// <summary>
        /// Finds the path from the unit's current position to the specified destination cell.
        /// </summary>
        /// <param name="destination">The destination cell.</param>
        /// <param name="cellManager">The cell manager that provides access to all cells.</param>
        /// <returns>An enumerable collection of cells representing the path to the destination, or an empty collection if no path is found.</returns>
        public IEnumerable<ICell> FindPath(ICell destination, ICellManager cellManager)
        {
            Debug.Assert(_pathCache != null, "Path cache is null, call CachePaths first");
            if (_pathCache.TryGetValue(destination, out var path))
            {
                return path;
            }
            return Enumerable.Empty<ICell>();
        }

        /// <summary>
        /// Caches all paths from the unit's current cell to other cells for efficient movement calculations.
        /// </summary>
        /// <param name="cellManager">The cell manager that provides access to all cells.</param>
        public void CachePaths(ICellManager cellManager)
        {
            if (_unitReference.CurrentCell == null)
            {
                UnityEngine.Debug.LogWarning($"Cannot cache paths for unit {(_unitReference as UnityEngine.Object)?.name ?? "unknown"}: CurrentCell is null. Make sure unit is positioned on a cell.");
                _pathCache = new Dictionary<ICell, LinkedList<ICell>>();
                return;
            }
            _pathCache = pathfinder.FindAllPaths(_unitReference.GetGraphEdges(cellManager), _unitReference.CurrentCell);
        }

        /// <summary>
        /// Diagnostic helper: logs why the specified target cell is not reachable from the unit's current cell.
        /// Useful for debugging disconnected islands, blocked neighbours, or movement cost issues.
        /// </summary>
        public void DebugWhyUnreachable(ICell target, ICellManager cellManager)
        {
            if (target == null)
            {
                UnityEngine.Debug.LogError("DebugWhyUnreachable: target is null");
                return;
            }

            if (_unitReference.CurrentCell == null)
            {
                UnityEngine.Debug.LogWarning($"DebugWhyUnreachable: unit {(_unitReference as UnityEngine.Object)?.name ?? "unknown"} has no CurrentCell set.");
                return;
            }

            // Ensure cache exists
            if (_pathCache == null)
            {
                CachePaths(cellManager);
            }

            // If a path exists in cache, it's reachable
            if (_pathCache != null && _pathCache.TryGetValue(target, out var path))
            {
                float cost = 0f;
                ICell prev = _unitReference.CurrentCell;
                foreach (var step in path)
                {
                    cost += _unitReference.GetMovementCost(prev, step);
                    prev = step;
                }
                UnityEngine.Debug.Log($"DebugWhyUnreachable: target '{(target as UnityEngine.Object)?.name ?? target.GetHashCode().ToString()}' is reachable. Path cost={cost}, MovementPoints={_unitReference.MovementPoints}");
                return;
            }

            // Not found — output diagnostic info
            UnityEngine.Debug.Log($"DebugWhyUnreachable: No cached path to target '{(target as UnityEngine.Object)?.name ?? target.GetHashCode().ToString()}' from unit '{(_unitReference as UnityEngine.Object)?.name ?? "unknown"}'. MovementPoints={_unitReference.MovementPoints}");

            // If the target itself is marked taken, that's likely the reason
            try
            {
                UnityEngine.Debug.Log($"Target.IsTaken={target.IsTaken}, MovementCost={target.MovementCost}");
            }
            catch
            {
                // swallow - some ICell implementations may throw for debug
            }

            // Inspect neighbours of the target to see if any edges lead to it
            var neighbours = target.GetNeighbours(cellManager).ToList();
            UnityEngine.Debug.Log($"Target has {neighbours.Count} neighbours.");
            foreach (var n in neighbours)
            {
                bool neighbourInGraph = false;
                bool edgeExists = false;
                bool traversableFromNeighbour = false;
                string neighbourName = (n as UnityEngine.Object)?.name ?? n.GetHashCode().ToString();

                // Is this neighbour present as a source in the graph?
                if (_pathCache != null && _pathCache.ContainsKey(n)) neighbourInGraph = true;

                // Check whether the unit would consider the neighbour traversable -> target
                try
                {
                    traversableFromNeighbour = _unitReference.IsCellTraversable(n, target);
                }
                catch { }

                // Check if neighbour has an outgoing edge to target per GetGraphEdges logic
                var edges = _unitReference.GetGraphEdges(cellManager);
                if (edges.TryGetValue(n, out var dict))
                {
                    edgeExists = dict.ContainsKey(target);
                }

                UnityEngine.Debug.Log($"Neighbour '{neighbourName}': InGraphSource={neighbourInGraph}, EdgeToTarget={edgeExists}, TraversableToTarget={traversableFromNeighbour}, IsTaken={n.IsTaken}");
            }

            // Additionally, log whether any cells in the world contain edges at all (sanity)
            var graph = _unitReference.GetGraphEdges(cellManager);
            UnityEngine.Debug.Log($"Graph contains {graph.Count} source nodes. Target present as source: {graph.ContainsKey(target)}");
        }

        /// <summary>
        /// Invalidates the cached paths, typically when the game state changes in a way that affects movement.
        /// </summary>
        public void InvalidateCache()
        {
            _pathCache = null;
        }

        /// <summary>
        /// Performs the movement animation for the unit along a specified path to the destination cell.
        /// This method must be implemented by derived classes to provide specific animation logic.
        /// </summary>
        /// <param name="path">The path the unit will follow.</param>
        /// <param name="destination">The destination cell.</param>
        /// <returns>A task representing the asynchronous animation of the movement.</returns>
        public abstract Task MovementAnimation(IEnumerable<ICell> path, ICell destination);
    }
}