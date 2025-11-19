using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using TurnBasedStrategyFramework.Unity.Cells;
using TurnBasedStrategyFramework.Unity.Controllers;
using TurnBasedStrategyFramework.Unity.Highlighters;
using UnityEngine;

namespace BountyOfTheDeathfeather.Debugging
{
    /// <summary>
    /// Runtime helper that attempts to auto-fix common scene setup problems that
    /// prevent cell highlighting from working. It will:
    /// - assign a found `RegularCellManager` to the `UnityGridController` if none is assigned
    /// - add `RendererHighlighter` components to cells that are missing reachable/path highlighters
    /// This is a temporary, runtime-only fixer to aid testing.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class CellHighlighterAutoFixer : MonoBehaviour
    {
        [SerializeField] private UnityGridController _gridController;

        private void Start()
        {
            if (_gridController == null)
            {
                _gridController = Object.FindFirstObjectByType<UnityGridController>();
            }

            if (_gridController == null)
            {
                Debug.LogWarning("CellHighlighterAutoFixer: No UnityGridController found in scene.");
                return;
            }

            var cellManager = _gridController.CellManager as UnityCellManager;
            if (cellManager == null)
            {
                // Try to locate a RegularCellManager in scene and assign it to the grid controller
                var found = Object.FindFirstObjectByType<RegularCellManager>();
                if (found != null)
                {
                    _gridController.CellManager = found;
                    cellManager = found;
                    Debug.Log("CellHighlighterAutoFixer: Assigned found RegularCellManager to UnityGridController.CellManager.");
                }
                else
                {
                    Debug.LogWarning("CellHighlighterAutoFixer: No UnityCellManager (RegularCellManager) found to assign.");
                    return;
                }
            }

            var cells = cellManager.GetCells().OfType<Cell>().ToList();
            if (cells == null || cells.Count == 0)
            {
                Debug.LogWarning("CellHighlighterAutoFixer: No cells returned by the cell manager.");
                return;
            }

            var fieldUnMark = typeof(Cell).GetField("_unMarkFn", BindingFlags.Instance | BindingFlags.NonPublic);
            var fieldReachable = typeof(Cell).GetField("_markAsReachableFn", BindingFlags.Instance | BindingFlags.NonPublic);
            var fieldPath = typeof(Cell).GetField("_markAsPathFn", BindingFlags.Instance | BindingFlags.NonPublic);
            int fixedUnMark = 0, fixedReachable = 0, fixedPath = 0;

            foreach (var cell in cells)
            {
                if (cell == null) continue;

                var unMarkList = fieldUnMark?.GetValue(cell) as List<Highlighter>;
                var reachableList = fieldReachable?.GetValue(cell) as List<Highlighter>;
                var pathList = fieldPath?.GetValue(cell) as List<Highlighter>;

                // Find a renderer we can use for a RendererHighlighter
                var renderer = cell.GetComponentInChildren<Renderer>();
                
                // Get the current material color as the "base" color for UnMark
                Color baseColor = Color.gray;
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    baseColor = renderer.sharedMaterial.color;
                }

                // Add UnMark highlighter if missing (restores to base color)
                if ((unMarkList == null || unMarkList.Count == 0) && renderer != null)
                {
                    var rhUnMark = renderer.gameObject.AddComponent<RendererHighlighter>();
                    
                    var rhType = typeof(RendererHighlighter);
                    var rhRendererField = rhType.GetField("_renderer", BindingFlags.Instance | BindingFlags.NonPublic);
                    var rhColorField = rhType.GetField("_color", BindingFlags.Instance | BindingFlags.NonPublic);
                    
                    rhRendererField?.SetValue(rhUnMark, renderer);
                    rhColorField?.SetValue(rhUnMark, baseColor);
                    
                    var mpbField = rhType.GetField("_mpb", BindingFlags.Instance | BindingFlags.NonPublic);
                    var mpbUnMark = new MaterialPropertyBlock();
                    mpbUnMark.SetColor("_Color", baseColor);
                    mpbField?.SetValue(rhUnMark, mpbUnMark);

                    if (unMarkList == null)
                    {
                        unMarkList = new List<Highlighter>();
                        fieldUnMark?.SetValue(cell, unMarkList);
                    }
                    unMarkList.Add(rhUnMark);
                    fixedUnMark++;
                }

                if ((reachableList == null || reachableList.Count == 0) && renderer != null)
                {
                    var rh = renderer.gameObject.AddComponent<RendererHighlighter>();
                    
                    // Configure highlighter via reflection (reachable = yellow)
                    var rhType = typeof(RendererHighlighter);
                    var rhRendererField = rhType.GetField("_renderer", BindingFlags.Instance | BindingFlags.NonPublic);
                    var rhColorField = rhType.GetField("_color", BindingFlags.Instance | BindingFlags.NonPublic);
                    
                    rhRendererField?.SetValue(rh, renderer);
                    rhColorField?.SetValue(rh, new Color(1f, 1f, 0.5f)); // Yellow for reachable
                    
                    // Manually initialize MaterialPropertyBlock since Awake hasn't been called yet
                    var mpbField = rhType.GetField("_mpb", BindingFlags.Instance | BindingFlags.NonPublic);
                    var mpb = new MaterialPropertyBlock();
                    mpb.SetColor("_Color", new Color(1f, 1f, 0.5f));
                    mpbField?.SetValue(rh, mpb);

                    if (reachableList == null)
                    {
                        reachableList = new List<Highlighter>();
                        fieldReachable?.SetValue(cell, reachableList);
                    }
                    reachableList.Add(rh);
                    fixedReachable++;
                }

                if ((pathList == null || pathList.Count == 0) && renderer != null)
                {
                    var rh2 = renderer.gameObject.AddComponent<RendererHighlighter>();
                    
                    // Configure highlighter via reflection (path = green)
                    var rhType = typeof(RendererHighlighter);
                    var rhRendererField = rhType.GetField("_renderer", BindingFlags.Instance | BindingFlags.NonPublic);
                    var rhColorField = rhType.GetField("_color", BindingFlags.Instance | BindingFlags.NonPublic);
                    
                    rhRendererField?.SetValue(rh2, renderer);
                    rhColorField?.SetValue(rh2, new Color(0.5f, 1f, 0.5f)); // Green for path
                    
                    // Manually initialize MaterialPropertyBlock since Awake hasn't been called yet
                    var mpbField = rhType.GetField("_mpb", BindingFlags.Instance | BindingFlags.NonPublic);
                    var mpb2 = new MaterialPropertyBlock();
                    mpb2.SetColor("_Color", new Color(0.5f, 1f, 0.5f));
                    mpbField?.SetValue(rh2, mpb2);

                    if (pathList == null)
                    {
                        pathList = new List<Highlighter>();
                        fieldPath?.SetValue(cell, pathList);
                    }
                    pathList.Add(rh2);
                    fixedPath++;
                }
            }

            Debug.Log($"CellHighlighterAutoFixer: Scanned {cells.Count} cells. Added UnMark={fixedUnMark}, Reachable={fixedReachable}, Path={fixedPath}.");
        }
    }
}
