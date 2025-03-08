using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects;
using _Project.Develop.Core.Effects.Base;

namespace _Project.Develop.Core.Entities.Potions
{
    public class RagePotion : UseableItem
    {
        public RagePotion(string id) : base(id)
        {
            
        }

        public override void Use(BaseEntity entity)
        {
            entity.ApplyEffect(GetEffect());
        }
        public override Effect GetEffect() => new Rage(2 + (int)Rarity,20 + (int)Rarity * 6);
    }
}