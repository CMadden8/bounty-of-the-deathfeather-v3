# GitHub Copilot Instructions for Bounty of the Deathfeather

## Unity Editor Integration via MCP

**PREFERENCE**: When performing tasks related to the Unity application, always use the MCP (Model Context Protocol) Unity bridge to execute operations directly in the Unity Editor. This is the preferred method over local file editing or CLI commands.

### MCP Unity Connection Setup

1. **Prerequisites**
   - Unity Editor must be running with the project open
   - MCP Unity package installed (already present in `Library/PackageCache/com.gamelovers.mcp-unity@...`)
   - MCP server configured in `.vscode/mcp.json` (already configured)

2. **Starting the MCP Unity Server**
   - In Unity Editor: `Tools → MCP Unity → Server Window`
   - Click "Start Server" to start the WebSocket server on port 8090
   - Unity will show "Server online" when ready
   - The MCP tools become available through the built-in MCP client in this editor

3. **Verifying Connection**
   - The Unity MCP bridge is available when Unity's Server Window shows "Server online"
   - You can test connectivity with: `node -e "const WebSocket=require('ws'); const ws=new WebSocket('ws://localhost:8090/McpUnity'); ws.on('open',()=>{ console.log('Connected'); ws.close(); }); ws.on('error',(e)=>{ console.error('Error', e); });"`
   - Once connected, MCP tools like `execute_menu_item`, `update_gameobject`, `select_gameobject`, etc. are available

### Available MCP Unity Tools

The MCP Unity server exposes these tools (use via built-in MCP client):

- **`execute_menu_item`** — Execute Unity menu items (e.g., run editor scripts, diagnostics, custom workflows)
  - Example: `{ menuItemPath: "Tools/Combat Sandbox/Run Alignment Diagnostics" }`
- **`select_gameobject`** — Select GameObjects in the Unity hierarchy by path or instance ID
- **`update_gameobject`** — Update GameObject properties (name, tag, layer, active/static state) or create new GameObjects
- **`update_component`** — Add or update component fields on GameObjects
- **`add_package`** — Install Unity packages via Package Manager
- **`run_tests`** — Run Unity Test Runner tests (EditMode, PlayMode)
- **`send_console_log`** — Send log messages to Unity Console
- **`add_asset_to_scene`** — Add assets from AssetDatabase to the current scene
- **`create_prefab`** — Create prefabs with optional MonoBehaviour scripts and field values
- **`recompile_scripts`** — Trigger recompilation of all scripts

### MCP Unity Resources

Query Unity state via MCP resources:

- **`unity://menu-items`** — List all available Unity menu items
- **`unity://scenes-hierarchy`** — Get current scene hierarchy structure
- **`unity://gameobject/{id}`** — Get detailed GameObject info (components, properties, fields)
- **`unity://logs`** — Retrieve Unity Console logs
- **`unity://packages`** — List installed and available packages
- **`unity://assets`** — Query Unity AssetDatabase
- **`unity://tests/{testMode}`** — List available tests

### Workflow: Running Unity Editor Operations via MCP

**IMPORTANT**: MCP Unity tools are available through the IDE's native MCP client integration. When the Unity MCP server is running (Tools → MCP Unity → Server Window → "Server online"), the following MCP tools become available to MCP-enabled IDEs (Windsurf, Cursor, Claude Desktop):

**Example 1: Run Alignment Diagnostics**
```
Tool: execute_menu_item
Arguments: { menuItemPath: "Tools/Combat Sandbox/Run Alignment Diagnostics" }
```

**Example 2: Organize Combat Sandbox Hierarchy**
```
Tool: execute_menu_item
Arguments: { menuItemPath: "Tools/Combat Sandbox/Organize Hierarchy" }
```

**Example 3: Fix Misalignments**
```
Tool: execute_menu_item
Arguments: { menuItemPath: "Tools/Combat Sandbox/Fix Misalignments" }
```

**Example 4: Query Scene Hierarchy**
```
Resource: unity://scenes-hierarchy
```

**Example 5: Check Unity Console Logs**
```
Resource: unity://logs
```

**Note**: If MCP tools are not directly accessible, ask the user to run the menu command manually in Unity Editor, or use the fallback Unity CLI method below.

### Command-Line MCP Usage (WebSocket Direct)

- The Unity MCP bridge exposes a plain WebSocket endpoint at `ws://localhost:8090/McpUnity`.
- Requests are **not** JSON-RPC; send a minimal JSON payload with `id`, `method`, and `params`.
- Execute a menu item:
  ```bash
  node -e "const WebSocket=require('ws'); const ws=new WebSocket('ws://localhost:8090/McpUnity'); ws.on('open',()=>{ const payload={id:'diag',method:'execute_menu_item',params:{menuPath:'Tools/Combat Sandbox/Run Alignment Diagnostics'}}; ws.send(JSON.stringify(payload)); }); ws.on('message',d=>{console.log(d.toString()); ws.close();});"
  ```
