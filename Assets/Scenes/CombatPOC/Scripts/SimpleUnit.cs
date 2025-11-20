using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TurnBasedStrategyFramework.Common.Cells;
using TurnBasedStrategyFramework.Common.Controllers;
using TurnBasedStrategyFramework.Common.Controllers.GridStates;
using TurnBasedStrategyFramework.Common.Units;
using TurnBasedStrategyFramework.Unity.Controllers;
using TurnBasedStrategyFramework.Unity.Units;
using TurnBasedStrategyFramework.Unity.Units.Abilities;
using UnityEngine;

namespace CombatPOC.Units
{
    /// <summary>
    /// Minimal concrete unit implementation for quick MVP prototyping.
    /// Uses simple color changes instead of highlighter stacks and skips advanced ability logic.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SimpleUnit : Unit
    {
        [Header("Dependencies")]
        [SerializeField] private UnityGridController _gridController;
        
        [Header("Visuals")]
        [SerializeField] private Renderer _renderer;
        [SerializeField] private Material _baseMaterial;
        [SerializeField] private Color _playerColor = new Color(0.3f, 0.7f, 0.95f);
        [SerializeField] private Color _enemyColor = new Color(0.9f, 0.35f, 0.35f);
        [SerializeField] private Color _selectedColor = new Color(1f, 0.9f, 0.4f);
        [SerializeField] private Color _finishedColor = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _movingColor = new Color(0.55f, 0.9f, 0.55f);
        [SerializeField] private Color _targetableColor = new Color(1f, 0.5f, 0.2f);
        [SerializeField] private Color _attackingColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color _defendingColor = new Color(0.2f, 0.4f, 1f);

        private Material _runtimeMaterial;

        private void Awake()
        {
            CacheRenderer();
            EnsureBaseAbilities();
            if (_baseMaterial != null)
            {
                _renderer.sharedMaterial = _baseMaterial;
            }
            ApplyIdleColor();
        }

        /// <summary>
        /// Called after Initialize() by the framework. Wire up click handling for unit selection.
        /// </summary>
        private void Start()
        {
            if (_gridController == null)
            {
                _gridController = Object.FindFirstObjectByType<UnityGridController>();
            }

            if (_gridController != null)
            {
                // Subscribe to own UnitClicked event to handle selection
                UnitClicked += OnUnitClickedHandler;
            }
            else
            {
                Debug.LogWarning($"SimpleUnit '{name}': No UnityGridController found. Unit selection will not work.", this);
            }
        }

        private void OnDestroy()
        {
            // Clean up event subscription
            UnitClicked -= OnUnitClickedHandler;
        }

        /// <summary>
        /// Handle unit click: select this unit if it belongs to the current player, otherwise deselect.
        /// </summary>
        private void OnUnitClickedHandler(IUnit clickedUnit)
        {
            if (_gridController == null || (Object)clickedUnit != this)
            {
                return;
            }

            // Check if it's currently this player's turn
            if (_gridController.TurnContext.CurrentPlayer.PlayerNumber != PlayerNumber)
            {
                Debug.Log($"SimpleUnit '{name}': Can't select - not our turn. Current: P{_gridController.TurnContext.CurrentPlayer.PlayerNumber}, Unit: P{PlayerNumber}");
                return;
            }

            // Check if this unit is already selected (avoid re-entering state)
            if (_gridController.GridState is GridStateUnitSelected)
            {
                Debug.Log($"SimpleUnit '{name}': Already in selection state, ignoring re-selection");
                return;
            }

            Debug.Log($"SimpleUnit '{name}': Selected! PlayerNumber={PlayerNumber}, BaseAbilities={GetBaseAbilities().Count()}");
            
            // Select this unit, passing base abilities (MoveAbility, AttackAbility) to the state
            // This will trigger MoveAbility.Display() which highlights reachable cells
            _gridController.GridState = new GridStateUnitSelected(this, GetBaseAbilities());
        }

        private void Reset()
        {
            CacheRenderer();
            EnsureBaseAbilities();
        }

        private void OnValidate()
        {
            CacheRenderer();
            if (!Application.isPlaying)
            {
                EnsureBaseAbilities();
            }
            if (_baseMaterial != null && _renderer != null && !Application.isPlaying)
            {
                _renderer.sharedMaterial = _baseMaterial;
            }
            if (!Application.isPlaying)
            {
                ApplyIdleColor();
            }
        }

        /// <summary>
        /// Guarantees core Move/Attack abilities exist so selection can enumerate and display them.
        /// Only runs in edit mode to prevent runtime duplicates.
        /// </summary>
        private void EnsureBaseAbilities()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (GetComponent<MoveAbility>() == null)
                {
                    gameObject.AddComponent<MoveAbility>();
                }
                if (GetComponent<AttackAbility>() == null)
                {
                    gameObject.AddComponent<AttackAbility>();
                }
            }
#endif
        }

        private void CacheRenderer()
        {
            if (_renderer != null)
            {
                return;
            }

            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer == null)
            {
                var meshFilter = GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    meshFilter = gameObject.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
                }

                var meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    meshRenderer = gameObject.AddComponent<MeshRenderer>();
                }

                _renderer = meshRenderer;
            }
        }

        private void ApplyIdleColor()
        {
            ApplyColor(PlayerNumber == 0 ? _playerColor : _enemyColor);
        }

        private void ApplyColor(Color color)
        {
            if (_renderer == null)
            {
                return;
            }

            if (_baseMaterial != null)
            {
                _renderer.sharedMaterial.color = color;
            }
            else
            {
                if (_runtimeMaterial == null)
                {
                    var shader = Shader.Find("Standard");
                    _runtimeMaterial = new Material(shader);
                    _renderer.material = _runtimeMaterial;
                }
                _runtimeMaterial.color = color;
            }
        }

        public override Task UnMark()
        {
            ApplyIdleColor();
            return Task.CompletedTask;
        }

        public override Task MarkAsSelected()
        {
            ApplyColor(_selectedColor);
            return Task.CompletedTask;
        }

        public override Task MarkAsFriendly()
        {
            ApplyColor(_playerColor);
            return Task.CompletedTask;
        }

        public override Task MarkAsFinished()
        {
            ApplyColor(_finishedColor);
            return Task.CompletedTask;
        }

        public override Task MarkAsTargetable()
        {
            ApplyColor(_targetableColor);
            return Task.CompletedTask;
        }

        public override Task MarkAsAttacking(Unit otherUnit)
        {
            ApplyColor(_attackingColor);
            return Task.CompletedTask;
        }

        public override Task MarkAsDefending(Unit otherUnit)
        {
            ApplyColor(_defendingColor);
            return Task.CompletedTask;
        }

        public override Task MarkAsMoving(ICell source, ICell destination, IEnumerable<ICell> path)
        {
            ApplyColor(_movingColor);
            return Task.CompletedTask;
        }

        public override Task UnMarkAsMoving(ICell source, ICell destination, IEnumerable<ICell> path)
        {
            ApplyIdleColor();
            return Task.CompletedTask;
        }

        public override Task MarkAsDestroyed()
        {
            ApplyColor(Color.black);
            return Task.CompletedTask;
        }

        public override void OnTurnStart(IGridController gridController)
        {
            base.OnTurnStart(gridController);
            // Reset action and movement points at the start of each turn
            ActionPoints = MaxActionPoints;
            MovementPoints = MaxMovementPoints;
            
            // Sync with CombatIdentity if present
            var identity = GetComponent<BountyOfTheDeathfeather.CombatSystem.CombatIdentity>();
            if (identity != null)
            {
                identity.ActionPoints = (int)ActionPoints;
                identity.MovementPoints = (int)MovementPoints;
                identity.GearPoints = identity.MaxGearPoints; // Reset GP as well
            }
        }
    }
}
