using _Project.Develop.Core.Base;
using UnityEngine;

namespace _Project.Develop.Core.Entities
{
    public class UseableItem : Item
    {
        public UseableItem(string id) : base(id)
        {
            MaxStackSize = 10;
            Count = 9;
        }

        public virtual void Use(BaseEntity entity)
        {
            
        }
        
        public virtual Effect GetEffect() => new Effect(0);
    }
}
