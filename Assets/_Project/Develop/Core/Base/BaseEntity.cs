using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Enum;
using UnityEngine;

public abstract class BaseEntity : IEntity
{
    public string Id { get; private set; }
    public Dictionary<StatType, Stat> Stats { get; private set; }
    public List<Effect> Effects { get; private set; }

    protected BaseEntity(string id)
    {
        Id = id;
        Stats = new Dictionary<StatType, Stat>();
        Effects = new List<Effect>();
    }

    public virtual void ApplyEffect(Effect effect)
    {
        Effects.Add(effect); // УДАЛИТЬ ПРИ НЕОБХОДИМОСТИ
        effect.OnApply(this);
    }

    public virtual void AddEffect(Effect effect)
    {
        Effects.Add(effect);
    }

    public virtual void RemoveEffect(Effect effect)
    {
        Effects.Remove(effect);
    }
    
    public virtual void RemoveEffect(int index)
    {
        if (index < 0 || index >= Effects.Count)
            return;
        var effect = Effects[index];
        RemoveEffect(effect);
    }

    public virtual void Update(float deltaTime)
    {
        for (int i = Effects.Count - 1; i >= 0; i--)
        {
            Effects[i].Update(this, deltaTime);
            if (Effects[i].IsFinished)
            {
                Effects[i].OnRemove(this);
                Effects.RemoveAt(i);
            }
        }
    }

    public Stat this[StatType type]
    {
        get => Stats[type];
    }
}