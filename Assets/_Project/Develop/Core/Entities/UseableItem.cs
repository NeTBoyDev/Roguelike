using _Project.Develop.Core.Base;
using UnityEngine;

namespace _Project.Develop.Core.Entities
{
    public class UseableItem : Item
    {
        public UseableItem(string id) : base(id)
        {
            MaxStackSize = 3;
            Count = Random.Range(1, MaxStackSize +1);
        }

        public virtual void Use(BaseEntity entity)
        {
            
        }
        
        public virtual Effect GetEffect() => new Effect(0);
    }
}
