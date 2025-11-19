# Tile Effects Implementation Guide

## Current Implementation (MVP)

The Combat POC currently uses **runtime color tinting** for tile effects (Fire/Ice/Shadow). This approach:

- Uses the existing `SimpleCubeGrey` prefab for all neutral ground tiles
- Applies runtime color changes via `CombatTileManager.UpdateVisuals()`:
  - **Flame tiles**: Red (`Color.red`)
  - **Ice tiles**: Cyan (`Color.cyan`)
  - **Shadow tiles**: Black (`Color.black`)
- Stores original colors and restores them when effects expire
- Changes material color directly on the renderer

### Advantages of Runtime Tinting (Current Approach)
- ✅ Fast prototyping - no need to create/manage multiple prefabs
- ✅ Single cell prefab to maintain for highlight behavior
- ✅ Easy to tweak colors in code
- ✅ Works with existing `SimpleCubeCellCreator` workflow
- ✅ No additional asset management overhead

### Disadvantages
- ❌ Less visually distinct than dedicated materials/textures
- ❌ Harder to add particle effects or unique visual features per tile type
- ❌ Color changes don't persist in prefab (runtime-only)

## Future Enhancement: Dedicated Tile Prefabs

For a more polished visual experience, you could create dedicated prefabs:

### Approach 1: Separate Prefabs (Fire/Ice/Shadow)
- Create `SimpleCubeFire`, `SimpleCubeIce`, `SimpleCubeShadow` prefabs
- Each uses a distinct material with:
  - Custom albedo/emission maps
  - Particle effects (flames, frost crystals, dark wisps)
  - Custom shaders (e.g., animated flame texture)
- `CombatTileManager` would instantiate/swap prefabs instead of tinting

**When to use:**
- When you want highly distinct visual identity for each effect
- When particle effects or animated materials are required
- Post-MVP polish phase

### Approach 2: Material Variants (Single Prefab)
- Keep `SimpleCubeGrey` as base
- Create material variants: `MatFire`, `MatIce`, `MatShadow`
- `CombatTileManager.UpdateVisuals()` swaps materials instead of tinting

**When to use:**
- Easier than managing 3+ prefabs
- Allows distinct materials without duplicating cell hierarchy
- Mid-level visual polish

## Recommendation for Current State

**Stick with runtime tinting for MVP** because:
1. No abilities currently spawn tile effects (Fire Spirit / Ice Spirit not yet used in gameplay)
2. Fast iteration is more valuable than visual polish right now
3. You can migrate to prefabs/materials later without breaking existing logic

## Migration Path (When Ready)

1. Create material variants for Fire/Ice/Shadow effects
2. Update `CombatTileManager.UpdateVisuals()` to swap materials:
   ```csharp
   [SerializeField] private Material _fireMaterial;
   [SerializeField] private Material _iceMaterial;
   [SerializeField] private Material _shadowMaterial;
   
   private void UpdateVisuals(ICell cell, TileEffectType type)
   {
       var renderer = (cell as Cell)?.GetComponentInChildren<Renderer>();
       if (renderer == null) return;
       
       switch (type)
       {
           case TileEffectType.Flame:
               renderer.material = _fireMaterial;
               break;
           case TileEffectType.Ice:
               renderer.material = _iceMaterial;
               break;
           // ...
       }
   }
   ```
3. Add particle effect prefabs and attach them to cells with active effects
4. Test and iterate

## References
- `CombatTileManager.cs` - Tile effect logic and visuals
- `SimpleCubeCellCreator.cs` - Editor tool for creating cell prefabs
- `.github/COMBAT_MECHANICS.md` - Tile effect rules (duration, spread, status application)
