using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class Axe : MeeleWeapon
{
    public Axe(string id) : base(id)
    {
        Stats[StatType.AttackSpeed].SetValue(0.75f);
    }
}
