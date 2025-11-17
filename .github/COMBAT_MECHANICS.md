# Combat Mechanics Reference

Purpose: concise, unambiguous reference for AI and engineers describing core combat mechanics, deterministic rules, and implementation notes for this project. Keep this document as the single source-of-truth for combat rules.

---

## 1. High level summary
- Units have three armour pools: Piercing, Slashing, Bludgeoning (integer HP each).
- Units also have Life HP (integer).
- Each attack or ability has one or more damage types (piercing/slashing/bludgeoning).
- Life HP cannot be damaged until all three armour pools are reduced to 0, except for explicit armour-bypass effects (e.g., `Poison`).
- Abilities consume AP. Gear has finite uses per battle.
- Experience (integer) is awarded in points at the end of each battle and applies to all characters, whether alive or downed.
- Every 4 levels, characters gain a respec point (they start with 0), which they can use to respec all of their abilities.
- The main player characters are Mirashala, Bishep and Tharl. They all take their turns first at the start of the battle, then each enemy takes their turns. This uses the TBSF defaults that allows for selecting any allied character who has not taken their turn and then assigning movement/actions to them as the user sees fit.
- All 'support' upgrades to character abilities are passively applied to the defaults for the ability.

## 2. Deterministic damage resolution
When resolving an action that deals damage, follow these steps:

1. Determine the damage components of the action. Each component has a damage value and a damage type (piercing/slashing/bludgeoning). Multi-type abilities resolve as sequential components in a defined order: piercing first, then slashing, then bludgeoning.
2. Perform a single accuracy roll for an attack, based on the following defaults: 
  - Ground level tiles - 90% accuracy
  - Tier 1 raised tiles - 95% accuracy
  - Tier 2 raised tiles (and above) - 100% accuracy
  If the accuracy roll fails, then the attack misses — no damage applies. Accuracy can be modified by additive/subtractive penalties based on statuses, and this needs to be taken into account. So first consider the default accuracy, then look at the unit's statuses and apply any buffs or penalities as is appropriate.
3. Abilities have a 100% chance to hit, unless otherwise stated in the criteria for the abilities.
4. For each damage component (in order):
   - If the target has the `BrokenArmour` status (all armour pools == 0), apply the component damage to Life HP (clamped to 0).
   - Else if the component's type matches an armour pool with HP > 0, subtract the component damage from that armour pool (clamp to 0). If that subtraction causes any armour pool to reach 0, set `DamagedArmour` immediately (see below).
   - Else if the target already has `DamagedArmour` (one or more pools is 0), apply the component's base damage once to each remaining nonzero armour pool (clamp each to 0). If this causes all three armour pools to reach 0, set `BrokenArmour` immediately.
   - Components do NOT auto-spill leftover damage into other pools unless an ability explicitly states spillover or if the damage is a `CriticalHit`, which means it has done greater than 3 points of damage to the remaining HP pool of a particular armour type. If greater than 3 points of damage are done to an armour HP pool, then the spillover damage is applied to all other armour pools. It is a flat rate of 1 damage per remaining armour pools. If no armour pools remain, then it is a flat rate of 1 damage to the life HP pool. 

Notes:
- Clamping: all HP values are clamped at a minimum of 0.
- `DamagedArmour` and `BrokenArmour` are per-unit flags (not attacker-specific).

## 3. Status effects

