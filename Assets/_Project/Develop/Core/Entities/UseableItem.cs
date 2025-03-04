using UnityEngine;

namespace _Project.Develop.Core.Entities
{
    public class UseableItem : Item
    {
        public UseableItem(string id) : base(id)
        {
            MaxStackSize = 10;
            Count = Random.Range(1,5);
        }
    }
}
