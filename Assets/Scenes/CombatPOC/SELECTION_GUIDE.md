# Combat POC - Unit Selection & Movement Range Guide

## Implementation Summary

I've implemented basic MVP unit selection and movement range visualization using the Turn-Based Strategy Framework (TBSF). Here's what was added:

### Changes Made

#### 1. SimpleUnit.cs - Selection Handler
- **Added click event handling**: When a unit is clicked during its player's turn, it's selected automatically
- **GridState integration**: Sets `GridStateUnitSelected` which triggers ability displays
- **Auto-find GridController**: Automatically finds the UnityGridController in the scene
- **Turn validation**: Only allows selection during the unit's owner's turn

#### 2. Auto-Added Abilities
- **MoveAbility**: Automatically added via Reset() - handles movement range highlighting
- **AttackAbility**: Automatically added via Reset() - handles attack target highlighting

#### 3. EnsureCameraRaycaster.cs - Editor Utility
- **Menu**: Tools → Combat POC → Ensure Camera Raycaster
- **Function**: Verifies Main Camera has PhysicsRaycaster component
- **Auto-creates EventSystem** if missing

---

## How to Use

### First-Time Setup

1. **Run Camera Raycaster Tool**
   - In Unity Editor: `Tools → Combat POC → Ensure Camera Raycaster`
   - This ensures your camera can detect clicks on 3D objects

2. **Open Your Scene**
   - Navigate to your CombatPOC scene
   - Ensure UnityGridController, CellManager, UnitManager, PlayerManager are present

3. **Check Unit Prefabs**
   - Select `Unit_Player.prefab` in Project window
   - In Inspector, verify these components exist:
     - SimpleUnit
     - MoveAbility
     - AttackAbility
     - Collider (for click detection)
   - Repeat for `Unit_Enemy.prefab`

