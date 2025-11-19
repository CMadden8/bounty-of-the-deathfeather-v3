# Highlight System - How It Works & How to Control It

## Overview

The Turn-Based Strategy Framework uses a **RendererHighlighter** system that **swaps material colors** using MaterialPropertyBlocks. This is **NOT** a transparent overlay system - it completely replaces the tile's color temporarily.

## How It Works

### 1. **Material Swap (Not Overlay)**
- When a tile is highlighted (for movement, path, hover, etc.), the system uses `MaterialPropertyBlock` to **replace** the tile's color
- The original tile material is NOT visible during highlighting
- This is a **complete color swap**, not a transparency blend

### 2. **RendererHighlighter Component**
Located in: `Assets/TBSFramework/Scripts/highlighters/RendererHighlighter.cs`

```csharp
public class RendererHighlighter : Highlighter
{
    [SerializeField] private Renderer _renderer;      // The tile's renderer
    [SerializeField] private Color _color;            // The highlight color
    [SerializeField] private string _propertyName;    // "_Color" or "_BaseColor"
    [SerializeField] private int _materialIndex;      // Usually 0
}
```

**Key Point:** The `_color` field is what you see when the tile is highlighted. It **replaces** the base tile color entirely.

## How to Control Highlight Colors

### Option 1: **Modify SimpleCubeCellCreator.cs** (Before Creating Prefabs)

Located in: `Assets/Scenes/CombatPOC/Scripts/SimpleCubeCellCreator.cs`

Lines 175-184:
```csharp
// UnMark - restore to the cell's base material color
GameObject unMarkObj = CreateRendererHighlighter("UnMark", highlightersContainer, renderer, baseColor);

// MarkAsHighlighted - for mouse hover (blue)
GameObject highlightedObj = CreateRendererHighlighter("MarkAsHighlighted", highlightersContainer, renderer, 
    new Color(0.5f, 0.7f, 1f, 0.05f));  // ← CHANGE THIS

// MarkAsReachable - for movement range (yellow)
GameObject reachableObj = CreateRendererHighlighter("MarkAsReachable", highlightersContainer, renderer, 
    new Color(1f, 0.9f, 0.4f, 0.05f));  // ← CHANGE THIS

// MarkAsPath - for movement path (green)
GameObject pathObj = CreateRendererHighlighter("MarkAsPath", highlightersContainer, renderer, 
    new Color(0.3f, 1f, 0.3f, 0.05f));  // ← CHANGE THIS
```

**Important:** The alpha channel (4th value) is currently set to `0.05f` which makes them nearly invisible. This was intentional for testing.

#### To Make Highlights Visible:
```csharp
// Bright yellow for reachable tiles
new Color(1f, 0.9f, 0.4f, 1.0f)  // Solid yellow

// Bright green for path
new Color(0.3f, 1f, 0.3f, 1.0f)  // Solid green

// Light blue for hover
new Color(0.7f, 0.8f, 1f, 1.0f)  // Solid light blue
```

After changing, run: **Tools → Combat POC → Create Simple 3D Cell Prefab (Gray/Blue/Green)** to regenerate prefabs.

### Option 2: **Edit Existing Prefabs Manually** (Unity Inspector)

1. **Open** your cell prefab (e.g., `SimpleCubeCell_Gray.prefab`)
2. **Navigate** to: `Highlighters/MarkAsReachable` (or `MarkAsPath`, `MarkAsHighlighted`)
3. **Select** the GameObject
4. **Find** the `RendererHighlighter` component in Inspector
5. **Change** the `Color` field
   - **R (Red):** 0-1 (e.g., 1 = full red)
   - **G (Green):** 0-1
   - **B (Blue):** 0-1
   - **A (Alpha):** Should be **1.0** for fully opaque (0.05 makes it invisible!)
6. **Change** `Property Name` if needed:
   - Standard shader: `_Color`
   - URP Lit shader: `_BaseColor`

### Option 3: **Runtime Color Change** (Advanced)

If you need to change highlight colors at runtime:

```csharp
// Get the cell
var cell = cellManager.GetCellAt(coordinates) as TurnBasedStrategyFramework.Unity.Cells.Cell;

// Find the highlighter GameObject
var reachableHighlighter = cell.transform.Find("Highlighters/MarkAsReachable");
var rendererHighlighter = reachableHighlighter.GetComponent<RendererHighlighter>();

// Change the color
rendererHighlighter.SetColor(new Color(1f, 1f, 0f, 1f)); // Bright yellow
```

## Why Transparency Doesn't Work Like You Expect

The alpha channel in `RendererHighlighter` doesn't create a **transparent overlay** that blends with the underlying tile. Instead:

- **Alpha = 1.0:** The highlight color is **solid/opaque** and completely replaces the tile color
- **Alpha = 0.5:** The highlight color is **semi-transparent**, but it blends with the **background** (like the skybox), not the tile
- **Alpha = 0.05:** The highlight color is **nearly invisible** (which is why you can't see it)

### To Get a "Blended" Look:
You need to manually blend the colors yourself:

```csharp
// Example: Blend gray base (0.5, 0.5, 0.5) with yellow highlight (1, 0.9, 0.4)
Color baseGray = new Color(0.5f, 0.5f, 0.5f);
Color yellow = new Color(1f, 0.9f, 0.4f);

// 70% gray + 30% yellow
Color blended = Color.Lerp(baseGray, yellow, 0.3f);
// Result: Yellowish-gray (0.65, 0.62, 0.47)

// Use this blended color in the highlighter
GameObject reachableObj = CreateRendererHighlighter("MarkAsReachable", highlightersContainer, renderer, blended);
```

## Current Settings (Why Highlights Are Invisible)

Currently in `SimpleCubeCellCreator.cs`, all highlight colors have **alpha = 0.05f**:

```csharp
new Color(0.5f, 0.7f, 1f, 0.05f)   // Hover: Nearly invisible light blue
new Color(1f, 0.9f, 0.4f, 0.05f)   // Reachable: Nearly invisible yellow
new Color(0.3f, 1f, 0.3f, 0.05f)   // Path: Nearly invisible green
```

This makes them essentially invisible. Change to **1.0f** to make them fully visible.

## Recommended Settings for Visible Highlights

```csharp
// Hover (light blue)
new Color(0.7f, 0.8f, 1f, 1.0f)

// Reachable/Movement Range (bright yellow)
new Color(1f, 0.92f, 0.5f, 1.0f)

// Path (bright green)
new Color(0.4f, 1f, 0.4f, 1.0f)
```

Or for a "tinted" look that shows some of the original tile:

```csharp
// Tinted yellow (blend gray + yellow)
Color baseGray = new Color(0.5f, 0.5f, 0.5f);
Color yellow = new Color(1f, 0.9f, 0.4f);
Color tintedYellow = Color.Lerp(baseGray, yellow, 0.4f); // 40% yellow tint

GameObject reachableObj = CreateRendererHighlighter("MarkAsReachable", highlightersContainer, renderer, tintedYellow);
```

## Summary

✅ **What Works:**
- Material swap highlighting (the current system)
- Changing highlight colors via prefab or code
- Red borders for attackable enemies (separate system that works!)

❌ **What Doesn't Work:**
- Transparent overlays that blend with underlying tiles
- Making highlights "see-through" to show the base tile underneath
- Using alpha channel to create overlay effects

🔧 **To Fix Invisible Highlights:**
Change alpha from `0.05f` to `1.0f` in `SimpleCubeCellCreator.cs` lines 178, 181, 184, then regenerate prefabs.
