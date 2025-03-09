

using _Project.Develop.Core;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;

public class Crossbow : RangeWeapon
{
    public Crossbow(string id,Rarity rarity) : base(id,rarity)
    {
        AddProjectile(ItemGenerator.Instance.GetRandomProjectile(WeaponType.Crossbow));
        //AddProjectile(new Projectile("vfx_Projectile_ArrowPoisoned"));
        isReloadable = true;
    }

}