using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class AIBase : MonoBehaviour
{
    private void Awake()
    {
        
        skeletonModel = new Creature("skeleton1");
    }

    public virtual void TakeDamage(float damage)
    {
        skeletonModel.Stats[StatType.Health].Modify(-damage);
        print($"Skeleton hp is {skeletonModel.Stats[StatType.Health].CurrentValue}");
    }
    public Creature skeletonModel { get; protected set; }
}
