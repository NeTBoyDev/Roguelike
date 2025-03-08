using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Enum;

namespace _Project.Develop.Core.Effects
{
    public class Wisdom : ContinuousEffect
    {
        public Wisdom(float magnitude, float duration = float.MaxValue) 
            : base(magnitude, duration) {Name = "Wisdom"; }

        public override void OnApply(IEntity target)
        {
            if (target.Stats.ContainsKey(StatType.Intelligence))
                target.Stats[StatType.Intelligence].Modify(magnitude);
        }

        public override void OnRemove(IEntity target)
        {
            if (target.Stats.ContainsKey(StatType.Intelligence))
                target.Stats[StatType.Intelligence].Modify(-magnitude);
        }
        
        public override Effect Clone()
        {
            return new Wisdom(magnitude,Duration);
        }
    }
}