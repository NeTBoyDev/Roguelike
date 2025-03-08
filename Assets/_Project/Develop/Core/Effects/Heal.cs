using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Enum;

public class Heal : PeriodicEffect
{
    public Heal(float magnitude, float duration, float interval) 
        : base(duration,interval,magnitude) {Name = "Heal"; }

    

    protected override void ApplyTick(IEntity target)
    {
        target.Stats[StatType.Health].Modify(magnitude);
    }
    
    public override Effect Clone()
    {
        return new Heal(magnitude,Duration,interval);
    }
}