using System;
using _Project.Develop.Core.Enum;
using UnityEngine;

namespace _Project.Develop.Core.Base
{
    [Serializable]
    public class Stat
    {
        [field:SerializeField]public StatType Type { get; private set; }
        [field:SerializeField]public float BaseValue { get; private set; }
        [field:SerializeField]public float CurrentValue { get; private set; }
        [field:SerializeField]public float MaxValue { get; private set; }

        public Stat(StatType type, float baseValue, float maxValue = float.MaxValue)
        {
            Type = type;
            BaseValue = baseValue;
            CurrentValue = baseValue;
            MaxValue = maxValue;
        }

        public void Modify(float value)
        {
            //Debug.Log($"Before{CurrentValue}");
            CurrentValue = Mathf.Clamp(CurrentValue + value, 0, MaxValue);
            //Debug.Log($"After{CurrentValue}");
            OnModify?.Invoke(CurrentValue);
        }
        public void SetValue(float value)
        {
            //Debug.Log($"Before{CurrentValue}");
            CurrentValue = Mathf.Clamp(value, 0, MaxValue);
            OnModify?.Invoke(CurrentValue);
            //Debug.Log($"After{CurrentValue}");
        }
        
        public void SetValue(float value,float maxValue)
        {
            //Debug.Log($"Before{CurrentValue}");
            CurrentValue = Mathf.Clamp(value, 0, MaxValue);
            BaseValue = maxValue;
            MaxValue = maxValue;
            OnModify?.Invoke(CurrentValue);
            //Debug.Log($"After{CurrentValue}");
        }

        public event Action<float> OnModify;
    }
}