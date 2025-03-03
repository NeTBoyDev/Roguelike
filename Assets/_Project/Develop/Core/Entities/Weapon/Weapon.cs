using _Project.Develop.Core.Base;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;

public class Weapon : Item
{
    public Weapon(string id) : base(id)
    {
        Stats[StatType.Damage] = new Stat(StatType.Damage, 10f);
        Stats[StatType.AttackSpeed] = new Stat(StatType.AttackSpeed, 1f);
        Stats[StatType.StaminaCost] = new Stat(StatType.StaminaCost, 5f);
    }
}
