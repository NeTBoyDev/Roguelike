using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class SecondaryWeapon : Weapon
{
    public SecondaryWeapon(string id,Rarity rarity) : base(id,rarity)
    {
        Stats = new();
        Stats[StatType.StaminaCost] = new Stat(StatType.StaminaCost, 40 * GameData.Rarity[Rarity]);
    }
}
