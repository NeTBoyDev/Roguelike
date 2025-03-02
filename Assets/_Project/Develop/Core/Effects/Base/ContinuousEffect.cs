using _Project.Develop.Core.Base;
using _Project.Develop.Core.Enum;

namespace _Project.Develop.Core.Effects.Base
{
    public abstract class ContinuousEffect : Effect
    {
        protected float magnitude;

        public ContinuousEffect(EffectType type, float magnitude, float duration = float.MaxValue)
        {
            Type = type;
            this.magnitude = magnitude;
            Duration = duration;
        }
    }
}