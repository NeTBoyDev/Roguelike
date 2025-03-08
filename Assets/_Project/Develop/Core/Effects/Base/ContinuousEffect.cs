using _Project.Develop.Core.Base;
using _Project.Develop.Core.Enum;

namespace _Project.Develop.Core.Effects.Base
{
    public abstract class ContinuousEffect : Effect
    {
        public float magnitude { get; protected set; }

        public ContinuousEffect(float magnitude, float duration = float.MaxValue) : base(duration)
        {
            this.magnitude = magnitude;
            Duration = duration;
        }
    }
}