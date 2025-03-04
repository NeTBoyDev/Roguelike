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

public class RangeWeapon : Weapon
{
    public List<Projectile> Projectile { get; private set; } = new();
    public List<ProjectileObject> ProjectileView { get; private set; } = new();

    public RangeWeapon(string id) : base(id)
    {
        Projectile.Add(new Projectile("vfx_Projectile_SwordFire"));
        Projectile.Add(new Projectile("vfx_Projectile_Fireball02"));

        //Effects.Add(new ShotCount(10));
        //Effects.Add(new PoisonEffect(5,1,10));
        //Effects.Add(new TrippleShot(10));
        //Effects.Add(new AutoAim(5));
        
        Projectile[0][StatType.Strength].Modify(Projectile[0][StatType.Strength].CurrentValue * Effects.Count);

        Stats[StatType.AttackSpeed].Modify(Effects.Count * Stats[StatType.AttackSpeed].CurrentValue);
        
        foreach (var p in Projectile)
        {
            p[StatType.Strength].Modify(p[StatType.Strength].CurrentValue * Effects.Count);
            ProjectileView.Add(Resources.Load<GameObject>($"Projectiles/{p.Id}").GetComponent<ProjectileObject>());
        }
    }

    public void ApplyEffects(ProjectileObject originalObj)
    {
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
