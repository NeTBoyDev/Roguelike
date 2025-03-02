using _Project.Develop.Core.Enum;
using UnityEngine;

namespace _Project.Develop.Core.Base
{
    public class Stat
    {
        public StatType Type { get; private set; }
        public float BaseValue { get; private set; }
        public float CurrentValue { get; private set; }
        public float MaxValue { get; private set; }

        public Stat(StatType type, float baseValue, float maxValue = float.MaxValue)
        {
            Type = type;
            BaseValue = baseValue;
            CurrentValue = baseValue;
            MaxValue = maxValue;
        }

        public void Modify(float value)
        {
            CurrentValue = Mathf.Clamp(CurrentValue + value, 0, MaxValue);
        }
    }
}