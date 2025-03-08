using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Enum;

namespace _Project.Develop.Core.Effects
{
    public class Rage : ContinuousEffect
    {
        public Rage(float magnitude, float duration = float.MaxValue) 
            : base(magnitude, duration) {Name = "Rage"; }

        public override void OnApply(IEntity target)
        {
            if (target.Stats.ContainsKey(StatType.Strength))
                target.Stats[StatType.Strength].Modify(magnitude);
        }

        public override void OnRemove(IEntity target)
        {
            if (target.Stats.ContainsKey(StatType.Strength))
                target.Stats[StatType.Strength].Modify(-magnitude);
        }
        
        public override Effect Clone()
        {
            return new Rage(magnitude,Duration);
        }
    }
}