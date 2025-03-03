using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Effects.SpellEffects;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class RangeWeapon : Weapon
{
    public Projectile Projectile { get; private set; }
    public ProjectileObject ProjectileView { get; private set; }

    public RangeWeapon(string id) : base(id)
    {
        Projectile = new Projectile("vfx_Projectile_ArrowBlue");
        ProjectileView = Resources.Load<GameObject>($"Projectiles/{Projectile.Id}").GetComponent<ProjectileObject>();

        /*Effects.Add(new ShotCount());
        Effects.Add(new TrippleShot());*/
        //Effects.Add(new AutoAim(5));
        
        Projectile[StatType.Strength].Modify(Projectile[StatType.Strength].CurrentValue * Effects.Count);

        Stats[StatType.AttackSpeed].Modify(Effects.Count * Stats[StatType.AttackSpeed].CurrentValue);
    }

    public void ApplyEffects(GameObject originalObj)
    {
        List<GameObject> currentObjects = new List<GameObject> { originalObj };

        for (int i = 0; i < Effects.Count; i++)
        {
            var currentEffect = Effects[i] as SpellEffect;
            List<GameObject> newObjects = new List<GameObject>();

            foreach (var obj in currentObjects)
            {
                List<GameObject> effectResults = new List<GameObject> { obj };
                currentEffect.Apply(obj, ref effectResults);
                newObjects.AddRange(effectResults);
            }

            currentObjects = newObjects;
        }

        Debug.Log($"Total projectiles created: {currentObjects.Count}");
    }
}
