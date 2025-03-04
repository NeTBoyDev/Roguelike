using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Enum;
using UnityEngine;

namespace _Project.Develop.Core.Effects
{
    public class PoisonEffect : PeriodicEffect
    {
        private float duration;
        private float interval;
        private float damagePerTick;

        public PoisonEffect(float duration, float interval, float damagePerTick) : base(EffectType.Poison,duration,interval,damagePerTick)
        {
            this.duration = duration;
            this.interval = interval;
            this.damagePerTick = damagePerTick;
            
        }

        public override void OnApply(IEntity target)
        {
            //target.ApplyEffect(new PoisonEffect(duration, interval, damagePerTick)); // Применяем дебафф к врагу
            
            base.OnApply(target);
        }

        protected override void ApplyTick(IEntity target)
        {
            Debug.Log("Tick");
            target.Stats[StatType.Health].Modify(-damagePerTick);
        }

        public override Effect Clone()
        {
            return new PoisonEffect(startDuration, interval, damagePerTick);
        }
    }
}