using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TurnBasedStrategyFramework.Unity.Controllers;
using TurnBasedStrategyFramework.Common.Controllers.GridStates;
using TurnBasedStrategyFramework.Common.Units;
using TurnBasedStrategyFramework.Common.Units.Abilities;
using TurnBasedStrategyFramework.Unity.Units.Abilities;
using TurnBasedStrategyFramework.Unity.Units;
using TurnBasedStrategyFramework.Common.Cells;
using BountyOfTheDeathfeather.CombatSystem.Commands;

namespace BountyOfTheDeathfeather.CombatSystem.UI
{
    /// <summary>
    /// Combat action footer UI displaying unit resources (MP/AP/TP/Life/Armour/Gear), abilities, and action buttons.
    /// Aligned with COMBAT_MECHANICS.md: AP for turn-scoped actions, TP (Talent Points) for ability unlocks, gear with uses.
    /// </summary>
    public class CombatActionFooterUI : MonoBehaviour
    {
        [SerializeField] private UnityGridController _gridController;
        
        private GridState _lastState;
        private bool _isMyCustomState;
        private IUnit _currentUnit;
        private Unit _currentUnityUnit; // Unity Unit reference for TBSF properties
        private CombatIdentity _currentIdentity;
        private List<IAbility> _allAbilities = new List<IAbility>();
        
        // Track the currently active ability (for UI highlighting)
        private IAbility _activeAbility;
        
        // Attack Mode State
        private bool _isAttackMode;
        
        // Cursor feedback state
        private IUnit _hoveredEnemy;
        private bool _showAttackCursor;
        
        // Attack range tracking
        private HashSet<ICell> _attackRangeCells = new HashSet<ICell>();
        private HashSet<IUnit> _attackableEnemies = new HashSet<IUnit>();
        private Dictionary<IUnit, UnitSelectionBorder> _attackableBorders = new Dictionary<IUnit, UnitSelectionBorder>();

