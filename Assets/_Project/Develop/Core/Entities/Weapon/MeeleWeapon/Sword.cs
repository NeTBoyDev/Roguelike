using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class Sword : MeeleWeapon
{
    public Sword(string id,Rarity rarity) : base(id,rarity)
    {
        Stats[StatType.AttackSpeed].SetValue(GameData.Rarity[Rarity]);
    }
}
