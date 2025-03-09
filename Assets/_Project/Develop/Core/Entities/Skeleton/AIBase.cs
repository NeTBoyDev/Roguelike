using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Project.Develop.Core;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Enum;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class AIBase : MonoBehaviour
{
    private void Awake()
    {
        
        skeletonModel = new Creature("skeleton1");
        skeletonModel.Stats[StatType.DropCount] = new Stat(StatType.DropCount,1);
        skeletonModel.Stats[StatType.DropQuality] = new Stat(StatType.DropCount,1);
    }

    public virtual void TakeDamage(float damage)
    {
        skeletonModel.Stats[StatType.Health].Modify(-damage);
        print($"Skeleton hp is {skeletonModel.Stats[StatType.Health].CurrentValue}");
    }
    public Creature skeletonModel { get; protected set; }

    public void DropLoot()
    {
        for (int i = 1; i < 3 * skeletonModel[StatType.DropCount].CurrentValue; i++)
        {
            var value = Random.value;
            if (value < .1f)
            {
                Rarity rarity = CalculateRarity();
                    
                var container = ItemGenerator.Instance.GenerateRandomGameobject(rarity).GetComponent<Rigidbody>();
                container.transform.position = transform.position;
                var scale = container.transform.localScale;
                container.transform.localScale = scale * .2f;
                container.transform.DOScale(scale, 2.5f).SetEase(Ease.OutBack);
                container.transform.DOJump(container.transform.position + new Vector3(Random.Range(-1, 2), 0, Random.Range(-1, 2)), 2, 1, 1);
                //container.AddForce(Vector3.up * 1 + ),ForceMode.Force);
                container.AddTorque(new Vector3(Random.Range(-15,15),Random.Range(-15,15),Random.Range(-15,15)));
            }
        }    
    }

    protected Rarity CalculateRarity()
    {
        var rarityvalue = Random.value;
        Rarity rarity;
        if (rarityvalue < 0.5f)
            rarity = Rarity.Common;
        else if (rarityvalue < 0.6f)
            rarity = Rarity.Uncommon;
        else if (rarityvalue < 0.7f)
            rarity = Rarity.Rare;
        else if (rarityvalue < 0.8f)
            rarity = Rarity.Epic;
        else 
            rarity = Rarity.Legendary;
        return rarity;
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
    
    public virtual void ModifyDropCount(float multiplyier)
    {
        skeletonModel[StatType.DropCount].SetValue(skeletonModel[StatType.DropQuality].CurrentValue*multiplyier);
    }
    public virtual void ModifyDropQuality(float multiplyier)
    {
        skeletonModel[StatType.DropQuality].SetValue(skeletonModel[StatType.DropQuality].CurrentValue*multiplyier);
    }
}
