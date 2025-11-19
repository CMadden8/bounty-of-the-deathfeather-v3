using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TurnBasedStrategyFramework.Common.Controllers;
using TurnBasedStrategyFramework.Common.Units;
using TurnBasedStrategyFramework.Common.Units.Abilities;
using UnityEngine;

namespace BountyOfTheDeathfeather.CombatSystem.Commands
{
    /// <summary>
    /// Concrete attack command that integrates DamageResolver with TBSF.
    /// Reads CombatIdentity from attacker/defender, resolves damage via DamageResolver, applies result back to defender's CombatIdentity.
    /// </summary>
    public readonly struct CombatAttackCommand : ICommand
    {
        private readonly IUnit _target;
        private readonly int _actionCost;

        /// <summary>
        /// Creates a new combat attack command.
        /// </summary>
        /// <param name="target">The unit to attack.</param>
        /// <param name="actionCost">AP cost for this attack (default 1).</param>
        public CombatAttackCommand(IUnit target, int actionCost = 1)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _actionCost = actionCost;
        }

        /// <summary>
        /// Executes the attack: reads CombatIdentity from attacker/defender, resolves damage via DamageResolver,
        /// applies result to defender's CombatIdentity, handles death, decrements attacker AP.
        /// </summary>
        public async Task Execute(IUnit attacker, IGridController controller)
        {
            if (_target == null || attacker == null)
            {
                Debug.LogError("CombatAttackCommand: attacker or target is null");
                return;
            }

            // Step 1: Get Unity GameObjects from TBSF units
            var attackerUnit = attacker as TurnBasedStrategyFramework.Unity.Units.Unit;
            var defenderUnit = _target as TurnBasedStrategyFramework.Unity.Units.Unit;

            if (attackerUnit == null || defenderUnit == null)
            {
                Debug.LogError("CombatAttackCommand: attacker or target is not a Unity Unit MonoBehaviour");
                return;
            }

            // Step 2: Get CombatIdentity components
            var attackerIdentity = attackerUnit.GetComponent<CombatIdentity>();
            var defenderIdentity = defenderUnit.GetComponent<CombatIdentity>();

            if (attackerIdentity == null || defenderIdentity == null)
            {
                Debug.LogError($"CombatAttackCommand: missing CombatIdentity on {attackerUnit.name} or {defenderUnit.name}");
                return;
            }

            // Step 3: Build DamageContext
            var attackerStats = attackerIdentity.ToUnitStats();
            var defenderStats = defenderIdentity.ToUnitStats();

            var context = new DamageContext(attackerStats, defenderStats);

            // TODO: Add tile tier modifiers from attacker/defender cells
            // Example: context.AccuracyModifier = CalculateTileModifiers(attacker.CurrentCell, _target.CurrentCell);
            // For now, use default accuracy (no modifiers)

            // Step 4: Create damage component from attacker's AttackPower and PrimaryDamageType
            var damageComponent = new DamageComponent(attackerIdentity.PrimaryDamageType, attackerIdentity.AttackPower);
            var damageComponents = new List<DamageComponent> { damageComponent };

            // Step 5: Resolve damage via DamageResolver
            var resolver = new DamageResolver(new SystemRandomProvider());
            var result = resolver.ResolveAttack(damageComponents, context);

            // Step 6: Log result for debugging
            Debug.Log($"[Combat] {attackerUnit.name} attacks {defenderUnit.name}: " +
                      $"Hit={!result.WasKilled && result.TotalDamageToArmour + result.TotalDamageToLife > 0}, " +
                      $"ArmourDmg={result.TotalDamageToArmour}, LifeDmg={result.TotalDamageToLife}, " +
                      $"Killed={result.WasKilled}, Critical={context.IsCritical}");

            // Step 7: Apply result to defender's CombatIdentity
            defenderIdentity.LifeHP = result.FinalStats.LifeHP;
            defenderIdentity.ArmourPiercing = result.FinalStats.Armour.Piercing;
            defenderIdentity.ArmourSlashing = result.FinalStats.Armour.Slashing;
            defenderIdentity.ArmourBludgeoning = result.FinalStats.Armour.Bludgeoning;

            // Also update legacy TBSF Unit.Health field for compatibility
            defenderUnit.Health = result.FinalStats.LifeHP;

            // Step 8: Handle death if unit was killed
            if (result.WasKilled)
            {
                Debug.Log($"[Combat] {defenderUnit.name} was killed by {attackerUnit.name}");
                // TODO: Trigger death event, remove from UnitManager, play death animation
                // For now, just mark as dead by setting Health to 0
                defenderUnit.Health = 0;
            }

            // Step 9: Decrement attacker's ActionPoints
            attacker.ActionPoints -= _actionCost;

            // Step 10: Invoke TBSF events (for animations, UI updates)
            _target.InvokeAttacked(new TurnBasedStrategyFramework.Common.Units.UnitAttackedEventArgs(
                _target, attacker, result.TotalDamageToArmour + result.TotalDamageToLife));

            // Step 11: Trigger attack/defend animations
            await Task.WhenAll(
                controller.UnitManager.MarkAsAttacking(attacker, _target),
                controller.UnitManager.MarkAsDefending(_target, attacker)
            );
        }

        /// <summary>
        /// Undo is not fully implemented yet. Would need to store pre-attack state and revert.
        /// </summary>
        public Task Undo(IUnit attacker, IGridController controller)
        {
            // TODO: Implement undo by restoring pre-attack CombatIdentity state
            Debug.LogWarning("CombatAttackCommand.Undo not implemented");
            attacker.ActionPoints += _actionCost;
            return Task.CompletedTask;
        }

        private static class SerializationKeys
        {
            public const string TargetID = "target_id";
            public const string ActionCost = "action_cost";
        }

        public Dictionary<string, object> Serialize()
        {
            return new Dictionary<string, object>
            {
                { SerializationKeys.TargetID, _target.UnitID },
                { SerializationKeys.ActionCost, _actionCost }
            };
        }

        public ICommand Deserialize(Dictionary<string, object> actionParams, IGridController gridController)
        {
            var target = gridController.UnitManager.GetUnits().First(u =>
                u.UnitID == (int)actionParams[SerializationKeys.TargetID]);
            var actionCost = (int)actionParams[SerializationKeys.ActionCost];

            return new CombatAttackCommand(target, actionCost);
        }
    }
}
