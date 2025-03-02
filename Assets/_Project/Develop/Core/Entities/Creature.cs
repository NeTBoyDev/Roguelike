using _Project.Develop.Core.Base;
using _Project.Develop.Core.Enum;

public class Creature : BaseEntity
{
    public Creature(string id) : base(id)
    {
        Stats[StatType.Strength] = new Stat(StatType.Strength, 10f);
        Stats[StatType.Stamina] = new Stat(StatType.Stamina, 100f, 100f);
        Stats[StatType.Agility] = new Stat(StatType.Agility, 10f);
        Stats[StatType.Intelligence] = new Stat(StatType.Intelligence, 10f);
        Stats[StatType.Health] = new Stat(StatType.Health, 100f, 100f);
    }
}