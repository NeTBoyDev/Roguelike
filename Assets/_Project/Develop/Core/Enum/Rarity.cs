using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects;
using UnityEngine;

namespace _Project.Develop.Core.Enum
{
    public enum Rarity : int
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public static class GameData
    {
        public static Dictionary<Rarity, float> Rarity = new()
        {
            { Enum.Rarity.Common, 1 },
            { Enum.Rarity.Uncommon, 1.2f },
            { Enum.Rarity.Rare, 1.4f },
            { Enum.Rarity.Epic, 1.6f },
            { Enum.Rarity.Legendary, 2f },

        };

        private static List<Effect> Effects = new()
        {
            new HasteEffect(2),
            new PoisonEffect(5, 1, 5),
            new SlowEffect(3)
        };
        
        public static Effect GetRandomEffect()
        {
            return Effects[Random.Range(0, Effects.Count)];
        }

        public static StatPreset _preset;
        public static Map _map;

    }
}
