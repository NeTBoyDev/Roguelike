using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public RangeWeapon(string id) : base(id)
    {

        Stats[StatType.AttackSpeed].Modify(Effects.Count * Stats[StatType.AttackSpeed].CurrentValue);
    }
   
}
