using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class Axe : MeeleWeapon
{
    public Axe(string id) : base(id)
    {
        Stats[StatType.AttackSpeed].SetValue(0.75f);
        Stats[StatType.AttackRange].SetValue(1);
        Stats[StatType.Damage].SetValue(15);
        Stats[StatType.StaminaCost].SetValue(25);
    }
}
