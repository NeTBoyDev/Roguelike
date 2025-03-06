using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class Hammer : MeeleWeapon
{
    public Hammer(string id) : base(id)
    {
        Stats[StatType.AttackSpeed].SetValue(0.5f);
        Stats[StatType.AttackRange].SetValue(1.25f);
        Stats[StatType.Damage].SetValue(20);
    }
}
