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
    
    
    public MeeleWeapon(string id,Rarity rarity) : base(id,rarity)
    {
        //Debug.Log($"Start damage is {Stats[StatType.Damage].CurrentValue} {Projectile.Count}");
        
        //AddProjectile(new Projectile("vfx_Projectile_SwordFire"));
        //AddProjectile(new Projectile("vfx_Projectile_Fireball02"));
        
        // Пример эффектов для ближнего боя
        //AddEffect(new PoisonEffect(5, 1, 100));
        //Effects.Add(new ShotCount(5));
        
        
        //Effects.Add(new ShotCount());           // Выпуск снаряда
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
    
}