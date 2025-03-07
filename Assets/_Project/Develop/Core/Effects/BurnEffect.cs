using System.Linq;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Enum;
using UnityEngine;

namespace _Project.Develop.Core.Effects
{
    public class BurnEffect : PeriodicEffect
    {
        private float damagePerTick;
        private float stackMultiplier;

        public BurnEffect(float duration, float interval, float damagePerTick, float stackMultiplier = 1.2f) 
            : base( duration, interval, damagePerTick)
        {
            this.damagePerTick = damagePerTick;
            this.stackMultiplier = stackMultiplier;
            Name = "Burn";
        }

        public override void OnApply(IEntity target)
        {
            if (target.Effects.Any(e => e is BurnEffect))
            {
                // Увеличиваем урон при наложении
                damagePerTick *= stackMultiplier;
            }
            base.OnApply(target);
        }

        protected override void ApplyTick(IEntity target)
        {
            if (target is AIBase ai)
            {
                Debug.Log("Burn Tick");
                ai.TakeDamage(damagePerTick);
            }
        }

        public override Effect Clone()
        {
            return new BurnEffect(startDuration, interval, damagePerTick, stackMultiplier);
        }
    }
}