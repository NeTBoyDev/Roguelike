using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Effects.Base;
using UnityEngine;

public class ShotCount : SpellEffect
{
    public ShotCount(int additionalShots = 1)
    {
        magnitude = additionalShots;
    }

    public override void Apply(GameObject target, ref List<GameObject> affectedObjects)
    {
        List<GameObject> newObjects = new List<GameObject> { target }; 

        for (int i = 0; i < magnitude; i++)
        {
            var newShot = Object.Instantiate(target, target.transform.position + target.transform.forward * (i + 1) * 0.5f, target.transform.rotation);
            newObjects.Add(newShot);
        }

        affectedObjects = newObjects; 
    }
    
}
