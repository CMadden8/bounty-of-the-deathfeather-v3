using System;
using System.Collections.Generic;

namespace BountyOfTheDeathfeather.CombatSystem
{
    /// <summary>
    /// Simple, editable templates for POC units (heroes & basic enemies).
    /// These are intentionally minimal: just the numeric stats used by the POC (HP, AP, MP, attack/defence/range).
    /// </summary>
    public static class UnitTemplates
    {
        public class Template
        {
            public int MaxLifeHP { get; set; }
            public int ActionPoints { get; set; }
            public int MovementPoints { get; set; }
            public int AttackRange { get; set; }
            public int AttackFactor { get; set; }
            public int DefenceFactor { get; set; }
            public ArmourPools Armour { get; set; }
        }

        public static Template GetTemplate(string name)
        {
            switch ((name ?? string.Empty).ToLowerInvariant())
            {
                case "tharl":
                    return Tharl();
                case "bishep":
                    return Bishep();
                case "mirashala":
                    return Mirashala();
                case "groctopod":
                    return Groctopod();
                case "medusa":
                    return Medusa();
                default:
                    return null;
            }
        }

        // Hero: Tharl - per COMBAT_MECHANICS.md
        public static Template Tharl() => new Template
        {
            MaxLifeHP = 3,
            ActionPoints = 1,
            MovementPoints = 5,
            AttackRange = 4,
            AttackFactor = 1,
            DefenceFactor = 0,
            Armour = new ArmourPools(piercing: 2, slashing: 1, bludgeoning: 3)
        };

        // Hero: Bishep - per COMBAT_MECHANICS.md
        public static Template Bishep() => new Template
        {
            MaxLifeHP = 4,
            ActionPoints = 1,
            MovementPoints = 4,
            AttackRange = 1,
            AttackFactor = 1,
            DefenceFactor = 0,
            Armour = new ArmourPools(piercing: 2, slashing: 2, bludgeoning: 2)
        };

        // Hero: Mirashala - per COMBAT_MECHANICS.md
        public static Template Mirashala() => new Template
        {
            MaxLifeHP = 3,
            ActionPoints = 1,
            MovementPoints = 4,
            AttackRange = 4,
            AttackFactor = 1,
            DefenceFactor = 0,
            Armour = new ArmourPools(piercing: 1, slashing: 3, bludgeoning: 1)
        };

        // Enemy: Infected Groctopod Grabber - per COMBAT_MECHANICS.md
        public static Template Groctopod() => new Template
        {
            MaxLifeHP = 2,
            ActionPoints = 1,
            MovementPoints = 4,
            AttackRange = 1,
            AttackFactor = 1,
            DefenceFactor = 0,
            Armour = new ArmourPools(piercing: 2, slashing: 2, bludgeoning: 1)
        };

        // Enemy: Infected Medusa Lamprey - per COMBAT_MECHANICS.md
        public static Template Medusa() => new Template
        {
            MaxLifeHP = 2,
            ActionPoints = 1,
            MovementPoints = 4,
            AttackRange = 1,
            AttackFactor = 1,
            DefenceFactor = 0,
            Armour = new ArmourPools(piercing: 2, slashing: 2, bludgeoning: 2)
        };
    }
}