        private void Start()
        {
            if (_gridController == null)
                _gridController = FindFirstObjectByType<UnityGridController>();

            if (_gridController != null)
                {
                    var unitManager = _gridController.UnitManager;
                    if (unitManager != null)
                    {
                        unitManager.UnitAdded += OnUnitAdded;
                        // Handle existing units
                        var units = unitManager.GetUnits();
                        if (units != null)
                        {
                            foreach (var unit in units)
                            {
                                OnUnitAdded(unit);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[CombatActionFooterUI] UnityGridController has no UnitManager assigned.", this);
                    }
                }
        }

        private void OnUnitAdded(IUnit unit)
        {
            // Don't subscribe to UnitSelected event - we'll detect selection via GridState changes
        }

        private void Update()
        {
            if (_gridController == null) return;

            var currentState = _gridController.GridState;

            // Detect state change to clear UI if we leave selection
            if (currentState != _lastState)
            {
                _lastState = currentState;
                if (!(currentState is GridStateUnitSelected))
                {
                    _currentUnit = null;
                    _allAbilities.Clear();
                    _activeAbility = null;
                    DeactivateAttackMode();
                }
            }

            // Check for enemy hover when attack ability is active
            UpdateEnemyHoverFeedback();

            // FORCE RED HIGHLIGHTS every frame if in attack mode
            // Show transparent overlay on attackable tiles AND red borders on enemies
            if (_isAttackMode)
            {
                if (_attackRangeCells.Count > 0)
                {
                    foreach (var cell in _attackRangeCells)
                    {
                        _gridController.CellManager.SetColor(cell, 1f, 0f, 0f, 0.3f); // Red, transparent
                    }
                }
                
                // Ensure enemy borders stay visible
                foreach (var kvp in _attackableBorders)
                {
                    if (kvp.Value != null && !kvp.Value.IsVisible)
                    {
                        kvp.Value.ShowAttackable();
                    }
                }
            }

            // Handle Attack Mode Clicks (Intercept before GridState if clicking an enemy)
            if (_isAttackMode && Input.GetMouseButtonDown(0))
            {
                HandleAttackModeClick();
            }

            // Handle new selection logic
            if (currentState is GridStateUnitSelected selectedState)
            {
                // Skip if this is a state we just created
                if (_isMyCustomState)
                {
                    _isMyCustomState = false;
                    return;
                }

                // Get the selected unit from the state via reflection
                var stateType = selectedState.GetType();
                var unitField = stateType.GetField("_selectedUnit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var selectedUnit = unitField?.GetValue(selectedState) as IUnit;

                // New unit selected - process it
                if (selectedUnit != null && selectedUnit != _currentUnit)
                {
                    _currentUnit = selectedUnit;
                    _currentUnityUnit = selectedUnit as Unit;
                    _currentIdentity = (_currentUnityUnit as MonoBehaviour)?.GetComponent<CombatIdentity>();
                    _allAbilities = _currentUnit.GetBaseAbilities().ToList();
                    
                    Debug.Log($"[CombatActionFooterUI] Unit selected: {_currentUnit.GetType().Name}, abilities: {_allAbilities.Count}");
                    
                    // Default to movement (MoveAbility active)
                    ActivateDefaultAbilities();
                }
                // Otherwise keep the current unit active (UI stays visible)
            }
        }

        private void ActivateDefaultAbilities()
        {
            if (_currentUnit == null) return;

            var defaults = _allAbilities.Where(IsDefaultAbility).ToList();
            
            _isMyCustomState = true;
            _gridController.GridState = new GridStateUnitSelected(_currentUnit, defaults);
            _activeAbility = defaults.FirstOrDefault(a => a is MoveAbility); // Just for UI highlight
            DeactivateAttackMode();
        }

        private void ActivateAbility(IAbility ability)
        {
            if (_currentUnit == null) return;

            // Special handling for Attack Ability to preserve movement highlights
            // We toggle Attack Mode but do NOT change the GridState (keeping MoveAbility active)
            if (ability is AttackAbility || ability.GetType().Name.Contains("Attack"))
            {
                if (_isAttackMode)
                {
                    DeactivateAttackMode();
                }
                else
                {
                    _isAttackMode = true;
                    _activeAbility = ability; // For UI highlight
                    DisplayAttackRange();
                }
                return;
            }

            // For other abilities, switch state normally
            DeactivateAttackMode();
            
            _isMyCustomState = true;
            _gridController.GridState = new GridStateUnitSelected(_currentUnit, ability);
            _activeAbility = ability;
        }

        private void DeactivateAttackMode()
        {
            _isAttackMode = false;
            
            // Clear the red highlights and enemy borders
            ClearAttackRangeDisplay();

            // If active ability was attack, clear it
            if (_activeAbility is AttackAbility || (_activeAbility != null && _activeAbility.GetType().Name.Contains("Attack")))
            {
                _activeAbility = null;
                // Restore MoveAbility as active UI element if we are in default state
                var defaults = _allAbilities.Where(IsDefaultAbility).ToList();
                var moveAbility = defaults.FirstOrDefault(a => a is MoveAbility);
                if (moveAbility != null) 
                {
                    _activeAbility = moveAbility;
                    // Restore movement highlights explicitly
                    moveAbility.Display(_gridController);
                }
            }
        }

        private void HandleAttackModeClick()
        {
            // Raycast to find clicked unit
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                var unitComponent = hit.collider.GetComponent<Unit>();
                if (unitComponent != null && _attackableEnemies.Contains(unitComponent))
                {
                    // Execute Attack
                    var attackAbility = _allAbilities.FirstOrDefault(a => a is AttackAbility || a.GetType().Name.Contains("Attack"));
                    if (attackAbility != null)
                    {
                        // Execute the command manually
                        var command = new CombatAttackCommand(unitComponent, 1); // 1 AP
                        command.Execute(_currentUnit, _gridController);
                        
                        // Consume AP visually (TurnResolver should handle logic)
                        DeactivateAttackMode();
                    }
                }
            }
        }

        private bool IsDefaultAbility(IAbility ability)
        {
            // Exclude Attack abilities from ability loop - we have a dedicated Attack button
            return ability is MoveAbility || 
                   ability is AttackAbility || 
                   ability is AttackRangeHighlightAbility ||
                   ability.GetType().Name.Contains("Attack");
        }

        private void OnGUI()
        {
            if (_currentUnit == null) return;

            float screenHeight = Screen.height;
            float screenWidth = Screen.width;
            
            // Responsive scaling
            float scaleFactor = Mathf.Clamp(screenHeight / 1080f, 0.75f, 2.0f);
            
            float footerHeight = 100f * scaleFactor;
            float buttonWidth = 120f * scaleFactor;
            float buttonHeight = 35f * scaleFactor;
            float spacing = 8f * scaleFactor;
            int fontSize = Mathf.RoundToInt(11f * scaleFactor);
            int labelFontSize = Mathf.RoundToInt(10f * scaleFactor);

            // Draw background
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = fontSize;
            GUI.Box(new Rect(0, screenHeight - footerHeight, screenWidth, footerHeight), "", boxStyle);

            float currentX = spacing;
            float topY = screenHeight - footerHeight + spacing;
            float bottomY = screenHeight - footerHeight + 50f * scaleFactor;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = fontSize;
            
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = labelFontSize;
            labelStyle.normal.textColor = Color.white;

            // ===== SECTION 1: RESOURCES (Top Row) =====
            
            // Movement Points
            if (_currentUnityUnit != null)
            {
                string mpText = $"MP: {Mathf.FloorToInt(_currentUnityUnit.MovementPoints)}/{Mathf.FloorToInt(_currentUnityUnit.MaxMovementPoints)}";
                GUI.Label(new Rect(currentX, topY, 80f * scaleFactor, buttonHeight), mpText, labelStyle);
                currentX += 85f * scaleFactor;
            }

            // Action Points (TBSF)
            if (_currentUnityUnit != null)
            {
                string apText = $"AP: {Mathf.FloorToInt(_currentUnityUnit.ActionPoints)}/{Mathf.FloorToInt(_currentUnityUnit.MaxActionPoints)}";
                GUI.Label(new Rect(currentX, topY, 80f * scaleFactor, buttonHeight), apText, labelStyle);
                currentX += 85f * scaleFactor;
            }

            // Talent Points (TP - per-combat from COMBAT_MECHANICS.md, used to unlock abilities)
            if (_currentIdentity != null)
            {
                string tpText = $"TP: {_currentIdentity.TalentPoints}/{_currentIdentity.MaxTalentPoints}";
                GUI.Label(new Rect(currentX, topY, 80f * scaleFactor, buttonHeight), tpText, labelStyle);
                currentX += 85f * scaleFactor;
            }

            // Gear Points (GP - per-turn resource for using gear, resets each turn)
            if (_currentIdentity != null)
            {
                string gpText = $"GP: {_currentIdentity.GearPoints}/{_currentIdentity.MaxGearPoints}";
                GUI.Label(new Rect(currentX, topY, 80f * scaleFactor, buttonHeight), gpText, labelStyle);
                currentX += 85f * scaleFactor;
            }

            // Life HP (N/A if unit has no life HP)
            if (_currentIdentity != null)
            {
                string lifeText = _currentIdentity.MaxLifeHP > 0 
                    ? $"Life: {_currentIdentity.LifeHP}/{_currentIdentity.MaxLifeHP}"
                    : "Life: N/A";
                GUI.Label(new Rect(currentX, topY, 90f * scaleFactor, buttonHeight), lifeText, labelStyle);
                currentX += 95f * scaleFactor;
            }

            // Armour HP (Piercing/Slashing/Bludgeoning)
            if (_currentIdentity != null)
            {
                string armourText = $"P:{_currentIdentity.ArmourPiercing} S:{_currentIdentity.ArmourSlashing} B:{_currentIdentity.ArmourBludgeoning}";
                GUI.Label(new Rect(currentX, topY, 120f * scaleFactor, buttonHeight), armourText, labelStyle);
                currentX += 125f * scaleFactor;
            }

            // Gear Display
            if (_currentIdentity != null && _currentIdentity.Gear != null)
            {
                foreach (var gear in _currentIdentity.Gear)
                {
                    if (gear == null || string.IsNullOrEmpty(gear.Name)) continue;
                    string gearText = $"{gear.Name}: {gear.RemainingUses}/{gear.MaxUses}";
                    float gearWidth = 120f * scaleFactor;
                    GUI.Label(new Rect(currentX, topY, gearWidth, buttonHeight), gearText, labelStyle);
                    currentX += gearWidth + spacing;
                }
            }

            // ===== SECTION 2: ACTION BUTTONS (Bottom Row) =====
            currentX = spacing;

            // Attack Button
            var attackAbility = _allAbilities.FirstOrDefault(a => a is AttackAbility || a.GetType().Name.Contains("Attack"));
            if (attackAbility != null)
            {
                bool isAttackActive = _activeAbility == attackAbility;
                if (isAttackActive)
                {
                    GUI.color = new Color(1f, 0.5f, 0.2f); // Orange highlight
                }

                if (GUI.Button(new Rect(currentX, bottomY, buttonWidth, buttonHeight), "Attack", buttonStyle))
                {
                    ActivateAbility(attackAbility);
                }
                GUI.color = Color.white;
                currentX += buttonWidth + spacing;
            }

            // Custom Ability Buttons (non-default, non-move/attack)
            foreach (var ability in _allAbilities)
            {
                if (IsDefaultAbility(ability)) continue;

                string name = ability.GetType().Name.Replace("Combat", "").Replace("Command", "").Replace("Ability", "");
                name = System.Text.RegularExpressions.Regex.Replace(name, "(\\B[A-Z])", " $1").Trim();
                
                // TODO: Check if ability is unlocked (requires TP investment tracking)
                // For now, show all abilities - unlock system will be separate UI
                
                bool isActive = _activeAbility == ability;
                if (isActive)
                {
                    GUI.color = Color.cyan;
                }

                if (GUI.Button(new Rect(currentX, bottomY, buttonWidth, buttonHeight), name, buttonStyle))
                {
                    ActivateAbility(ability);
                }
                GUI.color = Color.white;
                currentX += buttonWidth + spacing;
            }

            // Use Gear Buttons
            if (_currentIdentity != null && _currentIdentity.Gear != null)
            {
                foreach (var gear in _currentIdentity.Gear)
                {
                    if (gear == null || string.IsNullOrEmpty(gear.Name) || gear.RemainingUses <= 0) continue;
                    
                    string gearButtonText = gear.Name;
                    float gearButtonWidth = 110f * scaleFactor;
                    
                    if (GUI.Button(new Rect(currentX, bottomY, gearButtonWidth, buttonHeight), gearButtonText, buttonStyle))
                    {
                        UseGearItem(gear.Name);
                    }
                    currentX += gearButtonWidth + spacing;
                }
            }

            // End Turn Button (Right aligned)
            float endTurnWidth = 100f * scaleFactor;
            if (GUI.Button(new Rect(screenWidth - endTurnWidth - spacing, bottomY, endTurnWidth, buttonHeight), "End Turn", buttonStyle))
            {
                _gridController.EndTurn();
            }

            // ===== CURSOR FEEDBACK =====
            if (_showAttackCursor)
            {
                GUIStyle cursorStyle = new GUIStyle(GUI.skin.label);
                cursorStyle.fontSize = Mathf.RoundToInt(16f * scaleFactor);
                cursorStyle.normal.textColor = Color.red;
                Vector3 mousePos = Input.mousePosition;
                mousePos.y = screenHeight - mousePos.y; // Flip Y
                GUI.Label(new Rect(mousePos.x + 15f, mousePos.y - 15f, 100f, 30f), "[ATTACK]", cursorStyle);
            }
        }

        private void UseGearItem(string gearName)
        {
            if (_currentIdentity == null) return;

            // Check if unit has GP available
            if (_currentIdentity.GearPoints <= 0)
            {
                Debug.LogWarning($"[CombatActionFooterUI] Cannot use {gearName}: no Gear Points remaining.");
                return;
            }

            if (_currentIdentity.UseGear(gearName))
            {
                // Consume 1 GP
                _currentIdentity.GearPoints--;
                Debug.Log($"[CombatActionFooterUI] Used {gearName}. Gear uses remaining: {_currentIdentity.GetGearUses(gearName)}, GP: {_currentIdentity.GearPoints}/{_currentIdentity.MaxGearPoints}");
                // TODO: Trigger gear effect (heal, restore AP, place shadow bomb, etc.)
            }
            else
            {
                Debug.LogWarning($"[CombatActionFooterUI] Failed to use {gearName}.");
            }
        }

        private void DisplayAttackRange()
        {
            ClearAttackRangeDisplay();

            if (_currentUnit == null || _currentUnityUnit == null || _currentIdentity == null) return;

            int attackRange = _currentIdentity.AttackRange;
            var currentCell = _currentUnit.CurrentCell;
            if (currentCell == null) return;

            // Get all cells within attack range
            var allCells = _gridController.CellManager.GetCells();
            _attackRangeCells = new HashSet<ICell>(allCells.Where(cell =>
                cell.GetDistance(currentCell) <= attackRange && cell != currentCell));

            // Highlight attack range cells with WHITE color (Transparent overlay)
            if (_attackRangeCells.Count > 0)
            {
                foreach (var cell in _attackRangeCells)
                {
                    // Use 0.01 alpha for almost completely invisible testing
                    _gridController.CellManager.SetColor(cell, 1f, 1f, 1f, 0.01f); 
                }
            }

            // Get attackable enemies and show red borders
            var enemyUnits = _gridController.UnitManager.GetEnemyUnits(_gridController.TurnContext.CurrentPlayer);
            _attackableEnemies = new HashSet<IUnit>(enemyUnits.Where(enemy =>
                enemy.CurrentCell != null &&
                _attackRangeCells.Contains(enemy.CurrentCell)));

            // Show red borders on attackable enemies by attaching border to the enemy's current cell (tile)
            _attackableBorders.Clear();
            foreach (var enemy in _attackableEnemies)
            {
                // Prefer placing the border on the cell GameObject so it aligns to the tile grid
                var cellMono = enemy.CurrentCell as MonoBehaviour;
                GameObject host = cellMono != null ? cellMono.gameObject : (enemy as MonoBehaviour)?.gameObject;
                if (host != null)
                {
                    var border = host.GetComponent<UnitSelectionBorder>();
                    if (border == null)
                    {
                        border = host.AddComponent<UnitSelectionBorder>();
                    }
                    if (border != null)
                    {
                        border.ShowAttackable();
                        _attackableBorders[enemy] = border;
                    }
                }
            }

            Debug.Log($"[CombatActionFooterUI] Cells in attack range: {_attackRangeCells.Count}, Attackable enemies: {_attackableEnemies.Count}");
        }

        private void ClearAttackRangeDisplay()
        {
            if (_attackRangeCells.Count > 0 && _gridController != null)
            {
                // UnMark to reset colors (this might clear movement highlights if they overlap, 
                // but it's the cleanest way to remove the red override)
                _gridController.CellManager.UnMark(_attackRangeCells);
                _attackRangeCells.Clear();
            }
            
            // Hide enemy borders
            foreach (var kvp in _attackableBorders)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Hide();
                }
            }
            _attackableBorders.Clear();
            
            _attackableEnemies.Clear();
            _showAttackCursor = false;
            _hoveredEnemy = null;
        }

        private void UpdateEnemyHoverFeedback()
        {
            // Only show attack cursor if attack mode is active
            if (!_isAttackMode || _attackableEnemies.Count == 0)
            {
                _showAttackCursor = false;
                _hoveredEnemy = null;
                return;
            }

            // Raycast to detect hovered enemy - only show cursor if enemy is in attackable set
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            _showAttackCursor = false;
            _hoveredEnemy = null;

            if (Physics.Raycast(ray, out hit))
            {
                // Check if we hit a unit GameObject
                var unitComponent = hit.collider.GetComponent<Unit>();
                if (unitComponent != null)
                {
                    IUnit hoveredUnit = unitComponent as IUnit;
                    // Only show cursor if this enemy is actually attackable (in range)
                    if (_attackableEnemies.Contains(hoveredUnit))
                    {
                        _showAttackCursor = true;
                        _hoveredEnemy = hoveredUnit;
                        return;
                    }
                }
            }
        }
    }
}
