using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class Dagger : MeeleWeapon
{
    public Dagger(string id) : base(id)
    {
        Stats[StatType.AttackSpeed].SetValue(1.25f);
        Stats[StatType.AttackRange].SetValue(0.5f);
    }
}
