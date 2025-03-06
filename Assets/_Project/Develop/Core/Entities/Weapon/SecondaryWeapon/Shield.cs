using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class Shield : SecondaryWeapon
{
    public Shield(string id) : base(id)
    {
        Stats[StatType.StaminaCost].SetValue(40);
    }
}
