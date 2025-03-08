using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;

namespace _Project.Develop.Core.Entities.Potions
{
    public class HealingPotion : UseableItem
    {
        public HealingPotion(string id) : base(id)
        {
            
        }

        public override void Use(BaseEntity entity)
        {
            entity.ApplyEffect(GetEffect());
        }

        public override Effect GetEffect() => new Heal(2 + (int)Rarity, 10 - (int)Rarity, 1);
    }
}