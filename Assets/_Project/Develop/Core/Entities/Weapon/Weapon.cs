using System.Collections.Generic;
using System.Linq;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class Weapon : Item
{
    public List<Projectile> Projectile { get; private set; } = new();
    public List<ProjectileObject> ProjectileView { get; private set; } = new();
    public Weapon(string id,Rarity rarity) : base(id)
    {
        Rarity = rarity;
        Stats[StatType.Damage] = new Stat(StatType.Damage, 10f * GameData.Rarity[Rarity]);
        Stats[StatType.AttackSpeed] = new Stat(StatType.AttackSpeed, 1f / GameData.Rarity[Rarity]);
        Stats[StatType.StaminaCost] = new Stat(StatType.StaminaCost, 20f);
        Stats[StatType.AttackRange] = new Stat(StatType.AttackRange, 0.75f * GameData.Rarity[Rarity]);
    }

    public void AddProjectile(Projectile p)
    {
        Projectile.Add(p);
        p[StatType.Strength].Modify(p[StatType.Strength].CurrentValue * Effects.Count); //СДЕЛАТЬ ФОРМУЛУ
        ProjectileView.Add(Resources.Load<GameObject>($"Projectiles/{p.Id}").GetComponent<ProjectileObject>());
    }
    
    public void ApplyEffects(BaseEntity target)
    {
        List<ProjectileObject> spawnedProjectiles = new List<ProjectileObject>();
        
        // Применяем эффекты к цели ближнего боя и собираем созданные снаряды
        foreach (var effect in Effects)
        {
            Debug.Log($"Apply {effect.Name}");
            if (effect is not SpellEffect)
            {
                target.ApplyEffect(effect);
                //effect.OnApply(target);
            }
            //List<ProjectileObject> effectResults = new List<ProjectileObject>();
                
            //spawnedProjectiles.AddRange(effectResults);
        }

        // Применяем эффекты к каждому созданному снаряду
        foreach (var projectile in spawnedProjectiles)
        {
            projectile.SetDamage(Stats[StatType.Damage].CurrentValue); // Устанавливаем урон оружия
            projectile.SetEffects(Effects); // Передаём эффекты снаряду
        }

        Debug.Log($"Total projectiles created: {spawnedProjectiles.Count}");
    }
    
    public void ApplyEffects(ProjectileObject originalObj)
    {
        originalObj.SetEffects(Effects.Where(e=> e is not SpellEffect).ToList());
        List<ProjectileObject> currentObjects = new List<ProjectileObject> { originalObj };

        for (int i = 0; i < Effects.Count; i++)
        {
            if(Effects[i] is not SpellEffect currentEffect)
                continue;
            List<ProjectileObject> newObjects = new List<ProjectileObject>();

            foreach (var obj in currentObjects)
            {
                List<ProjectileObject> effectResults = new List<ProjectileObject> { obj };
                currentEffect.Apply(obj, ref effectResults);
                foreach (var effect in effectResults)
                {
                    effect.SetDamage(originalObj.Damage);
                    //effect.SetEffects(Effects.Where(e=> e is not SpellEffect).ToList());
                }
                newObjects.AddRange(effectResults);
            }

            currentObjects = newObjects;
        }
        foreach (var proj in currentObjects)
        {
            proj.SetEffects(Effects.Where(e=> e is not SpellEffect).ToList());
        }
        Debug.Log($"Total projectiles created: {currentObjects.Count}");
    }
    
    public void FireProjectile(float multiplyier = 1)
    {
        for (int i = 0; i < Projectile.Count; i++)
        {
            var direction = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
            var projectile = Object.Instantiate(ProjectileView[i], 
                Camera.main.transform.position + Camera.main.transform.forward, 
                direction);
        
            projectile.SetDamage(Projectile[i][StatType.Strength].CurrentValue * multiplyier * GameData.Rarity[Rarity]);
        
            ApplyEffects(projectile);
        }
    }
}