### Debuffs
- Status effects are objects attached to units with duration and per-turn tick behavior.
- Tick order: run status ticks in a deterministic sequence at the start or end of a turn as defined by game rules (prefer: `TurnStarted` → statuses tick affecting unit before action).
- Statuses applied during a turn do not take effect until the start of the target's next turn (e.g., Poison damage or concussed behavior begins then, not immediately).
- `Burning`: each tick deals N damage (configurable) to a random nonzero pool (armour pools first, then life once armour is 0). Use injected RNG with seed for tests. The default burning damage is 2 damage to a random nonzero pool.
- `Poisoned`: bypasses armour and applies damage directly to Life HP. Lasts for 3 turns. Poison cannot reduce Life below 1 and there are no exceptions. By default, it applies 1 life HP damage per turn. 
- `Freezing`: accumulates percent stacks while unit stands in ice tiles; at thresholds apply movement/accuracy penalties and `Frozen` when ≥ 100%. `Freezing` lowers attack accuracy by 3%. Lowers movement range by 1 between 0 - 50% Freezing stack. And 2 lowers movement range by between 51% - 99% Freezing stack. A `Freezing` character can only escape the status if they can get to a neutral tile for 2 turns, at which point the `Freezing` status is removed. The current freezing stacks are not gradually removed per turn. They are collectively removed at the end of the 2 turns on a neutral tile. 
- `Frozen`: cannot move for 2 turns, or attacked by a fire-based attack.
- `Concussed`: randomly attacks friend or foe for 2 turns.
- `Panicked`: the unit runs randomly in any direction and cannot attack, use an ability or gear item.Lasts 2 turns.
- `Shocked`: -30% attack accuracy penalty for 2 turns.
- `Electrified`: can move but cannot attack, use gear or use an ability for 1 turn.
- `Lightning Rod`: lasts for 4 turns. Each turn, there's a 25% chance of being struck for 4 Piercing damage.
- `Lightning Cursed`: Lasts 3 turns. Each turn, there's a chance to apply one of 3 possible status effects (if the unit doesn't already have the status): 20% chance to apply `Shocked`, 20% chance to apply `Electrified`, 20% chance to apply `Concussed`. The 20% chance is calculated independently, such that all, some or none of the statuses may be applied in a given turn.
- `Addled`: Lasts 2 turns. The unit can only attack and move, but cannot use any of its abilities.
- `Pinned`: Cannot move for 3 turns, but can attack or use an ability (if the target is in range).
- `Enraged`: Forces the unit to attack the unit who bestowed `Enraged` upon them (the `Enraged` unit can still use abilities to attack the unit).
- `Clouded`: -20% attack accuracy penalty for 2 turns.
- `Grabbed`: Unit cannot move, attack, use gear or an ability while `Grabbed` lasts
- `Grabbing`: Unit cannot move, attack, use gear or an ability while `Grabbing` lasts

## 4. Flame & Ice tile interactions
- Flames are environmental tile effects that occupy cells and have duration and spread mechanics.
- On death-by-burning, convert the tile the unit upon to a Flame tile.
- Each Flame tile: duration default is 3 turns before reverting to a normal tile. At the start of each turn, a flame tile will spawn additional flame tiles in all directions adjacent to the current flame tile. This spread continues for each new Flame tile spawned during their next turns.  
- Flames apply `Burning` to units occupying the Flame tile at the start of their turn.
- Flame + Ice: when a Flame is placed on an Ice tile, both are removed and the tile returns to default state.
- Ice + Flame: when ice is cast over Flame tiles, both are removed and the tile returns to default state.

## 5. Tile Types

The battleground consists of various tile types that affect movement, combat, and environmental effects. The possible tile types are:

1. **Fire Tiles (Flame Tiles)**: Environmental effects that occupy cells and have duration and spread mechanics. On death-by-burning, convert the tile the unit was on to a Flame tile. Each Flame tile lasts for a default of 3 turns before reverting to a neutral tile. At the start of each turn, a flame tile spawns additional flame tiles in all adjacent directions. Flames apply `Burning` to units occupying the tile at the start of their turn. Flame tiles are removed if an Ice tile is placed on them, and vice versa.

2. **Ice Tiles**: Created by abilities like Ice Spirit. Units standing on ice tiles accumulate `Freezing` stacks each turn. Ice tiles last for 3 turns by default. Ice tiles are removed if a Flame tile is placed on them, and vice versa.

3. **Shadow Tiles**: Created by gear like Shadow Bomb. Last for 2 turns. Only the creator (e.g., Bishep) can enter shadow tiles; other units cannot. When the creator stands on a shadow tile, enemies attacking them have their accuracy reduced to 50%.

4. **Neutral Tiles**: Default tiles with no special effects. Used for movement and combat without penalties or bonuses. Freezing status can be escaped by staying on neutral tiles for 2 turns.

5. **Impassible Tiles**: Tiles that cannot be moved to by any unit. These block movement and line-of-sight for abilities and attacks.

## 6. Abilities, AP, Gear
- Abilities require AP to cast (a per-ability cost). If a unit has insufficient AP, the ability cannot be used. AP resets to default at the end of a battle. There is a flat rate of 1 AP cost per ability that's used. 
- Gear items have per-battle uses; decrement on use. Gear item uses reset to defaults at the end of a battle.
- End-of-battle: restore all units' AP to the default per character; restore all gear uses to the default per character; and revive downed allies to full armour and life HP.

