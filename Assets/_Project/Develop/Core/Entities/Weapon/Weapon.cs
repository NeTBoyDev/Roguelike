using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class Weapon : Item
{
    public List<Projectile> Projectile { get; private set; } = new();
    public List<ProjectileObject> ProjectileView { get; private set; } = new();
    public Weapon(string id) : base(id)
    {
        Stats[StatType.Damage] = new Stat(StatType.Damage, 10f);
        Stats[StatType.AttackSpeed] = new Stat(StatType.AttackSpeed, 1f);
        Stats[StatType.StaminaCost] = new Stat(StatType.StaminaCost, 25f);
        Stats[StatType.AttackRange] = new Stat(StatType.AttackRange, 0.75f);
    }

    public void AddProjectile(Projectile p)
    {
        Projectile.Add(p);
        p[StatType.Strength].Modify(p[StatType.Strength].CurrentValue * Effects.Count); //СДЕЛАТЬ ФОРМУЛУ
        ProjectileView.Add(Resources.Load<GameObject>($"Projectiles/{p.Id}").GetComponent<ProjectileObject>());
    }
}
