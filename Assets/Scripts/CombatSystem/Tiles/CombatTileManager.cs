using System.Collections.Generic;
using System.Linq;
using TurnBasedStrategyFramework.Common.Cells;
using TurnBasedStrategyFramework.Common.Controllers;
using TurnBasedStrategyFramework.Unity.Controllers;
using TurnBasedStrategyFramework.Unity.Cells;
using TurnBasedStrategyFramework.Common.Utilities;
using UnityEngine;
using BountyOfTheDeathfeather.CombatSystem.Managers;

namespace BountyOfTheDeathfeather.CombatSystem.Tiles
{
    /// <summary>
    /// Manages tile effects (Flame, Ice, Shadow) on the grid.
    /// Handles duration, spreading, interactions, and applying statuses to units.
    /// </summary>
    public class CombatTileManager : MonoBehaviour
    {
        [SerializeField] private UnityGridController _gridController;
        
        // Map cell coordinates to active effect
        private Dictionary<Vector2Int, TileEffect> _activeEffects = new Dictionary<Vector2Int, TileEffect>();
        
        // Visuals (simple color tinting for MVP)
        private Dictionary<Vector2Int, Color> _originalColors = new Dictionary<Vector2Int, Color>();

        private void Awake()
        {
            if (_gridController == null)
            {
                _gridController = FindFirstObjectByType<UnityGridController>();
            }
        }

        private void OnEnable()
        {
            if (_gridController != null)
            {
                _gridController.TurnStarted += OnTurnStarted;
            }
        }

        private void OnDisable()
        {
            if (_gridController != null)
            {
                _gridController.TurnStarted -= OnTurnStarted;
            }
        }

        /// <summary>
        /// Adds or updates a tile effect on a specific cell.
        /// Handles interactions (Ice vs Flame).
        /// </summary>
        public void AddEffect(ICell cell, TileEffectType type, int duration)
        {
            var coords = new Vector2Int(cell.GridCoordinates.x, cell.GridCoordinates.y);
            
            if (_activeEffects.TryGetValue(coords, out var existing))
            {
                // Interaction Logic
                if (existing.Type == TileEffectType.Flame && type == TileEffectType.Ice)
                {
                    RemoveEffect(cell); // Extinguish
                    Debug.Log($"[CombatTileManager] Ice extinguished Flame at {coords}");
                    return;
                }
                if (existing.Type == TileEffectType.Ice && type == TileEffectType.Flame)
                {
                    RemoveEffect(cell); // Melt
                    Debug.Log($"[CombatTileManager] Flame melted Ice at {coords}");
                    return;
                }
            }

            // Apply new effect
            _activeEffects[coords] = new TileEffect(type, duration);
            UpdateVisuals(cell, type);
            Debug.Log($"[CombatTileManager] Applied {type} to {coords} for {duration} turns");
        }

        public void RemoveEffect(ICell cell)
        {
            var coords = new Vector2Int(cell.GridCoordinates.x, cell.GridCoordinates.y);
            if (_activeEffects.ContainsKey(coords))
            {
                _activeEffects.Remove(coords);
                RestoreVisuals(cell);
            }
        }

        private void OnTurnStarted(TurnTransitionParams turnParams)
        {
            // Only process tile logic once per full round (e.g. start of Player 0 turn)
            // OR process at start of every turn?
            // Mechanics: "At the start of each turn, a flame tile will spawn additional flame tiles..."
            // "Flames apply Burning to units occupying the Flame tile at the start of their turn."
            
            // 1. Apply effects to units (happens on unit's turn start)
            ApplyTileEffectsToUnits(turnParams.TurnContext.CurrentPlayer.PlayerNumber);

            // 2. Process tile duration/spread (happens globally or per turn?)
            // "Each Flame tile: duration default is 3 turns... At the start of each turn, a flame tile will spawn..."
            // This implies tile logic runs every turn.
            ProcessTileLogic();
        }

