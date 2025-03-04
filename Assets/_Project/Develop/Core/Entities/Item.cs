using _Project.Develop.Core.Base;
using _Project.Develop.Core.Enum;
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
        public int Count = 1;//УБРАТЬ НАХУЙ
        public int MaxStackSize;
        public bool IsStackable => MaxStackSize > 1;
        
        public Item Clone() //УБРАТЬ НАХУЙ
        {
            return new Item(Id)
            {
                Sprite = this.Sprite,
                Count = this.Count,
                MaxStackSize = this.MaxStackSize
            };
        }
    }
}