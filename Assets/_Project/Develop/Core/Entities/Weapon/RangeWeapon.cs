using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Project.Develop.Core;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Effects.SpellEffects;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class RangeWeapon : Weapon
{
    public bool isReloadable { get; protected set; }
    public bool isReloaded = false;
    public RangeWeapon(string id,Rarity rarity) : base(id,rarity)
    {
        Stats[StatType.RangeAttackSpeed] = new Stat(StatType.RangeAttackSpeed, 1f / GameData.Rarity[Rarity]);
        Stats[StatType.RangeAttackSpeed].Modify(Effects.Count * Stats[StatType.RangeAttackSpeed].CurrentValue);
        Stats[StatType.StaminaCost].SetValue(Stats[StatType.StaminaCost].CurrentValue * GameData.Rarity[Rarity]);
        //Stats[StatType.AttackSpeed].SetValue(Stats[StatType.AttackSpeed].CurrentValue / GameData.Rarity[Rarity]);
    }
   
}