        private void ApplyTileEffectsToUnits(int currentPlayerNumber)
        {
            foreach (var kvp in _activeEffects)
            {
                var coords = kvp.Key;
                var effect = kvp.Value;
                var cell = _gridController.CellManager.GetCellAt(new Vector2IntImpl(coords.x, coords.y));
                
                if (cell == null || cell.CurrentUnits.Count == 0) continue;

                foreach (var unit in cell.CurrentUnits)
                {
                    if (unit.PlayerNumber != currentPlayerNumber) continue;

                    var unityUnit = unit as TurnBasedStrategyFramework.Unity.Units.Unit;
                    if (unityUnit == null) continue;
                    var identity = unityUnit.GetComponent<CombatIdentity>();
                    if (identity == null) continue;

                    if (effect.Type == TileEffectType.Flame)
                    {
                        // Apply Burning
                        identity.AddStatus(new StatusEffect(CombatStatusIds.Burning, 1, 3));
                        Debug.Log($"[CombatTileManager] Flame tile applied Burning to {unityUnit.name}");
                    }
                    else if (effect.Type == TileEffectType.Ice)
                    {
                        // Apply Freezing (accumulate stacks logic handled by StatusManager or here?)
                        // "accumulates percent stacks while unit stands in ice tiles"
                        // For MVP, just add Freezing status
                        identity.AddStatus(new StatusEffect(CombatStatusIds.Freezing, 1, 3));
                        Debug.Log($"[CombatTileManager] Ice tile applied Freezing to {unityUnit.name}");
                    }
                }
            }
        }

        private void ProcessTileLogic()
        {
            var cellsToSpreadTo = new List<Vector2Int>();
            var expiredCoords = new List<Vector2Int>();

            // Create a snapshot of keys to iterate
            var keys = _activeEffects.Keys.ToList();

            foreach (var coords in keys)
            {
                var effect = _activeEffects[coords];

                // Spread Logic (Flame)
                if (effect.Type == TileEffectType.Flame)
                {
                    // "At the start of each turn, a flame tile will spawn additional flame tiles in all directions adjacent"
                    // We need to be careful not to spread infinitely in one frame.
                    // Only spread if it's not a newly spawned tile? 
                    // For MVP, let's just spread to neighbors.
                    var cell = _gridController.CellManager.GetCellAt(new Vector2IntImpl(coords.x, coords.y));
                    if (cell != null)
                    {
                        foreach (var neighbor in cell.GetNeighbours(_gridController.CellManager))
                        {
                            var nCoords = new Vector2Int(neighbor.GridCoordinates.x, neighbor.GridCoordinates.y);
                            if (!_activeEffects.ContainsKey(nCoords))
                            {
                                cellsToSpreadTo.Add(nCoords);
                            }
                        }
                    }
                }

                // Duration Logic
                var nextEffect = effect.DecrementDuration();
                if (nextEffect.IsExpired)
                {
                    expiredCoords.Add(coords);
                }
                else
                {
                    _activeEffects[coords] = nextEffect;
                }
            }

            // Apply Spread
            foreach (var coords in cellsToSpreadTo)
            {
                var cell = _gridController.CellManager.GetCellAt(new Vector2IntImpl(coords.x, coords.y));
                if (cell != null)
                {
                    // New flames last 3 turns
                    AddEffect(cell, TileEffectType.Flame, 3);
                }
            }

            // Remove Expired
            foreach (var coords in expiredCoords)
            {
                var cell = _gridController.CellManager.GetCellAt(new Vector2IntImpl(coords.x, coords.y));
                if (cell != null)
                {
                    RemoveEffect(cell);
                }
            }
        }

        private void UpdateVisuals(ICell cell, TileEffectType type)
        {
            var unityCell = cell as Cell;
            if (unityCell == null) return;

            var renderer = unityCell.GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            var coords = new Vector2Int(cell.GridCoordinates.x, cell.GridCoordinates.y);
            if (!_originalColors.ContainsKey(coords))
            {
                _originalColors[coords] = renderer.material.color;
            }

            switch (type)
            {
                case TileEffectType.Flame:
                    renderer.material.color = Color.red;
                    break;
                case TileEffectType.Ice:
                    renderer.material.color = Color.cyan;
                    break;
                case TileEffectType.Shadow:
                    renderer.material.color = Color.black; // or dark purple
                    break;
            }
        }

        private void RestoreVisuals(ICell cell)
        {
            var unityCell = cell as Cell;
            if (unityCell == null) return;

            var renderer = unityCell.GetComponentInChildren<Renderer>();
            if (renderer == null) return;

            var coords = new Vector2Int(cell.GridCoordinates.x, cell.GridCoordinates.y);
            if (_originalColors.TryGetValue(coords, out var originalColor))
            {
                renderer.material.color = originalColor;
                _originalColors.Remove(coords);
            }
        }
    }
}
