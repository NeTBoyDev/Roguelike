using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Enum;
using UnityEngine;

[CreateAssetMenu(menuName = "Preset/Stat")]
public class StatPreset : ScriptableObject
{
    public List<Stat> Stats;
}
