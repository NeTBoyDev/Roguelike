using _Project.Develop.Core.Base;
using _Project.Develop.Core.Enum;

namespace _Project.Develop.Core.Effects.Base
{
    public abstract class PeriodicEffect : Effect
    {
        protected float interval;
        protected float timer;
        protected float magnitude;

        public PeriodicEffect(float duration, float interval, float magnitude) : base(duration)
        {
            Duration = duration;
            this.interval = interval;
            this.magnitude = magnitude;
            timer = interval;
        }

        public override void Update(IEntity target, float deltaTime)
        {
            base.Update(target, deltaTime);
            timer -= deltaTime;
            if (timer <= 0)
            {
                ApplyTick(target);
                timer = interval;
            }
        }

        protected abstract void ApplyTick(IEntity target);
    }
}