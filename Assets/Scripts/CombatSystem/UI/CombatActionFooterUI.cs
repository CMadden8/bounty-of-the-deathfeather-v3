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
            unit.UnitSelected += OnUnitSelected;
        }

        private void OnUnitSelected(IUnit unit)
        {
            _pendingUnit = unit;
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
            if (currentState is GridStateUnitSelected)
            {
                // Skip if this is a state we just created
                if (_isMyCustomState)
                {
                    _isMyCustomState = false;
                    return;
                }

                // New unit selected - process it
                if (_pendingUnit != null)
                {
                    _currentUnit = _pendingUnit;
                    _pendingUnit = null;
                    
                    _allAbilities = _currentUnit.GetBaseAbilities().ToList();
                    
                    Debug.Log($"[CombatActionFooterUI] Unit selected: {_currentUnit}, abilities: {_allAbilities.Count}");
                    
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
            float footerHeight = 80f;
            float buttonWidth = 140f;
            float buttonHeight = 40f;
            float spacing = 10f;

            // Draw background
            GUI.Box(new Rect(0, screenHeight - footerHeight, screenWidth, footerHeight), "Actions");

            float startX = 20f;
            float startY = screenHeight - footerHeight + 20f;

            // "Move / Attack" Button (Resets to defaults)
            if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), "Move / Attack"))
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

                if (GUI.Button(new Rect(startX, startY, buttonWidth, buttonHeight), name))
                {
                    ActivateAbility(ability);
                }

                GUI.color = Color.white;
                startX += buttonWidth + spacing;
            }
            
            // End Turn Button (Right aligned)
            if (GUI.Button(new Rect(screenWidth - buttonWidth - 20f, startY, buttonWidth, buttonHeight), "End Turn"))
            {
                _gridController.EndTurn();
            }
        }
    }
}
