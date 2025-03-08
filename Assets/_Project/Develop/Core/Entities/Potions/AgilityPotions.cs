using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects;
using _Project.Develop.Core.Effects.Base;

namespace _Project.Develop.Core.Entities.Potions
{
    public class AgilityPotions : UseableItem
    {
        public AgilityPotions(string id) : base(id)
        {
            
        }

        public override void Use(BaseEntity entity)
        {
            entity.ApplyEffect(GetEffect());
        }
        public override Effect GetEffect() => new HasteEffect(2 + (int)Rarity,20 + (int)Rarity * 6);
    }
}