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
    /// Command for the Fire Spirit ability.
    /// Applies 'Burning' status to the target unit.
    /// </summary>
    public readonly struct CombatFireSpiritCommand : ICommand
    {
        private readonly IUnit _target;
        private readonly int _actionCost;

        public CombatFireSpiritCommand(IUnit target, int actionCost = 1)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _actionCost = actionCost;
        }

        public async Task Execute(IUnit attacker, IGridController controller)
        {
            if (_target == null || attacker == null) return;

            var attackerUnit = attacker as TurnBasedStrategyFramework.Unity.Units.Unit;
            var defenderUnit = _target as TurnBasedStrategyFramework.Unity.Units.Unit;

            if (attackerUnit == null || defenderUnit == null) return;

            var defenderIdentity = defenderUnit.GetComponent<CombatIdentity>();
            if (defenderIdentity == null)
            {
                Debug.LogError($"CombatFireSpiritCommand: missing CombatIdentity on {defenderUnit.name}");
                return;
            }

            // Apply Burning status
            // Duration 3 turns, default damage 2 (handled by status system later)
            var burningStatus = new StatusEffect(CombatStatusIds.Burning, stacks: 1, duration: 3);
            defenderIdentity.AddStatus(burningStatus);

            Debug.Log($"[Combat] {attackerUnit.name} cast Fire Spirit on {defenderUnit.name}. Applied Burning (3 turns).");

            // Decrement AP
            attacker.ActionPoints -= _actionCost;

            // Trigger animations/events
            await controller.UnitManager.MarkAsAttacking(attacker, _target);
            // Maybe play a specific spell effect here
        }

        public Task Undo(IUnit attacker, IGridController controller)
        {
            Debug.LogWarning("CombatFireSpiritCommand.Undo not implemented");
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

            return new CombatFireSpiritCommand(target, actionCost);
        }
    }
}
