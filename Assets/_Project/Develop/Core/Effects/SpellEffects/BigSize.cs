using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Effects.Base;
using UnityEngine;

public class BigSize : SpellEffect
{
    public BigSize(float additionalShots = 1)
    {
        magnitude = additionalShots;
    }

    public override void Apply(GameObject target, ref List<GameObject> affectedObjects)
    {
        foreach (var projectile in affectedObjects)
        {
            projectile.transform.localScale *= magnitude;
        }
    }
    
}
