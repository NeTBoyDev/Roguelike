using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class Map : Item
{
    public Map(Rarity r,string id) : base(id)
    {
        Rarity = r;
        Stats[StatType.DropCount] = new Stat(StatType.DropCount, 1 * GameData.Rarity[Rarity] + Random.Range(-0.2f,0.2f));
        Stats[StatType.DropQuality] = new Stat(StatType.DropQuality, 1 * GameData.Rarity[Rarity] + Random.Range(-0.2f,0.2f));
        Stats[StatType.MobPower] = new Stat(StatType.MobPower, 1 * GameData.Rarity[Rarity] + Random.Range(-0.2f,0.2f));
        Stats[StatType.RoomCount] = new Stat(StatType.RoomCount, 1 * GameData.Rarity[Rarity] + Random.Range(-0.2f,0.2f));
    }
}

