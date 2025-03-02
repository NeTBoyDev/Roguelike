using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Enum;

namespace _Project.Develop.Core.Effects
{
    public class SlowEffect : ContinuousEffect
    {
        public SlowEffect(float magnitude, float duration = float.MaxValue) 
            : base(EffectType.Slow, magnitude, duration) { }

        public override void OnApply(IEntity target)
        {
            if (target.Stats.ContainsKey(StatType.Agility))
                target.Stats[StatType.Agility].Modify(-magnitude);
        }

        public override void OnRemove(IEntity target)
        {
            if (target.Stats.ContainsKey(StatType.Agility))
                target.Stats[StatType.Agility].Modify(magnitude);
        }
    }
}