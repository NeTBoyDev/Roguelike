using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class Hammer : MeeleWeapon
{
    public Hammer(string id,Rarity rarity) : base(id,rarity)
    {
        Stats[StatType.AttackSpeed].SetValue(0.5f * GameData.Rarity[Rarity]);
        Stats[StatType.AttackRange].SetValue(1.25f * GameData.Rarity[Rarity]);
        Stats[StatType.Damage].SetValue(20 * GameData.Rarity[Rarity]);
        Stats[StatType.StaminaCost].SetValue(35 * GameData.Rarity[Rarity]);
    }
}
