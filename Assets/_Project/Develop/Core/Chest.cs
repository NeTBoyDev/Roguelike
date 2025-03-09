using System;
using _Project.Develop.Core;
using _Project.Develop.Core.Enum;
using _Project.Develop.Core.Player;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class Chest : MonoBehaviour
{
    public bool isKitStart;
    private Transform lid;
    public AudioClip open;
    private SoundManager manager;
    private void Awake()
    {
        manager = new SoundManager();
        lid = transform.GetChild(0);
    }

    private bool isOpened;

    public void Open()
    {
        if (isOpened)
            return;
        isOpened = true;
        if (isKitStart)
        {
            var container = ItemGenerator.Instance.GenerateWeaponGameobject(GameData._preset._startWeapon,Rarity.Common).GetComponent<Rigidbody>();
            container.transform.position = transform.position;
            var scale = container.transform.localScale;
            container.transform.localScale = scale * .2f;
            container.transform.DOScale(scale, 2.5f).SetEase(Ease.OutBack);
            container.transform.DOJump(new Vector3(Random.Range(-1, 2), 1, Random.Range(-1, 2)), 3, 1, 1).SetRelative();
            //container.AddForce(container.transform.up * 30,ForceMode.Force);
            //container.AddTorque(new Vector3(Random.Range(-15,15),Random.Range(-15,15),Random.Range(-15,15)));
        }
        else
        {
            for (int i = 1; i < Random.Range(2,4); i++)
            {
                var value = Random.value;
                Rarity rarity = CalculateRarity();
                    
                var container = ItemGenerator.Instance.GenerateRandomGameobject(rarity).GetComponent<Rigidbody>();
                container.transform.position = transform.position;
                var scale = container.transform.localScale;
                container.transform.localScale = scale * .2f;
                container.transform.DOScale(scale, 2.5f).SetEase(Ease.OutBack);
                container.transform.DOJump(new Vector3(Random.Range(-1, 2), 1, Random.Range(-1, 2)), 3, 1, 1).SetRelative();
                //container.AddForce(container.transform.up * 30,ForceMode.Force);
                //container.AddTorque(new Vector3(Random.Range(-15,15),Random.Range(-15,15),Random.Range(-15,15)));
            }    
        }

        lid.transform.DOLocalRotate(new Vector3(-110, 0, 0), .75f).SetEase(Ease.OutBounce).OnComplete(()=>transform.DOScale(Vector3.zero, 1).SetEase(Ease.InBack).OnComplete(() => Destroy(gameObject)));
        manager.ProduceSound(transform.position,open);
    }
    protected Rarity CalculateRarity()
    {
        float rarityvalue;
        if(isKitStart)
            rarityvalue = Random.value;
        else if(GameData._map == null)
        {
            rarityvalue = Random.value;
        }
        else
        {
            rarityvalue = Random.value + (1 -GameData._map[StatType.DropQuality].CurrentValue);
        }
        
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

   
}