- Fetch Unity console logs (replace `limit` / `includeStackTrace` as needed):
  ```bash
  node -e "const WebSocket=require('ws'); const ws=new WebSocket('ws://localhost:8090/McpUnity'); ws.on('open',()=>{ const payload={id:'logs',method:'get_console_logs',params:{logType:null,offset:0,limit:20,includeStackTrace:false}}; ws.send(JSON.stringify(payload)); }); ws.on('message',d=>{console.log(d.toString()); ws.close();});"
  ```
- If you accidentally send JSON-RPC (`{"jsonrpc":"2.0","method":"tools/call"...}`) Unity replies with `Unknown method: tools/call`. Always send the simple payload above.

### When to Use MCP vs. Local File Editing

- **Use MCP** for:
  - Running Unity Editor operations (menu items, tests, recompilation)
  - Inspecting or modifying scene GameObjects and components at runtime
  - Querying Unity state (hierarchy, logs, packages, assets)
  - Creating/updating prefabs and assets in the Unity AssetDatabase
  - Any operation that requires Unity Editor context (scene management, serialization, asset pipeline)

- **Use local file editing** for:
  - Editing C# scripts, shaders, or configuration files (`.cs`, `.json`, `.asset`)
  - Creating new editor utilities or runtime scripts
  - Modifying project settings files that are text-based
  - Batch refactoring or search-replace operations across many files

### Important Notes

- Always ensure Unity Editor's MCP Server is started before attempting MCP operations
- MCP operations are synchronous from Unity's perspective — wait for completion before next call
- Unity console logs and errors are captured and returned in MCP tool responses
- If connection fails, verify Unity Editor is running and Server Window shows "Server online"
- The MCP bridge uses WebSocket (default port 8090) — ensure no firewall blocks localhost connections

### Combat Sandbox Specific Workflows

**Preferred MCP-based workflows for this project:**

1. **Diagnostics First**: Always run `Tools/Combat Sandbox/Run Alignment Diagnostics` before applying fixes
2. **Hierarchy Organization**: Use `Tools/Combat Sandbox/Organize Hierarchy` to ensure flat structure (Managers/Environment/Units/UI)
3. **Safe Fixes**: Use `Tools/Combat Sandbox/Fix Misalignments` only after reviewing diagnostics output
4. **Scene Validation**: Query `unity://scenes-hierarchy` and `unity://logs` to verify changes

**Custom MCP Entrypoints Available:**
- `AlignmentDiagnostics.RunDiagnosticsMCP` — menu: Tools/Combat Sandbox/Run Alignment Diagnostics
- `FixMisalignments.FixViaMCP` — menu: Tools/Combat Sandbox/Fix Misalignments
- `OrganizeCombatSandboxHierarchy.Organize` — menu: Tools/Combat Sandbox/Organize Hierarchy
- `CombatSandboxMCPWorkflow.*` — menu: Tools/Combat Sandbox/MCP Workflows/*

### Fallback: Unity CLI -executeMethod

If MCP tools are not accessible and Unity Editor is NOT already running, use Unity CLI with the fully-qualified method name:
```bash
"/c/Program Files/Unity/Hub/Editor/6000.2.7f2/Editor/Unity.exe" \
  -projectPath "C:/Users/madde/Bounty_Of_The_Deathfeather" \
  -executeMethod CombatSandbox.Editor.AlignmentDiagnostics.RunDiagnosticsMCP \
  -batchmode -quit \
  -logFile "diagnostics_log.txt"
```

**Important**: Unity CLI batchmode will fail if Unity Editor is already running with the project open. In that case, either:
1. Close Unity Editor first, then run the CLI command
2. Or run the menu command manually in Unity Editor: `Tools → Combat Sandbox → Run Alignment Diagnostics`

---

## Project-Specific Conventions

### Combat Sandbox Generator
- No automatic tile painting — manual grid setup only
- Use `autoOrganizeHierarchy` flag in `CombatSandboxTilemapGenerator` to auto-organize created objects
- Preferred hierarchy: `Managers/`, `Environment/Grid/`, `Units/`, `UI/`, `Camera & Lighting/`

### Code Style
- Editor scripts → `Assets/Editor/CombatSandbox/`
- Runtime scripts → `Assets/Scripts/CombatSandbox/Runtime/`
- Use `[MenuItem("Tools/Combat Sandbox/...")]` for editor tools
- Add MCP-friendly static entrypoints for automation (suffix with `MCP`)

### Git Workflow
- `.gitignore` updated to exclude: `node_modules/`, `.autogen/`, `.vscode/`, `.vs/`, `*.log`
- Commit logical chunks: editor utilities, runtime changes, docs separately
- Always run diagnostics before committing scene changes

### TBSF quick-reference

Note: this project uses the Turn-Based Strategy Framework (TBSF). For a concise, AI-focused quick reference and MCP examples that describe the TurnManager, PlayerManager, units/cells, and safe MCP patterns, consult
Note: this project uses the Turn-Based Strategy Framework (TBSF). For a concise, AI-focused quick reference and MCP examples that describe the TurnManager, PlayerManager, units/cells, and safe MCP patterns, consult
-`.github/copilot-tbsf-cheatsheet.md` before making changes to turn-based logic or editor automation.

Combat rules: consult the canonical combat mechanics reference at `.github/COMBAT_MECHANICS.md`.  The AI should read this file before proposing or making changes to combat logic, abilities, or status effects.