4. **If Abilities Missing** (shouldn't happen but just in case):
   - Select the prefab
   - In Inspector: SimpleUnit component → three-dot menu → Reset
   - This will auto-add MoveAbility and AttackAbility

### Playing the Game

1. **Enter Play Mode**

2. **Select a Unit**
   - Click on `Unit_Player` (the unit for Player 0)
   - If it's Player 0's turn, the unit will be selected
   - You should see:
     - Unit changes to "selected" color (yellow by default)
     - Reachable cells are highlighted (yellow)
     - A movement path appears when hovering over cells

3. **Move the Unit**
   - Click on any highlighted (yellow) cell
   - Unit will move to that cell
   - Movement range updates after moving

4. **Deselect**
   - Click on another friendly unit to select it instead
   - Click on empty space (cell) to deselect

5. **Turn Management**
   - After moving/acting, end the turn (if GUIController is set up)
   - Or manually via code: `gridController.EndTurn()`

---

## Expected Behavior

### When You Click a Unit:

**During the unit's player turn:**
- Unit color → Yellow (selected)
- Reachable cells → Yellow highlight
- Hover over cells shows green path
- Click cell → unit moves

**During opponent's turn:**
- Nothing happens (can't select opponent's units on their turn)

### Movement Range Calculation:

The framework automatically calculates reachable cells based on:
- `MovementPoints` (default: configured on unit)
- Cell `MovementCost` (default: 1 per cell)
- Pathfinding via A* algorithm
- `IsCellMovableTo()` checks (walkable, not taken, etc.)

---

## Troubleshooting

### "Nothing happens when I click units"

**Check:**
1. Camera has PhysicsRaycaster component (run Tools → Ensure Camera Raycaster)
2. EventSystem exists in scene hierarchy
3. Units have Collider components
4. It's the correct player's turn (check TurnContext.CurrentPlayer)

**Debug:**
```csharp
// Add to SimpleUnit.OnUnitClickedHandler:
Debug.Log($"Unit clicked: {name}, PlayerNumber: {PlayerNumber}, CurrentPlayer: {_gridController.TurnContext.CurrentPlayer.PlayerNumber}");
```

### "No cells are highlighted when I select a unit"

**Check:**
1. MoveAbility component exists on unit (Inspector)
2. Unit has MovementPoints > 0
3. Cells have Square component and highlighters assigned
4. CellManager is initialized

**Debug:**
```csharp
// Check in Console during Play mode:
// - "MoveAbility.Display() called"
// - Check unit's MovementPoints value
```

### "Cells are highlighted but I can't see them"

**Check:**
1. Cells have `_markAsReachableFn` highlighters assigned
2. Camera is positioned to see the grid
3. Cell materials/colors are visible

**Fix:**
- Ensure SimpleCubeCell prefabs have highlighter setup
- Check cell color isn't too subtle

---

## Customization

### Change Movement Range
```csharp
// On unit prefab/instance:
unit.MovementPoints = 5; // How far unit can move per turn
```

### Change Cell Traversal Cost
```csharp
// On cell prefab:
cell.MovementCost = 2; // Costs 2 movement points to enter
```

### Change Highlight Colors
```csharp
// In SimpleUnit.cs:
[SerializeField] private Color _selectedColor = Color.yellow; // Change this
```

### Disable Movement Confirmation
```csharp
// On MoveAbility component:
// Uncheck "With Confirmation" in Inspector
// Unit moves immediately on cell click (no double-tap)
```

---

## Next Steps (Beyond MVP)

Once basic selection/movement works, you can extend with:

1. **Attack Range Visualization**
   - AttackAbility already added - should show attack targets when selected
   - Red highlight on attackable enemies

2. **UI Feedback**
   - Add unit info panel (name, health, stats)
   - Show selected unit portrait
   - Display movement/action points remaining

3. **Turn End Button**
   - Add GUIController with End Turn button
   - Or: press Space to end turn

4. **AI Player**
   - Change Player_1 from HumanPlayer to AIPlayer
   - AI will auto-select and move units

5. **Advanced Abilities**
   - Add custom abilities (heal, special attack, etc.)
   - Create compound abilities (spellbook menu)

---

## Framework Reference

### Key Classes Used

- **GridStateUnitSelected**: State when unit is selected, displays abilities
- **GridStateAwaitInput**: Default state waiting for player input
- **MoveAbility**: Handles movement logic and range visualization
- **IGridController**: Central coordinator for grid, units, players, turns
- **ICellManager**: Manages cells, highlighting, pathfinding queries

### Key Events

- **UnitClicked**: Fired when unit GameObject is clicked
- **CellClicked**: Fired when cell GameObject is clicked
- **TurnStarted**: Fired when new turn begins
- **TurnEnded**: Fired when turn ends

### Documentation

For more advanced features, see:
- `.github/copilot-instructions.md` - MCP Unity integration
- `.github/copilot-tbsf-cheatsheet.md` - Quick TBSF reference
- `Assets/TBSFramework/README.html` - Full framework documentation

---

## Testing Checklist

- [ ] Camera Raycaster tool run successfully
- [ ] Unit prefabs have MoveAbility and AttackAbility components
- [ ] Unit prefabs have Collider components
- [ ] Scene has UnityGridController with managers assigned
- [ ] Scene has EventSystem
- [ ] Painted some cells with SimpleCubeCell prefabs
- [ ] Placed Unit_Player and Unit_Enemy in scene
- [ ] Enter Play mode
- [ ] Click Unit_Player → yellow color, cells highlighted
- [ ] Click highlighted cell → unit moves
- [ ] Movement range updates after move
- [ ] Can select other units
- [ ] Opponent turn prevents selection of opponent units

---

## Quick Commands

### Run the Camera Setup Tool
Unity Editor → `Tools` → `Combat POC` → `Ensure Camera Raycaster`

### Check Current Turn
```csharp
// During Play mode, check Console:
Debug.Log($"Current Player: {gridController.TurnContext.CurrentPlayer.PlayerNumber}");
```

### Manually End Turn
```csharp
gridController.EndTurn();
```

### Select Unit via Code
```csharp
gridController.GridState = new GridStateUnitSelected(unit, unit.GetBaseAbilities());
```

---

Good luck testing! Let me know if you encounter any issues.
