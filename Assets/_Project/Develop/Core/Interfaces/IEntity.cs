using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Enum;

public interface IEntity
{
    string Id { get; }
    Dictionary<StatType, Stat> Stats { get; }
    List<Effect> Effects { get; }
    void ApplyEffect(Effect effect);
    void Update(float deltaTime);
}