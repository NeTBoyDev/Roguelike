using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Enum;

namespace _Project.Develop.Core.Effects
{
    public class PoisonEffect : PeriodicEffect
    {
        public PoisonEffect(float duration, float interval, float damagePerTick) 
            : base(EffectType.Poison, duration, interval, damagePerTick) { }

        protected override void ApplyTick(IEntity target)
        {
            if (target.Stats.ContainsKey(StatType.Health))
                target.Stats[StatType.Health].Modify(-magnitude);
        }
    }
}