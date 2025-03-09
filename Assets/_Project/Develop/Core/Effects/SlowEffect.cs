using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Enum;
using UnityEngine;

namespace _Project.Develop.Core.Effects
{
    public class SlowEffect : ContinuousEffect
    {
        public SlowEffect(float magnitude, float duration = 3)
            : base(magnitude, duration)
        {
            Name = "Slow";
        }

        public override void OnApply(IEntity target)
        {
            if (target.Stats.ContainsKey(StatType.Agility))
            {
                target.Stats[StatType.Agility].Modify(-Mathf.Clamp01(magnitude));
                target.Stats[StatType.Agility].SetValue(Mathf.Clamp01(target.Stats[StatType.Agility].CurrentValue));
            }
        }

        public override void OnRemove(IEntity target)
        {
            if (target.Stats.ContainsKey(StatType.Agility))
            {
                target.Stats[StatType.Agility].Modify(Mathf.Clamp01(magnitude));
                target.Stats[StatType.Agility].SetValue(Mathf.Clamp01(target.Stats[StatType.Agility].CurrentValue));
            }
            
        }

        public override Effect Clone()
        {
            return new SlowEffect(magnitude, Duration);
        }
    }
}