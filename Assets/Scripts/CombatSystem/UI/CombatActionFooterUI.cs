using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TurnBasedStrategyFramework.Unity.Controllers;
using TurnBasedStrategyFramework.Common.Controllers.GridStates;
using TurnBasedStrategyFramework.Common.Units;
using TurnBasedStrategyFramework.Common.Units.Abilities;
using TurnBasedStrategyFramework.Unity.Units.Abilities;

namespace BountyOfTheDeathfeather.CombatSystem.UI
{
    public class CombatActionFooterUI : MonoBehaviour
    {
        [SerializeField] private UnityGridController _gridController;
        
        private GridState _lastState;
        private bool _isMyCustomState;
        private IUnit _currentUnit;
        private IUnit _pendingUnit;
        private List<IAbility> _allAbilities = new List<IAbility>();
        
        // Track the currently active ability (for UI highlighting)
        private IAbility _activeAbility;

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
                }
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
                    _allAbilities = _currentUnit.GetBaseAbilities().ToList();
                    
                    Debug.Log($"[CombatActionFooterUI] Unit selected: {_currentUnit.GetType().Name}, abilities: {_allAbilities.Count}");
                    
                    // Default to Move/Attack
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
        }

        private void ActivateAbility(IAbility ability)
        {
            if (_currentUnit == null) return;

            _isMyCustomState = true;
            _gridController.GridState = new GridStateUnitSelected(_currentUnit, ability);
            _activeAbility = ability;
        }

        private bool IsDefaultAbility(IAbility ability)
        {
            return ability is MoveAbility || 
                   ability is AttackAbility || 
                   ability is AttackRangeHighlightAbility;
        }

        private void OnGUI()
        {
            if (_currentUnit == null || _allAbilities.Count == 0) return;

            float screenHeight = Screen.height;
            float screenWidth = Screen.width;
            
            // Responsive scaling based on screen height
            // Reference: 1080p (1920x1080) as baseline
            float scaleFactor = Mathf.Clamp(screenHeight / 1080f, 0.75f, 2.0f);
            
            float footerHeight = 80f * scaleFactor;
            float buttonWidth = 140f * scaleFactor;
            float buttonHeight = 40f * scaleFactor;
            float spacing = 10f * scaleFactor;
            int fontSize = Mathf.RoundToInt(12f * scaleFactor);

            // Draw background
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = fontSize;
            GUI.Box(new Rect(0, screenHeight - footerHeight, screenWidth, footerHeight), "Actions", boxStyle);

            float startX = 20f * scaleFactor;
            float startY = screenHeight - footerHeight + 20f * scaleFactor;

            // Create scaled button style
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = fontSize;

            // "Move / Attack" Button (Resets to defaults)
            if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "Move / Attack", buttonStyle))
            {
                ActivateDefaultAbilities();
            }
            startX += buttonWidth + spacing;

            // Ability Buttons
            foreach (var ability in _allAbilities)
            {
                if (IsDefaultAbility(ability)) continue;

                string name = ability.GetType().Name.Replace("Combat", "").Replace("Command", "").Replace("Ability", "");
                // Add spaces to CamelCase
                name = System.Text.RegularExpressions.Regex.Replace(name, "(\\B[A-Z])", " $1");

                // Highlight if active
                if (_activeAbility == ability)
                {
                    GUI.color = Color.green;
                }

                if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), name, buttonStyle))
                {
                    ActivateAbility(ability);
                }

                GUI.color = Color.white;
                startX += buttonWidth + spacing;
            }
            
            // End Turn Button (Right aligned)
            if (GUI.Button(new Rect(screenWidth - buttonWidth - 20f * scaleFactor, startY, buttonWidth, buttonHeight), "End Turn", buttonStyle))
            {
                _gridController.EndTurn();
            }
        }
    }
}
