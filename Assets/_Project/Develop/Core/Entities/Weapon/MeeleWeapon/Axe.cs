using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class Axe : MeeleWeapon
{
    public Axe(string id,Rarity rarity) : base(id,rarity)
    {
        Stats[StatType.AttackSpeed].SetValue(0.75f * GameData.Rarity[Rarity]);
        Stats[StatType.AttackRange].SetValue(1 * GameData.Rarity[Rarity]);
        Stats[StatType.Damage].SetValue(15 * GameData.Rarity[Rarity]);
        Stats[StatType.StaminaCost].SetValue(25);
    }
}
