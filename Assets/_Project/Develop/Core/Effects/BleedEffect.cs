using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Enum;
using UnityEngine;

namespace _Project.Develop.Core.Effects
{
    public class BleedEffect : PeriodicEffect
    {
        private float bleedDamageMultiplier;

        public BleedEffect(float duration, float interval, float bleedDamageMultiplier = 0.2f) 
            : base(duration, interval, 0f) // damagePerTick будет вычисляться
        {
            this.bleedDamageMultiplier = bleedDamageMultiplier;
            Name = "Bleed";
        }

        public override void OnApply(IEntity target)
        {
            // Урон зависит от текущего здоровья цели или нанесённого урона (здесь упрощённо от здоровья)
            float initialDamage = target.Stats[StatType.Health].CurrentValue * bleedDamageMultiplier;
            magnitude = initialDamage / (Duration / interval); // Распределяем урон по тикам
            base.OnApply(target);
        }

        protected override void ApplyTick(IEntity target)
        {
            Debug.Log("Bleed Tick");
            target.Stats[StatType.Health].Modify(-magnitude);
        }

        public override Effect Clone()
        {
            return new BleedEffect(startDuration, interval, bleedDamageMultiplier);
        }
    }
}