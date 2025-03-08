using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Enum;
using UnityEngine;

namespace _Project.Develop.Core.Effects
{
    public class ExplosiveShot : SpellEffect
    {
        private float explosionRadius;
        private float explosionDamage;

        public ExplosiveShot(float radius = 2f, float damage = 10f)
        {
            explosionRadius = radius;
            explosionDamage = damage;
            Name = "Explosive Shot";
        }

        public override void Apply(ProjectileObject target, ref List<ProjectileObject> affectedObjects)
        {
            target.OnHit += () =>
            {
                Collider[] hits = Physics.OverlapSphere(target.transform.position, explosionRadius);
                foreach (Collider hit in hits)
                {
                    if (hit.TryGetComponent<AIBase>(out var entity))
                    {
                        entity.TakeDamage(-explosionDamage);
                    }
                }
            };
            affectedObjects = new List<ProjectileObject> { target }; // Оригинальный снаряд остаётся
        }
        
        public override Effect Clone()
        {
            return new ExplosiveShot((int)magnitude);
        }
    }
}