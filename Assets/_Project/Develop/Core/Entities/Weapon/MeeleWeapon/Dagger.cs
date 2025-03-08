using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class Dagger : MeeleWeapon
{
    public Dagger(string id,Rarity rarity) : base(id,rarity)
    {
        Stats[StatType.AttackSpeed].SetValue(1.25f * GameData.Rarity[Rarity]);
        Stats[StatType.AttackRange].SetValue(0.5f * GameData.Rarity[Rarity]);
        Stats[StatType.StaminaCost].SetValue(13 * GameData.Rarity[Rarity]);
    }
}
