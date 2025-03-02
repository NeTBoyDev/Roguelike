using _Project.Develop.Core.Enum;

namespace _Project.Develop.Core.Base
{
    public abstract class Effect
    {
        public EffectType Type { get; protected set; }
        public float Duration { get; protected set; }
        public bool IsFinished => Duration <= 0 && Duration != float.MaxValue;

        public virtual void OnApply(IEntity target) { }
        public virtual void Update(IEntity target, float deltaTime)
        {
            if (Duration != float.MaxValue) // Постоянные эффекты имеют бесконечную длительность
                Duration -= deltaTime;
        }
        public virtual void OnRemove(IEntity target) { }
    }
}