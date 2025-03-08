using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Enum;
using UnityEngine;

namespace _Project.Develop.Core.Effects
{
    public class HasteEffect : ContinuousEffect
    {
        public HasteEffect(float magnitude, float duration = float.MaxValue) 
            : base(magnitude, duration) {Name = "Haste"; }

        public override void OnApply(IEntity target)
        {
            Debug.Log("Haste");
            if (target.Stats.ContainsKey(StatType.Agility))
            {
                target.Stats[StatType.Agility].Modify(magnitude);
                Debug.Log(target.Stats[StatType.Agility].CurrentValue);
            }
                
        }

        public override void OnRemove(IEntity target)
        {
            if (target.Stats.ContainsKey(StatType.Agility))
                target.Stats[StatType.Agility].Modify(-magnitude);
        }

        public override Effect Clone()
        {
            return new HasteEffect(magnitude,Duration);
        }
    }
}