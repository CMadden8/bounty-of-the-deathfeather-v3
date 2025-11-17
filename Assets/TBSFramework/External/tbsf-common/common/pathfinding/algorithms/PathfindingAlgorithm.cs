using System.Collections.Generic;
using System.Linq;
using TurnBasedStrategyFramework.Common.Cells;

namespace TurnBasedStrategyFramework.Common.Pathfinding.Algorithms
{
    /// <summary>
    /// Represents the base structure for pathfinding algorithms, defining core methods to compute paths in a graph.
    /// </summary>
    public abstract class PathfindingAlgorithm
    {
        /// <summary>
        /// Finds a path between the origin and destination nodes in the graph.
        /// </summary>
        /// <param name="edges">
        /// The graph representation, where each key is a node, and the value is a dictionary of neighboring nodes with their respective edge weights.
        /// </param>
        /// <param name="originNode">The starting node of the pathfinding process.</param>
        /// <param name="destinationNode">The target node that the pathfinding process aims to reach.</param>
        /// <returns>
        /// A linked list of cells representing the computed path from the origin to the destination. If no path exists, returns an empty linked list.
        /// </returns>
        public abstract LinkedList<ICell> FindPath(Dictionary<ICell, Dictionary<ICell, float>> edges, ICell originNode, ICell destinationNode);

        /// <summary>
        /// Finds all possible paths from the origin node to all reachable nodes in the graph.
        /// </summary>
        /// <param name="edges">
        /// The graph representation, where each key is a node, and the value is a dictionary of neighboring nodes with their respective edge weights.
        /// </param>
        /// <param name="originNode">The starting node for finding all possible paths.</param>
        /// <returns>
        /// A dictionary where each key is a destination node and the value is a linked list representing the path from the origin to that node.
        /// </returns>
        public abstract Dictionary<ICell, LinkedList<ICell>> FindAllPaths(Dictionary<ICell, Dictionary<ICell, float>> edges, ICell originNode);

        /// <summary>
        /// Retrieves the neighboring nodes for the specified node from the graph's edge structure.
        /// </summary>
        /// <param name="edges">The graph representation, where each key is a node, and the value is a dictionary of neighboring nodes.</param>
        /// <param name="node">The node whose neighbors are to be retrieved.</param>
        /// <returns>
        /// An enumerable of neighboring cells. If the node has no neighbors, returns an empty enumerable.
        /// </returns>
        protected IEnumerable<ICell> GetNeighbours(Dictionary<ICell, Dictionary<ICell, float>> edges, ICell node)
        {
            if (edges.TryGetValue(node, out var neighbours))
            {
                return neighbours.Keys;
            }
            return Enumerable.Empty<ICell>();
        }
    }
}
