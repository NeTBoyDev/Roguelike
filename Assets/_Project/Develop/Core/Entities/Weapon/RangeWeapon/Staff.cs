using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;

public class Staff : RangeWeapon
{
    public Staff(string id,Rarity rarity) : base(id,rarity)
    {
        AddProjectile(new Projectile("vfx_Projectile_SwordFire"));
        isReloadable = false;
    }
}