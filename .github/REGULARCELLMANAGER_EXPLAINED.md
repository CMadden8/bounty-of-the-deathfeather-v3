# RegularCellManager Explained

## What is RegularCellManager?

`RegularCellManager` is a Unity-specific implementation of the TBSF (Turn-Based Strategy Framework) `ICellManager` interface. It's part of the framework's architecture for managing grid cells in a Unity scene.

## Why is it needed?

The TBSF framework separates concerns into:
- **Common layer** (platform-agnostic logic): `ICellManager`, `ICell`, `IUnit`, etc.
- **Unity layer** (Unity-specific implementations): `UnityCellManager`, `Cell`, `Unit`, etc.

The `UnityGridController` requires a concrete `ICellManager` implementation to:
1. **Track all cells** in the scene
2. **Look up cells by coordinates** (e.g., `GetCellAt(Vector2Int)`)
3. **Apply highlighting** (reachable, path, etc.) by delegating to individual `Cell` instances
4. **Notify the framework** when cells are added/removed

### Without RegularCellManager
- `GridController.CellManager` would be `null`
- Movement abilities couldn't query available cells
- Cell highlighting (reachable/path) wouldn't work
- Cell click events wouldn't propagate correctly

## How RegularCellManager Works

1. **Initialization** (called by `UnityGridController.Awake`):
   - Scans all child GameObjects for `ICell` components
   - Builds an internal list of cells
   - Fires `CellAdded` events for each cell

2. **Cell Lookup**:
   ```csharp
   public override ICell GetCellAt(IVector2Int coords)
   {
       return _cells.Where(c => c.GridCoordinates.Equals(coords)).FirstOrDefault();
   }
   ```

3. **Highlighting Delegation**:
   - `MarkAsReachable(cells)` → calls `Cell.MarkAsReachable()` on each cell
   - `MarkAsPath(cells, origin)` → calls `Cell.MarkAsPath(path, index, origin)` on each cell
   - `UnMark(cells)` → calls `Cell.UnMark()` to clear highlights

4. **Scene Hierarchy**:
   ```
   UnityGridController
   ├── RegularCellManager  ← CellManager reference
   │   ├── Cell_0_0  ← Painted cell prefab instances
   │   ├── Cell_1_0
   │   ├── Cell_2_0
   │   └── ...
   ├── UnityUnitManager
   ├── UnityPlayerManager
   └── TurnResolver
   ```

## Common Issues

### Issue: "GridController has no UnityCellManager assigned"
**Cause:** The scene's `UnityGridController` Inspector has an empty `CellManager` field.

**Fix:**
1. Open Unity Editor
2. Find `UnityGridController` in Hierarchy (search `t:UnityGridController`)
3. Inspect the `CellManager` field
4. If empty, use menu: `Tools → Combat POC → Ensure RegularCellManager Assigned`
5. Or manually drag the `RegularCellManager` GameObject onto the field

### Issue: Cells lack Reachable/Path highlighters
**Cause:** Cell prefabs were created before `SimpleCubeCellCreator` added highlighters via reflection.

**Fix:**
1. **Recreate prefabs**: Use `Tools → Combat POC → Create Simple 3D Cell Prefab (Green/Gray/Blue)`
2. **Repaint cells**: Delete old cells and paint new ones using Unity's Tile Painter (GridHelper window)
3. **Or use runtime fixer**: The `CellHighlighterAutoFixer` script will add highlighters at runtime (temporary fix)

## How Was It "Not Needed Before"?

**It was always needed** — you likely had it assigned and didn't notice because:
- The scene builder (`MinimalCombatSceneBuilder`) auto-locates and assigns it
- Previous scenes may have had the reference serialized already
- The framework's `AutoAssignDependencies()` tries to find it via `GetComponentInChildren<UnityCellManager>()`

The recent diagnostic scripts (`CellHighlighterChecker`) exposed the issue by explicitly checking the assignment.

## Editor Utilities

### Menu: Tools → Combat POC → Ensure RegularCellManager Assigned
- Finds or creates a `RegularCellManager` in the scene
- Assigns it to `UnityGridController.CellManager`
- Marks the scene dirty so changes are saved

### Menu: Tools → Combat POC → Validate Scene Setup
- Checks all required `UnityGridController` fields
- Logs warnings/errors for missing assignments
- Counts cells and validates configuration

## Summary

- **RegularCellManager** = TBSF's Unity implementation of `ICellManager`
- **Required** for cell lookup, highlighting, and framework integration
- **Auto-assigned** by scene builder, but must be explicitly set if missing
- **Always present** in properly configured TBSF scenes (not a new requirement)

For permanent fixes, use the editor utilities above. For temporary runtime patching, the `CellHighlighterAutoFixer` can assign it automatically when the scene starts.
