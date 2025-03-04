using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Effects.SpellEffects;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class MeeleWeapon : Weapon
{
    public List<Projectile> Projectile { get; private set; } = new();
    public List<ProjectileObject> ProjectileView { get; private set; } = new();
    
    public MeeleWeapon(string id) : base(id)
    {
        /*Projectile.Add(new Projectile("vfx_Projectile_SwordFire"));
        Projectile.Add(new Projectile("vfx_Projectile_Fireball02"));*/
        
        // Пример эффектов для ближнего боя
        Effects.Add(new PoisonEffect(5, 1, 100)); // Дебафф: яд
        //Effects.Add(new ShotCount(5));
        
        
        //Effects.Add(new ShotCount());           // Выпуск снаряда

        foreach (var p in Projectile)
        {
            p[StatType.Strength].Modify(p[StatType.Strength].CurrentValue * Effects.Count);
            ProjectileView.Add(Resources.Load<GameObject>($"Projectiles/{p.Id}").GetComponent<ProjectileObject>());
        }
    }

    public void ApplyEffects(BaseEntity target)
    {
        List<ProjectileObject> spawnedProjectiles = new List<ProjectileObject>();
        
        // Применяем эффекты к цели ближнего боя и собираем созданные снаряды
        foreach (var effect in Effects)
        {
            if(effect is not SpellEffect)
            //List<ProjectileObject> effectResults = new List<ProjectileObject>();
                effect.OnApply(target);
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

    /*public void ApplyProjectiles()
    {
        List<ProjectileObject> spawnedProjectiles = new List<ProjectileObject>();
        
        // Применяем эффекты к цели ближнего боя и собираем созданные снаряды
        foreach (var effect in Effects)
        {
            if (effect is not SpellEffect)
            {
                List<ProjectileObject> effectResults = new List<ProjectileObject>();
                spawnedProjectiles.AddRange(effectResults);
            }
                
        }
        foreach (var projectile in spawnedProjectiles)
        {
            projectile.SetDamage(Stats[StatType.Damage].CurrentValue); // Устанавливаем урон оружия
            projectile.SetEffects(Effects.Where(e=> e is not SpellEffect).ToList()); // Передаём эффекты снаряду
            ApplyEffects(projectile);
        }
    }*/
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
                    effect.SetEffects(Effects.Where(e=> e is not SpellEffect).ToList());
                }
                newObjects.AddRange(effectResults);
            }

            currentObjects = newObjects;
        }

        Debug.Log($"Total projectiles created: {currentObjects.Count}");
    }
    
    public void FireProjectile()
    {
        for (int i = 0; i < Projectile.Count; i++)
        {
            var direction = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
            var projectile = Object.Instantiate(ProjectileView[i], 
                Camera.main.transform.position + Camera.main.transform.forward, 
                direction);
        
            projectile.SetDamage(Projectile[i][StatType.Strength].CurrentValue);
        
            ApplyEffects(projectile);
        }
    }
}