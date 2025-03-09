using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Project.Develop.Core;
using _Project.Develop.Core.Enum;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

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

    public void DropLoot()
    {
        for (int i = 0; i < 3; i++)
        {
            var container = ItemGenerator.Instance.GenerateRandomGameobject().GetComponent<Rigidbody>();
            container.transform.position = transform.position;
            var scale = container.transform.localScale;
            container.transform.localScale = scale * .2f;
            container.transform.DOScale(scale, 2.5f).SetEase(Ease.OutBack);
            container.transform.DOJump(container.transform.position + new Vector3(Random.Range(-1, 2), 0, Random.Range(-1, 2)), 2, 1, 1);
            //container.AddForce(Vector3.up * 1 + ),ForceMode.Force);
            container.AddTorque(new Vector3(Random.Range(-15,15),Random.Range(-15,15),Random.Range(-15,15)));
        }
    }
    
    public virtual void ModifyHP(float multiplyier)
    {
        skeletonModel[StatType.Health].SetValue(skeletonModel[StatType.Health].CurrentValue*multiplyier);
    }
    public virtual void ModifySpeed(float multiplyier)
    {
        skeletonModel[StatType.Agility].SetValue(skeletonModel[StatType.Agility].CurrentValue*multiplyier);
    }
    
    public virtual void ModifyDamage(float multiplyier)
    {
        skeletonModel[StatType.Strength].SetValue(skeletonModel[StatType.Strength].CurrentValue*multiplyier);
    }
}
