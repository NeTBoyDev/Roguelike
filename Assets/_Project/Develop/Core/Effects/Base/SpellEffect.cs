using System.Collections.Generic;
using _Project.Develop.Core.Base;
using UnityEngine;

namespace _Project.Develop.Core.Effects.Base
{
    public abstract class SpellEffect : Effect
    {
        public float magnitude;
        
        public void SetMagnitude(float value)
        {
            magnitude = value;
        }

        public virtual void Apply(ProjectileObject target, ref List<ProjectileObject> affectedObjects)
        {
            // Базовая реализация может быть пустой
            // Дочерние классы переопределяют этот метод
        }

        // Унаследованный метод для совместимости
        public virtual void Apply(ProjectileObject target)
        {
            List<ProjectileObject> affectedObjects = new List<ProjectileObject> { target };
            Apply(target, ref affectedObjects);
        }
        
        public virtual void Apply(GameObject target, Vector3 attackPoint, float attackRange, Weapon sourceWeapon, ref List<ProjectileObject> spawnedProjectiles)
        {
            // Базовая реализация может быть пустой
            // Дочерние классы переопределяют этот метод
        }

        public virtual void Apply(GameObject target, ref List<ProjectileObject> affectedObjects)
        {
            // Совместимость с RangeWeapon
        }

        public virtual void Apply(GameObject target)
        {
            List<ProjectileObject> affectedObjects = new List<ProjectileObject> { target.GetComponent<ProjectileObject>() };
            Apply(target, ref affectedObjects);
        }

        protected SpellEffect() : base(0)
        {
        }
    }
}
