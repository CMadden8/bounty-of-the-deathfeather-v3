using System.Collections.Generic;
using System.Threading.Tasks;
using TurnBasedStrategyFramework.Common.Cells;
using TurnBasedStrategyFramework.Common.Units;
using TurnBasedStrategyFramework.Unity.Units;
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
            if (_baseMaterial != null)
            {
                _renderer.sharedMaterial = _baseMaterial;
            }
            ApplyIdleColor();
        }

        private void Reset()
        {
            CacheRenderer();
        }

        private void OnValidate()
        {
            CacheRenderer();
            if (_baseMaterial != null && _renderer != null && !Application.isPlaying)
            {
                _renderer.sharedMaterial = _baseMaterial;
            }
            if (!Application.isPlaying)
            {
                ApplyIdleColor();
            }
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
    }
}
