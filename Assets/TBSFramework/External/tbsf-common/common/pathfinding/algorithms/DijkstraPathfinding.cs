using System.Collections.Generic;
using TurnBasedStrategyFramework.Common.Cells;
using TurnBasedStrategyFramework.Common.Pathfinding.DataStructures;

namespace TurnBasedStrategyFramework.Common.Pathfinding.Algorithms
{
    /// <summary>
    /// Implementation of Dijkstra pathfinding algorithm.
    /// </summary>
    class DijkstraPathfinding : PathfindingAlgorithm
    {
        public override LinkedList<ICell> FindPath(Dictionary<ICell, Dictionary<ICell, float>> edges, ICell originNode, ICell destinationNode)
        {
            IPriorityQueue<ICell> frontier = new SortedListPriorityQueue<ICell>(edges.Count);
            frontier.Enqueue(originNode, 0);

            Dictionary<ICell, ICell> cameFrom = new Dictionary<ICell, ICell>(edges.Count);
            cameFrom.Add(originNode, default);
            Dictionary<ICell, float> costSoFar = new Dictionary<ICell, float>(edges.Count);
            costSoFar.Add(originNode, 0);

            while (frontier.Count != 0)
            {
                var current = frontier.Dequeue();
                var neighbours = GetNeighbours(edges, current);
                var currentCost = costSoFar[current];
                var currentEdges = edges[current];

                foreach (var neighbour in neighbours)
                {
                    var newCost = currentCost + currentEdges[neighbour];
                    if (!costSoFar.TryGetValue(neighbour, out var neighbourCost) || newCost < neighbourCost)
                    {
                        costSoFar[neighbour] = newCost;
                        cameFrom[neighbour] = current;
                        frontier.Enqueue(neighbour, newCost);
                    }
                }
                if (current.Equals(destinationNode)) break;
            }
            LinkedList<ICell> path = new LinkedList<ICell>();
            if (!cameFrom.ContainsKey(destinationNode))
            {
                return path;
            }

            path.AddFirst(destinationNode);
            var temp = destinationNode;

            while (!cameFrom[temp].Equals(originNode))
            {
                var currentPathElement = cameFrom[temp];
                path.AddFirst(currentPathElement);

                temp = currentPathElement;
            }

            return path;
        }

        public override Dictionary<ICell, LinkedList<ICell>> FindAllPaths(Dictionary<ICell, Dictionary<ICell, float>> edges, ICell originNode)
        {
            IPriorityQueue<ICell> frontier = new HeapPriorityQueue<ICell>(edges.Count);
            frontier.Enqueue(originNode, 0);

            Dictionary<ICell, ICell> cameFrom = new Dictionary<ICell, ICell>(edges.Count);
            cameFrom.Add(originNode, default);
            Dictionary<ICell, float> costSoFar = new Dictionary<ICell, float>(edges.Count);
            costSoFar.Add(originNode, 0);

            while (frontier.Count != 0)
            {
                var current = frontier.Dequeue();
                var neighbours = GetNeighbours(edges, current);
                var currentCost = costSoFar[current];
                var currentEdges = edges[current];

                foreach (var neighbour in neighbours)
                {
                    var newCost = currentCost + currentEdges[neighbour];
                    if (!costSoFar.TryGetValue(neighbour, out var neighbourCost) || newCost < neighbourCost)
                    {
                        costSoFar[neighbour] = newCost;
                        cameFrom[neighbour] = current;
                        frontier.Enqueue(neighbour, newCost);
                    }
                }
            }

            Dictionary<ICell, LinkedList<ICell>> paths = new Dictionary<ICell, LinkedList<ICell>>();
            foreach (ICell destination in cameFrom.Keys)
            {
                LinkedList<ICell> path = new LinkedList<ICell>();
                var current = destination;
                while (current != null && !current.Equals(originNode))
                {
                    path.AddFirst(current);
                    current = cameFrom[current];
                }
                paths.Add(destination, path);
            }
            return paths;
        }
    }
}