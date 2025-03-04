using UnityEngine;

namespace _Project.Develop.Core.Entities
{
    public class Item : BaseEntity
    {
        public Item(string id) : base(id)
        {
            Sprite = Resources.Load<Sprite>($"Sprites/{id}");
        }

        public Sprite Sprite;
        public int Count = 1;
        public int MaxStackSize;
        public bool IsStackable => MaxStackSize > 1;
       
    }
}