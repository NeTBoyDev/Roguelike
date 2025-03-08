using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Effects.SpellEffects;
using UnityEngine;

public class ShotCount : SpellEffect
{
    public ShotCount(int additionalShots = 1)
    {
        magnitude = additionalShots;
        Name = "Vertical multishot";
    }

    public override void Apply(ProjectileObject target, ref List<ProjectileObject> affectedObjects)
    {
        List<ProjectileObject> newObjects = new List<ProjectileObject> { target }; 

        for (int i = 0; i < magnitude; i++)
        {
            var newShot = Object.Instantiate(target, target.transform.position + target.transform.forward * (i + 1) * 0.5f, target.transform.rotation);
            newObjects.Add(newShot);
        }

        affectedObjects = newObjects; 
    }
    
    public override Effect Clone()
    {
        return new ShotCount((int)magnitude);
    }
    
}