## 7. Victory & rewards
- Default victory condition: all enemy units must be dead (units with Life HP == 0 and not allied).
- End-of-battle rewards: XP, items (as configured), and restore per-battle resources. Downed allies are revived to full armour and life HP.

## 8. Determinism & testability
- Use small, dependency-injected services for core logic (DamageResolver, StatusService, FlameManager). These should be pure C# classes with no Unity scene dependency where possible to allow fast EditMode tests.
- Inject RNG (seedable) for tests to make burning/flame spread deterministic.

## 9. Level Thresholds

Players start at level 1 with 2 AUPs to spend on abilities.

AUPs are 'ability upgrade points' that are spent on upgrading abilities.

Level 2: reached at 100 exp
Level 3: reached at 300 exp
Level 4: reached at 600 exp
Level 5: reached at 1000 exp
Level 6: reached at 1200 exp
Level 7: reached at 1600 exp
Level 8: reached at 2000 exp

## 10. Main Player Characters

Mirashala, Bishep and Tharl are the main player characters. They are the only main player characters throughout the game and all 3 appear in each battle. 

### Mirashala
- Life: 3
- Armour: Piercing 1, Slashing 3, Bludgeoning 1
- Attack: Piercing 1, range 4
- AP: 2
- Gear:
  - Health Potion x1 — heals 2 Life HP (can only target self)
  - Heroic Potion x1 — restores 1 AP (can only target self)
  - Healing Dust x1 - activates a targeted healing well on the map 

- Abilities (overview):
  
  - **Fire Spirit** (unlock cost: 2 AUP): applies `Burning` to one enemy for 3 turns (burn tick = 2 damage to a random nonzero pool). If any armour HPs are still greater than 0, these have a 50% higher chance of being damaged than the life HP pool. On death-by-burning spawn a `Flame` tile that spreads and applies `Burning`. 
    
    - **Fire Spirit Support Upgrades**
      
      - *Explosion (1 AUP per level)*: add an AOE that increases AoE radius per level. All units (whether enemy or ally) in the AOE will receive the `Burning` status. The AOE is adjustable via the UI, such that the AOE can be downgraded or upgraded according to the available AOE levels.

        - Level 1: 1 cell AOE, applying `Burning` to all enemies in the AOE.
        - Level 2: 2 cell AOE, applying `Burning` to all enemies in the AOE.
        - Level 3: 3 cell AOE, applying `Burning` to all enemies in the AOE.

      - *Wildfire (1 AUP per level)*: adds a percentage chance to apply the `Panicked` status to the unit.

        - Level 1: 20% chance to apply `Panicked`.
        - Level 2: 30% chance to apply `Panicked`.
        - Level 3: 45% chance to apply `Panicked`.
      
      - *Furnace (1 AUP per level)*: 50% chance to add extra random damage to any HP type, determined by level. If multiple enemies are caught in an AOE, then the chance to do additional damage is calculated on an individual basis per enemy

        - Level 1: 50% chance to add + 1 additional extra random damage to any HP type
        - Level 2: 50% chance to add + 2 additional extra random damage to any HP type
        - Level 3: 50% chance to add + 3 additional extra random damage to any HP type
      
  - **Ice Spirit** (unlock cost: 2 AUP): creates a 3-cell radius ice tile area for 3 turns; applies `Freezing` stacks. All units (whether enemy or ally) in the AOE will receive the `Freezing` status.
    
    - **Ice Spirit Support Upgrades**
      
      - *Deep Freeze (1 AUP per level)*: Applies a greater Freezing stack % per level.  

        - Level 1: Freezing now stacks at 35% per turn.
        - Level 2: Freezing now stacks at 45% per turn.
        - Level 3: Freezing now stacks at 60% per turn.
      
      - *Creeping Frost (1 AUP per level)*: Extends the AOE of ice tiles. All units (whether enemy or ally) in the AOE will receive the `Freezing` status. The AOE is adjustable via the UI, such that the AOE can be downgraded or upgraded according to the available AOE levels.

        - Level 1: creates a 4-cell radius ice tile area for 3 turns
        - Level 2: creates a 5-cell radius ice tile area for 3 turns
        - Level 3: creates a 6-cell radius ice tile area for 3 turns
            
      - *Frozen Fingers (1 AUP per level)*: Increases the attack accuracy penalty of the `Freezing` status per level.

        - Level 1: changes `Freezing` accuracy penalty to -6% penalty 
        - Level 2: changes `Freezing` accuracy penalty to -12% penalty 
        - Level 3: changes `Freezing` accuracy penalty to -18% penalty (and additionally applies the penalty to ability accuracy)
      
  - **Lightning Spirit** (unlock cost: 2 AUP): single-target ability that applies the `Lightning Cursed` status.  

    - **Lightning Spirit Support Upgrades**

      - *Lightning Rod (1 AUP per level)*: Adds the `Lightning Rod` status as an additional possible status to the `Lightning Cursed` list of status effects. As with the other `Lightning Cursed` statuses, there's a default 20% chance for `Lightning Rod` to be applied per turn of `Lightning Cursed`.

        - Level 1: increases the chance of `Lightning Rod` from 20% to 25%
        - Level 2: increases the chance of `Lightning Rod` from 20% to 30%
        - Level 3: increases the chance of `Lightning Rod` from 20% to 35% (and increases `Lightning Rod` damage from 4 Piercing to 5 Piercing)

      - *Volatile (1 AUP per level)*: Increases the % chance of each default status for `Lightning Cursed` (with the exemption of `Lightning Rod` if it was applied via the `Lightning Rod` ability).

        - Level 1: changes the % chance of each default status for `Lightning Cursed` to 25% 
        - Level 2: changes the % chance of each default status for `Lightning Cursed` to 30% 
        - Level 3: changes the % chance of each default status for `Lightning Cursed` to 35% (and adds a new status to `Lightning Cursed` list of status effects: 35% chance to apply the `Addled` status) 

      - *Spark (1 AUP per level)*: Adds AOE to Lightning Spirit, with the radius based on the level. All units (whether enemy or ally) in the AOE will receive the `Lightning Cursed` status. The AOE is adjustable via the UI, such that the AOE can be downgraded or upgraded according to the available AOE levels.

        - Level 1: creates a 1-cell radius for `Lightning Rod`
        - Level 2: creates a 2-cell radius for `Lightning Rod`
        - Level 3: creates a 3-cell radius for `Lightning Rod`

