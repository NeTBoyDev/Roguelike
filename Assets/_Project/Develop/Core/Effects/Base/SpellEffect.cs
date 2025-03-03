using System.Collections.Generic;
using _Project.Develop.Core.Base;
using UnityEngine;

namespace _Project.Develop.Core.Effects.Base
{
    public abstract class SpellEffect : Effect
    {
        protected float magnitude;

        public virtual void Apply(GameObject target, ref List<GameObject> affectedObjects)
        {
            // Базовая реализация может быть пустой
            // Дочерние классы переопределяют этот метод
        }

        // Унаследованный метод для совместимости
        public virtual void Apply(GameObject target)
        {
            List<GameObject> affectedObjects = new List<GameObject> { target };
            Apply(target, ref affectedObjects);
        }
    }
}
