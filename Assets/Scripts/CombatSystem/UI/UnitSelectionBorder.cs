using UnityEngine;

namespace BountyOfTheDeathfeather.CombatSystem.UI
{
    /// <summary>
    /// Visual indicator for units that are in attack range (attackable enemies).
    /// Displays a red border or highlight around the unit when enabled.
    /// </summary>
    public class UnitSelectionBorder : MonoBehaviour
    {
        [SerializeField] private GameObject _borderObject;
        [SerializeField] private Renderer _borderRenderer;
        [SerializeField] private Color _attackableColor = new Color(1f, 0f, 0f, 0.7f); // Semi-transparent red
        [SerializeField] private Color _selectedColor = new Color(1f, 1f, 0f, 0.8f); // Yellow

        private bool _isVisible;

        private void Awake()
        {
            // Try to find border object if not assigned
            if (_borderObject == null)
            {
                var borderTransform = transform.Find("SelectionBorder");
                if (borderTransform != null)
                {
                    _borderObject = borderTransform.gameObject;
                    _borderRenderer = _borderObject.GetComponent<Renderer>();
                }
                else
                {
                    // Create border dynamically if not found
                    CreateSelectionBorder();
                }
            }

            // Hide by default
            SetVisible(false);
        }

        private void CreateSelectionBorder()
        {
            // Create child GameObject
            _borderObject = new GameObject("SelectionBorder");
            _borderObject.transform.SetParent(transform);
            // Position at visual cube top surface: visual is at y=0 with height 0.1, so top is at 0.05 + small offset
            _borderObject.transform.localPosition = new Vector3(0.5f, 0.06f, 0.5f); // Centered at visual position (0.5, 0, 0.5) + top offset
            _borderObject.transform.localRotation = Quaternion.identity; // Keep flat in parent's space
            _borderObject.transform.localScale = Vector3.one * 1.0f; // Match tile size

            // Add LineRenderer for circular border
            LineRenderer lineRenderer = _borderObject.AddComponent<LineRenderer>();
            _borderRenderer = lineRenderer;

            // Configure LineRenderer
            lineRenderer.loop = false; // We manually close the loop with 5 points
            lineRenderer.startWidth = 0.03f;
            lineRenderer.endWidth = 0.03f;
            lineRenderer.useWorldSpace = false;

            // Create square border around tile edges (4 corners + close the loop)
            // Visual is scaled 0.9, so halfSize should be 0.45 to frame the edges
            float halfSize = 0.45f; // Frame the 0.9-unit visual tile edges
            Vector3[] positions = new Vector3[5];
            positions[0] = new Vector3(-halfSize, 0f, -halfSize); // Bottom-left
            positions[1] = new Vector3(halfSize, 0f, -halfSize);  // Bottom-right
            positions[2] = new Vector3(halfSize, 0f, halfSize);   // Top-right
            positions[3] = new Vector3(-halfSize, 0f, halfSize);  // Top-left
            positions[4] = positions[0]; // Close the loop
            lineRenderer.positionCount = 5;
            lineRenderer.SetPositions(positions);

            // Create or assign material
            Material borderMat = GetOrCreateBorderMaterial();
            lineRenderer.sharedMaterial = borderMat;

            // Use vertex colors on the LineRenderer for reliable transparency
            lineRenderer.startColor = _attackableColor;
            lineRenderer.endColor = _attackableColor;

            // Start disabled
            _borderObject.SetActive(false);
        }

        private static Material _borderMat;
        private static Material GetOrCreateBorderMaterial()
        {
            if (_borderMat != null) return _borderMat;

            // Try to load existing material
            _borderMat = Resources.Load<Material>("UnitSelectionBorder");
            if (_borderMat != null) return _borderMat;

            // Create new material for LineRenderer using built-in Sprites shader (works with Standard pipeline)
            _borderMat = new Material(Shader.Find("Sprites/Default"));
            _borderMat.name = "UnitSelectionBorder";
            // Sprites/Default uses _Color for tinting
            _borderMat.color = new Color(1f, 0f, 0f, 0.9f);

            return _borderMat;
        }

        /// <summary>
        /// Shows the border with the attackable (red) color.
        /// </summary>
        public void ShowAttackable()
        {
            SetVisible(true);
            if (_borderRenderer != null)
            {
                // Pure red color - no blue/green components
                Color redColor = new Color(1f, 0f, 0f, 0.6f); // Semi-transparent red (use vertex colors)
                _borderRenderer.material.color = redColor;
                if (_borderRenderer is LineRenderer lr)
                {
                    lr.startColor = redColor;
                    lr.endColor = redColor;
                }
                if (_borderRenderer.material.HasProperty("_Color"))
                {
                    _borderRenderer.material.SetColor("_Color", redColor);
                }
            }
        }

        /// <summary>
        /// Shows the border with the selected (yellow) color.
        /// </summary>
        public void ShowSelected()
        {
            SetVisible(true);
            if (_borderRenderer != null)
            {
                _borderRenderer.material.color = _selectedColor;
                if (_borderRenderer.material.HasProperty("_BaseColor"))
                {
                    _borderRenderer.material.SetColor("_BaseColor", _selectedColor);
                }
            }
        }

        /// <summary>
        /// Hides the border.
        /// </summary>
        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (_borderObject != null)
            {
                _borderObject.SetActive(visible);
            }
        }

        public bool IsVisible => _isVisible;
    }
}