### Tharl
- Life: 3
- Armour: Piercing 2, Slashing 1, Bludgeoning 3
- AP: 2
- Attack: Piercing 1, range 4
- Gear:
  - Health Potion x1 — heals 2 Life HP (can only target self)
  - Heroic Potion x1 — restores 1 AP (can only target self)
  - Sporesmith Key x1 - activates a targeted sporesmith on the map 

- Abilities (overview):

  - **Root Turret** (unlock cost: 2 AUP): spawns a stationary turret (a summoned unit) that has no Life HP (dies when its armour is depleted). Default turret stats: Armour Piercing 1, Slashing 1, Bludgeoning 1; attack range 4; attack = 1 Piercing damage. Turret acts as an additional player unit. It begins its turn after the enemy has made their turn following the spawn of the turret.

    - **Root Turret Support Upgrades**

      - *Root Harpoon (1 AUP per level)*: adds a chance to apply `Pinned` status.

        - Level 1: 25% chance to apply the `Pinned` status
        - Level 2: 30% chance to apply the `Pinned` status
        - Level 3: 35% chance to apply the `Pinned` status (and increases the default 1 Piercing damage to 3, whether or not `Pinned` was applied)

      - *Root Vision (1 AUP per level)*: increases turret range.

        - Level 1: attack range is now 5
        - Level 2: attack range is now 6
        - Level 3: attack range is now 7

      - *Root Endurance (1 AUP per level)*: increases turret armour pools.

        - Level 1: Armour is now Piercing 2, Slashing 2, Bludgeoning 1
        - Level 2: Armour is now Piercing 3, Slashing 3, Bludgeoning 1
        - Level 3: Armour is now Piercing 4, Slashing 4, Bludgeoning 1

  - **Spore Blast** (unlock cost: 2 AUP): has a default range of 5 and augments Tharl's Piercing attack (+1 Piercing) and adds a 20% chance for extra 1 Slashing and 1 Bludgeoning damage (both are applied if the roll is successful).

    - **Spore Support Upgrades**

      - *Spore Eruption (1 AUP per level)*: adds AoE radius to the spore effect (can cause friendly fire when enabled). The AOE is adjustable via the UI, such that the AOE can be downgraded or upgraded according to the available AOE levels.

        - Level 1: 2 cell AOE.
        - Level 2: 3 cell AOE.
        - Level 3: 4 cell AOE.

      - *Volatile Spore (1 AUP per level)*: increases chance to amplify additional Slashing and Bludgeoning damage.

        - Level 1: now a 25% chance for extra 1 Slashing and 1 Bludgeoning damage.
        - Level 2: now a 30% chance for extra 1 Slashing and 1 Bludgeoning damage.
        - Level 3: now a 35% chance for extra 2 Slashing and 2 Bludgeoning damage (the Slashing/Bludgeoning bonus is also increased at this level).

      - *Homing Spore (1 AUP per level)*: spawns a controllable new player unit (no Life HP, low armour) that can be guided and then detonated; stats scale with levels. Homing spore only spawns the homing missile if the user chooses not to directly attack an enemy with Spore Blast.

        - Level 1: movement 3, armour 1/1/1, damage 1 Piercing on detonation.
        - Level 2: movement 4, armour 1/1/1, damage 2 Piercing on detonation.
        - Level 3: movement 6, armour 1/1/1, damage 3 Piercing on detonation. Detonates using Spore Eruption radius if unlocked.
  
  - **Root Ram** (unlock cost: 2 AUP): summons a durable ram unit (no Life HP, armour pools: Piercing 1, Slashing 1, Bludgeoning 2) with movement 4 and a headbutt attack (2 Bludgeoning, range 1). The ram has 1 AP for its abilities (if any are available).
    
    - **Root Ram Support Upgrades**
      
      - *Dash Attack (1 AUP per level)*: charges for up a max of 3 cells in a line (player can choose how far it charges), hitting all units, friendly or otherwise, for Bludgeoning damage.

        - Level 1: 1 Bludgeoning to units in the line of fire.
        - Level 2: 2 Bludgeoning to units in the line of fire.
        - Level 3: 3 Bludgeoning to units in the line of fire.
      
      - *Taunt (1 AUP per level)*: % chance to apply `Enraged` to a enemies in a given radius, forcing them to target the ram.

        - Level 1: 70% chance in 2-cell radius.
        - Level 2: 80% chance in 3-cell radius.
        - Level 3: 90% chance in 4-cell radius + 40% chance to also add the `Clouded` status (this is calculated per enemy in the radius, rather than globally).

      - *Tough Skin (1 AUP per level)*: increases Root Ram armour pools.

        - Level 1: Armour is now Piercing 1, Slashing 1, Bludgeoning 2
        - Level 2: Armour is now Piercing 1, Slashing 2, Bludgeoning 2
        - Level 3: Armour is now Piercing 2, Slashing 2, Bludgeoning 3

