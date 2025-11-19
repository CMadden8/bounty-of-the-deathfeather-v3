using System.Collections.Generic;
using System.Linq;
using TurnBasedStrategyFramework.Common.Controllers;
using TurnBasedStrategyFramework.Common.Units;
using TurnBasedStrategyFramework.Unity.Controllers;
using UnityEngine;

namespace BountyOfTheDeathfeather.CombatSystem.Managers
{
    /// <summary>
    /// Manages status effect ticks (Burning, Poisoned, etc.) at the start of each turn.
    /// Subscribes to UnityGridController.TurnStarted.
    /// </summary>
    public class CombatStatusManager : MonoBehaviour
    {
        [SerializeField] private UnityGridController _gridController;

        private DamageResolver _damageResolver;

        private void Awake()
        {
            if (_gridController == null)
            {
                _gridController = UnityEngine.Object.FindFirstObjectByType<UnityGridController>();
            }

            // Initialize DamageResolver with system RNG for now (could be seeded if needed)
            _damageResolver = new DamageResolver(new SystemRandomProvider());
        }

        private void OnEnable()
        {
            if (_gridController == null)
            {
                _gridController = FindFirstObjectByType<UnityGridController>();
            }
            if (_gridController != null)
            {
                _gridController.TurnStarted += OnTurnStarted;
            }
        }

        private void OnDisable()
        {
            if (_gridController != null)
            {
                _gridController.TurnStarted -= OnTurnStarted;
            }
        }

        private void OnTurnStarted(TurnTransitionParams turnParams)
        {
            var currentPlayer = turnParams.TurnContext.CurrentPlayer;
            if (currentPlayer == null) return;

            Debug.Log($"[CombatStatusManager] Processing status ticks for Player {currentPlayer.PlayerNumber}");

            // Get all units belonging to the current player
            var playerUnits = _gridController.UnitManager.GetUnits()
                .Where(u => u.PlayerNumber == currentPlayer.PlayerNumber)
                .ToList();

            foreach (var unit in playerUnits)
            {
                ProcessUnitStatuses(unit);
            }
        }

        private void ProcessUnitStatuses(IUnit unit)
        {
            var unityUnit = unit as TurnBasedStrategyFramework.Unity.Units.Unit;
            if (unityUnit == null) return;

            var identity = unityUnit.GetComponent<CombatIdentity>();
            if (identity == null) return;

            // Iterate backwards to allow removal
            for (int i = identity.Statuses.Count - 1; i >= 0; i--)
            {
                var status = identity.Statuses[i];

                // Apply tick effects
                switch (status.StatusId)
                {
                    case CombatStatusIds.Burning:
                        ApplyBurningTick(identity, unityUnit, status);
                        break;
                    case CombatStatusIds.Poisoned:
                        ApplyPoisonTick(identity, unityUnit, status);
                        break;
                    // Add other status logic here
                }

                // Decrement duration
                var nextStatus = status.DecrementDuration();
                
                if (nextStatus.IsExpired)
                {
                    Debug.Log($"[CombatStatusManager] Status {status.StatusId} expired on {unityUnit.name}");
                    identity.Statuses.RemoveAt(i);
                }
                else
                {
                    identity.Statuses[i] = nextStatus;
                }
            }
        }

        private void ApplyBurningTick(CombatIdentity identity, TurnBasedStrategyFramework.Unity.Units.Unit unit, StatusEffect status)
        {
            int damage = 2; // Default burning damage
            // Could read from status.Metadata if we stored custom damage there

            var stats = identity.ToUnitStats();
            var result = _damageResolver.ResolveBurningTick(stats, damage);

            ApplyDamageResult(identity, unit, result, "Burning");
        }

        private void ApplyPoisonTick(CombatIdentity identity, TurnBasedStrategyFramework.Unity.Units.Unit unit, StatusEffect status)
        {
            int damage = 1; // Default poison damage

            var stats = identity.ToUnitStats();
            var result = _damageResolver.ApplyDirectLifeDamage(stats, damage);

            ApplyDamageResult(identity, unit, result, "Poison");
        }

        private void ApplyDamageResult(CombatIdentity identity, TurnBasedStrategyFramework.Unity.Units.Unit unit, DamageResult result, string source)
        {
            if (result.TotalDamageToArmour > 0 || result.TotalDamageToLife > 0)
            {
                Debug.Log($"[CombatStatusManager] {source} dealt {result.TotalDamageToArmour} Armour Dmg, {result.TotalDamageToLife} Life Dmg to {unit.name}");
                
                // Update Identity
                identity.LifeHP = result.FinalStats.LifeHP;
                identity.ArmourPiercing = result.FinalStats.Armour.Piercing;
                identity.ArmourSlashing = result.FinalStats.Armour.Slashing;
                identity.ArmourBludgeoning = result.FinalStats.Armour.Bludgeoning;

                // Update Unity Unit
                unit.Health = result.FinalStats.LifeHP;

                // Trigger visual feedback (optional, via TBSF events if possible)
                unit.InvokeAttacked(new UnitAttackedEventArgs(unit, null, result.TotalDamageToArmour + result.TotalDamageToLife));

                if (result.WasKilled)
                {
                    Debug.Log($"[CombatStatusManager] {unit.name} died from {source}");
                    // Handle death (TBSF usually handles death when Health reaches 0 if we call OnUnitDestroyed or similar, 
                    // but setting Health=0 might be enough if the Unit checks it in Update or we trigger it manually)
                    // For now, we rely on the Unit's internal check or next interaction.
                    // Ideally we should call _gridController.UnitManager.DestroyUnit(unit);
                    // But we are iterating the list, so be careful.
                    // We'll leave it as Health=0 for now, TBSF often cleans up dead units.
                }
            }
        }
    }
}
