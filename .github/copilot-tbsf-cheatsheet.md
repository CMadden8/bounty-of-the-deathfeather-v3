Copilot — TBSF quick reference

Purpose
- A focused, single-file cheat-sheet the AI should consult when working on turn-based gameplay in this repo. Keep changes small and always verify behavior with examples/tests before editing core turn logic.

Where the canonical docs live
- Canonical project docs: `Assets/TBSFramework/README.html`
- Core library: `Assets/TBSFramework/External/tbsf-common/`
- Working examples: `Assets/TBSFramework/Examples/TilemapExample/`, `Assets/TBSFramework/Examples/LegacyDemos/`, `Assets/TBSFramework/Examples/ClashOfHeroes/`
- Editor helpers: `Assets/TBSFramework/Editor/`

Key TBSF concepts the AI must consult before making changes
1. Turn lifecycle and managers
   - Look for classes and GameObjects named `TurnManager`, `TurnResolver`, `Turn`, `TurnPhase`, or similar. These control turn progression and should not be changed without tests.
2. Player & Game controllers
   - `PlayerManager` / `GameController` own player state, action queues, and high-level rules.
3. Units, Cells, Grid
   - Unit model: search for `Unit`, `IUnit`, `UnitType`.
   - Cell/Grid model: search for `Cell`, `ICell`, `GridController`, `TilemapCellManager`.
   - Abilities/Actions: search for `IAbility`, `Action`, `Command` implementations.
4. Event hooks
   - Find published events or callbacks used by UI/editor to display turn / action updates. Prefer hooking into existing hooks rather than polling.
  - Look for UnityEvents, C# events, or On... methods in editor helpers under `Assets/TBSFramework/Editor/`.
5. Tests & Examples
   - Always check examples for canonical behaviour. Examples implement working patterns that should be followed when possible.

AI change checklist (must follow before committing changes that affect TBSF behavior)
- [ ] Read `Assets/TBSFramework/README.html` and at least one example implementation relevant to the change.
- [ ] Use MCP to inspect the running scene and locate the runtime `TurnManager`/controllers when possible.
- [ ] If change affects turn resolution, add or update EditMode/PlayMode tests in the repo and run them (via MCP or Unity Test Runner) before committing.
- [ ] Keep changes small and reversible. When changing serialized data structures, include migration notes.

MCP examples (how the AI should interact with Unity Editor at runtime)
- Find `TurnManager` in the scene hierarchy (pseudo):
  - Resource: `unity://scenes-hierarchy` → find entry where name contains `TurnManager` or `Turn`.
  - Tool: `select_gameobject` with the found hierarchy path to focus it.
- Inspect a GameObject's components:
  - Resource: `unity://gameobject/{id}` (use the id from `scenes-hierarchy`) to list components and public fields.
- Run tests / diagnostics:
  - Tool: `run_tests` (EditMode or PlayMode) to execute unit/tests and return results.
  - Tool: `execute_menu_item` to run editor tools (if the project exposes diagnostics menu items).
- Fetch Unity console logs:
  - Resource: `unity://logs` to see errors and runtime traces after executing a scenario.

Concrete class & file references (search these first)
- `Assets/TBSFramework/Scripts/controllers/turnResolvers/SubsequentTurnResolver.cs`
- `Assets/TBSFramework/Scripts/controllers/turnResolvers/UnityTurnResolver.cs`
- `Assets/TBSFramework/Scripts/controllers/GridController.cs`
- `Assets/TBSFramework/External/tbsf-common/common/players/HumanPlayer.cs`
- `Assets/TBSFramework/Examples/TilemapExample/Scripts/Cells/TilemapCellManager.cs`
- `Assets/TBSFramework/Examples/*` (TilemapExample, LegacyDemos, ClashOfHeroes) — copy patterns from here

Simple MCP pattern to inspect TurnManager (preferred flow)
1. Query the scene hierarchy (resource: `unity://scenes-hierarchy`) to get GameObject ids and names.
2. Find an entry with a name containing `TurnManager`, `Turn`, or `TurnResolver`.
3. Use the `unity://gameobject/{id}` resource to read component list and public fields.
4. If needed, use `select_gameobject` to focus the object in the Editor, then `execute_menu_item` or `run_tests` to exercise behaviors and `unity://logs` to capture output.

Quick grep terms the AI should use when searching code
- TurnManager | TurnResolver | TurnPhase | Turn
- IAbility | Ability | Action | Command
- PlayerManager | GameController
- TilemapCellManager | GridController | Cell | CellManager
- Unit | IUnit | UnitType

Helper script: locate TurnManager via MCP
- Path: `tools/find_turn_manager_mcp.js`
- Purpose: attempt common MCP queries and report any GameObjects whose name contains `Turn` (useful when class names vary).

How to run the helper script (local dev machine with Node.js)
```bash
node tools/find_turn_manager_mcp.js
```


Example WebSocket payload (if using raw MCP WebSocket):
- Execute menu item:
  {"id":"diag","method":"execute_menu_item","params":{"menuPath":"Tools/Combat Sandbox/Run Alignment Diagnostics"}}
- Get console logs:
  {"id":"logs","method":"get_console_logs","params":{"logType":null,"offset":0,"limit":50,"includeStackTrace":true}}

Where to search programmatically (grep terms)
- Turn, TurnManager, TurnResolver, TurnPhase
- IAbility, Ability, Action, Command
- PlayerManager, GameController
- TilemapCellManager, GridController, Cell

Recommended additions to `.github/copilot-instructions.md`
- A short pointer linking to this file so the main copilot instructions know to consult it before editing TBSF code.
- A short, concrete MCP pattern for inspecting turn state: (1) query `unity://scenes-hierarchy`, (2) select the runtime TurnManager, (3) call `unity://gameobject/{id}` to read fields, (4) run tests.

Notes and cautions
- Turn-based logic is stateful; small code changes can produce non-obvious breakage. Prefer tests and example-driven edits.
- Avoid committing regenerated project files (`*.csproj`, `*.sln`) — these are ignored in the repo's `.gitignore`.

If you'd like, I can:
- Append a short pointer in `.github/copilot-instructions.md` that references this file (non-destructive), or
- Insert a condensed TBSF section directly into `.github/copilot-instructions.md` (edits the existing file).

-- End of cheatsheet