### Bishep
- Life: 4
- Armour: Piercing 2, Slashing 2, Bludgeoning 2
- AP: 2
- Attack types (player-selectable per attack):
  - Mace: 1 Bludgeoning, range 1
  - Sword: 1 Slashing, range 1
  - Throwing Blade: 1 Piercing, range 3
- Gear:
  - Health Potion x1 — heals 2 Life HP (can only target self)
  - Heroic Potion x1 — restores 1 AP (can only target self)
  - Shadow Bomb. Quantity: 2. Range of 4 cells. Can only be thrown at a free neutral ground cell (unoccupied by a unit and isn't a fire or ice tile). Creates a shadow cell on the cell where it lands that lasts 2 turns. Only Bishep can enter shadow cells - no other unit, ally or enemy, can enter these cells. When Bishep stands on a shadow cell, all enemies who attack Bishep have their accuracy reduced to 50%.

- Abilities (overview):

  - **Assassinate** (unlock cost: 2 AUP): enhanced Throwing Blade (range 5, base damage +1). Has a 5% base crit chance that deals additional +1 damage.

    - **Assassinate Support Upgrades**

      - *Deadly (1 AUP per level)*: increase the default crit chance and critical hit damage.

        - Level 1: critical chance increases to 10% and damage bonus increases to +2.
        - Level 2: critical chance increases to 15% and critical bonus increases to +3.
        - Level 3: critical chance increases to 20% and critical bonus increases to +4.

      - *Envenomed (1 AUP per level)*: applies `Poisoned` status.

        - Level 1: poison deals the default 1 Life damage per turn for the `Poisoned` status.
        - Level 2: poison deals 2 Life damage per turn.
        - Level 3: poison deals 3 Life damage per turn.

      - *Spore Eruption (1 AUP per level)*: adds AoE radius to Assassinate (can cause friendly fire when enabled). The AOE is adjustable via the UI, such that the AOE can be downgraded or upgraded according to the available AOE levels.

        - Level 1: 2 cell AOE.
        - Level 2: 3 cell AOE.
        - Level 3: 4 cell AOE.

  - **Vortex** (unlock cost: 2 AUP): Mace attack that knocks a target back by 3 cells and draws nearby enemies into a 2-cell radius vortex. This attack never affects allied units. All affected enemies move 2 cells closer to the target enemy at the centre of the vortex. If they collide with the central enemy, it causes an additional 1 Bludgeoning damage to both the central enemy and the enemy who collided with them.
    
    - **Vortex Support Upgrades**
      
      - *Brutal Energies (1 AUP per level)*: adds guaranteed bonus damage to all enemies within the vortex AOE. 
      
        - Level 1: enemies pulled into the vortex take +1 additional damage to whatever other damage they may accumulate due to vortex rules
        - Level 2: enemies pulled into the vortex take +2 additional damage to whatever other damage they may accumulate due to vortex rules
        - Level 3: enemies pulled into the vortex take +3 additional damage to whatever other damage they may accumulate due to vortex rules

      - *Mind Rend (1 AUP per level)*: chance to inflict `Concussion` to all enemies within the vortex AOE. 
      
        - Level 1: 20% chance to inflict `Concussion`
        - level 2: 30% chance to inflict `Concussion`
        - Level 3: 40% chance to inflict `Concussion`
            
      - *Toxic Core (1 AUP per level)* chance to inflict `Poison` to all enemies within the vortex AOE.  
      
        - Level 1: 20% chance to inflict `Poisoned`
        - level 2: 30% chance to inflict `Poisoned`
        - Level 3: 40% chance to inflict `Poisoned`

  - **Shadow Axis** (unlock cost: 2 AUP): sword attack that creates a T-shaped blast that is 1 cell to the left, right and top of the original target. It deals 2 Slashing damage to the original target and 1 Slashing damage to each enemy caught within the T-shaped blast.

    - **Shadow Axis Support Upgrades**

      - *Deathly Materials (1 AUP per level)*: repairs armour for each enemy caught in the T-blast (but does not repair it for the target enemy).

        - Level 1: Restores 1 Piercing armour HP per enemy caught in the T-blast
        - Level 2: Restores 1 Piercing and 1 Bludgeoning armour HP per enemy caught in the T-blast
        - Level 3: Restores 1 Piercing, 1 Bludgeoning and 1 Slashing armour HP per enemy caught in the T-blast

      - *Devastate (1 AUP per level)*: increases the number of cells affected by the T-shaped blast.

        - Level 1: T-shaped blast that is 2 cells to the left, right and top of the original target
        - Level 2: T-shaped blast that is 3 cells to the left, right and top of the original target
        - Level 3: T-shaped blast that is 4 cells to the left, right and top of the original target

      - *Devastate (1 AUP per level)*: adds a chance to inflict `Clouded` on both the original target and anyone caught in the T-shaped blast.

        - Level 1: 20% chance to inflict `Clouded` on both the original target and anyone caught in the T-shaped blast.
        - Level 2: 30% chance to inflict `Clouded` on both the original target and anyone caught in the T-shaped blast.
        - Level 3: 40% chance to inflict `Clouded` on both the original target and anyone caught in the T-shaped blast.

## 10. Implementation notes (TBSF extension points)
- Extend `Unit` (or project-specific unit wrappers) to add armour pools, Life HP, AP, gear counters, and status lists.
- Implement `DamageResolver` service and call it from IAbility / Action execution code paths.
- Use the framework's Turn events (`TurnStarted`, `TurnEnded`, `UnitDestroyed`, `GameEnded`) to tick statuses, spread flames, and restore resources.
- Use `RegularCellManager` to query neighbors and manage flame placement.
- If needed, create a `FlameManager` MonoBehaviour to own flame propagation logic and run it on the appropriate turn event.

## Enemies

The player character will be referred to as the player character for this section. The enemy is the enemy. An enemy will always attack or use debuff abilities on a player character, unless under a status which causes them to randomly attack player or enemy.

### General Enemy AI

General enemy AI uses the API and script format used by the TBSF for enemy AI. However it is custom for this game and should not use any of the default behaviour scripts provided by TBSF.

- It should remain stationary unless a player character is within a 7-cell radius of the enemy. Then it will seek to get as close to the enemy as it can get before it can trigger an attack or an ability.
- Unless otherwise stated, assume enemies will also try to use their powerful abilities first. Like the player character, the enemies use up 1 AP per ability.
- Where possible, it should seek prioritise a player character whose armour is broken, where they only have life HP remaining
- The next priority would be a player character who is weak to their attack or ability type (as in, their armour against that ability or attack is currently lower than the other armour HPs the player character possesses)
- The next priority is a player character whose armour HP against the enemy's attack type is 0, therefore will now take global armour damage from the enemy's attacks 
- If the tiles are ice or fire tiles, the enemy will make sure to walk around them.
- If an enemy is caught in an ice or fire tile, it will prioritise moving out of the fire or ice tiles, always in the direction of the closest player character
- If however a player character is also caught in the fire or ice tiles and is within attack range, then the enemy will not move out of the fire or ice tiles and will attack the player character (as long the enemy's current HP means it will not die by staying on a fire tile)
- If the path to a player is blocked by obstacles but can move within attack or ability range of a player, it will prioritise doing do
- If the path to a player is blocked by obstacles and the enemy cannot attack any player characters from behind the obstacles, the enemy will not try to move towards the obstacles blocking its path
- If the path to a player is blocked by obstacles, the enemy will check the range attacks and abilities of all player characters and it will move one cell out of range so as not to be hit from behind the obstacles

### Infected Groctopod Grabber

- Life: 2
- Armour: Piercing 2, Slashing 2, Bludgeoning 1
- AP: 2
- Attack: 1 Slashing, range 1

Abilities:

- *Groctopod Grab*: Has a range of 4. If used, it will apply the `Grabbed` status to the player unit that was targetted. Doing so will also apply the `Grabbing` status to the Groctopod Grabber who triggered the ability. This means they cannot move or attack while grabbing a player character.

Groctopod Grabber AI:

- If there are any non-Groctopod Grabber enemy units in 7-cell radius, the Groctopod Grabber will prioritise using the Groctopod Grab, as these enemy units can then attack the player while the other enemy attacks the player
- If there are no enemies within a 7-cell radius of the Groctopod Grabber, it will prioritise attacking the player with its slash attack
- If there are no enemies within a 7-cell radius of the Groctopod Grabber, and it has already triggered a Groctopod Grab, it will cancel the Groctopod Grab on its next turn (a free action) and it will prioritise attacking the player with its slash attack

### Infected Medusa Lamprey  

- Life: 2
- Armour: Piercing 2, Slashing 2, Bludgeoning 2
- AP: 2
- Attack: 1 Slashing, range 1

Abilities:

- *Spike Belch*: Has a range of 3. Does 1 Piercing damage and has a 30% chance to apply the `Addled` status.

Infected Medusa Lamprey AI:

- Prioritises using its Spike Belch ability until it runs out of AP. 
- If the player character is adjacent to the Infected Medusa Lamprey and it can still use a Spike Belch, it will move as far away as possible from the player until it can use its Spike Belch

## Structures and Obstacles  

Structures and obstacles cannot move. Structures and obstacles have no life HP, but do have defined armour HPs, unless they are a sporesmith or a healing well, both of which are invulnerable.

Structures are considered player units, in the sense they have a turn where they can use their ability once, then their turn ends.

They are controlled by the player while they still have AP during the player phase.

If structures have not been activated, or have no AP left, they should not be targeted by the game for a turn, since they can't do anything.

### Structures 

#### Spore Spitter

- Armour: Piercing 2, Slashing 2, Bludgeoning 1
- Attack: 1 Piercing, range 5

Spore Spitter AI:

- Attacks any player character in range 

#### Sporesmith 

- Invulnerable 
- Has no attack
- Can only be activated by Tharl's Sporesmith Key
- AP: 2

Sporesmith abilities:

- *Armour Repair*: Range is 3 cells. Single Target. Repairs 1 Piercing and 1 Slashing HP

Sporesmith upgrades (TBD):

- These will upgrade the Sporesmith in various ways, but are not yet decided.

#### Healing Well

- Invulnerable 
- Has no attack
- Can only be activated by Mirashala's Healing Dust
- AP: 2

Healing Well abilities:

- *Heal*: Range is 3 cells. Single target. Repairs 1 life HP.

Healing Well upgrades (TBD):

- These will upgrade the Healing Well in various ways, but are not yet decided.

### Obstacles

Obstacles are immune to certain tile transformations, such that the neutral tile they inhabit remains neutral until that obstacle is destroyed. 

#### Root Mound

- Armour: Piercing 1, Slashing 1, Bludgeoning 1
- If a fire tile is spawned on a tile that contains a root mound, the bush is immediately destroyed
- immune to ice and shadow tiles

#### Rock

- Armour: Piercing 2, Slashing 2, Bludgeoning 1
- Immune to fire, shadow and ice tiles

#### Bush

- Armour: Piercing 1, Slashing 1, Bludgeoning 1
- If a fire tile is spawned on a tile that contains a bush, the bush is immediately destroyed
- immune to ice and shadow tiles

# Combat UI

This describes the UIs for an interfaces relevant to the turn based combat arenas. Where applicable, it will use the UIs available within the TBSF. For all other UI scenarios, it will use the Unity UI Toolkit to create the UI. 

Player characters go first. They can be selected in any order to move or take actions. The 'end turn' button is presented at the bottom of the screen to end their turn at any moment.

Both player an enemy units can move and then attack, or attack immediately.

Using an attack or ability automatically ends the player or enemy's turn.

Enemies take their turn when all player characters have ended theirs.

## Battleground Units UI

All units on the battleground have their HP shown above their model. If any statuses have been applied to the unit, then these are presented as a row of circular icons above the unit. If the unit is clicked, it opens the `Unit Interaction UI`, described later. It also shows how far the unit can move (in blue highlight) and how far the unit can attack (in green highlight).

## Unit Interaction UI

This is a simple menu displayed to the side of the unit. The menu options will change depending on whether the unit is an ally or an enemy.

### Ally Menu Options

1. Inspect: opens the `Unit Stats UI` described later.
2. Heal Life: only if the ally is in movement range of the current unit and the current unit has this ability.
3. Repair Slashing Armour: only if the ally is in movement range of the current unit and the current unit has this ability.
4. Repair Piercing Armour: only if the ally is in movement range of the current unit and the current unit has this ability.
5. Repair Bludgeoning Armour: only if the ally is in movement range of the current unit and the current unit has this ability.
6. Cure Negative Status: only if the ally is in movement range of the current unit and the current unit has this ability.

### Enemy Menu Options

1. Inspect: opens the `Unit Stats UI` described later.
2. Attack: attacks the unit, but only if the enemy is in attack range of the active unit. 
3. Ability Options: these are a list of any abilities that the character currently possesses (as long as they have remaining AP to use them). The appearance of the ability as a menu item must also respect the ability's range: it should not appear if it is not possible to use the ability relative to the current position of the player.

## Unit Stats UI

This appears at the right side of the screen and is locked to the right side of the screen. It has a close button in the top right corner which will close the `Unit Stats UI` if clicked. The `Unit Stats UI` displays the following sections relevant to unit stats:

1. General: armour HP, life HP, attack, movement and range
2. Statuses: Any buffs or debuffs applied to the unit
2. Gear: any gear the unit possesses and the remaining uses
3. Abilities: Any abilities the unit has and what they do

Clicking on another unit will close the `Unit Stats UI`.

Clicking the same unit again will close the `Unit Stats UI`.

## Movement UI

Uses the default TBSF to indicate movement range.

## Ability AOE UI

Should use the TBSF API to indicate the AOE of the ability (if applicable). This should show when the user hovers over the AOE-applicable ability in the `Unit Interaction UI`.


