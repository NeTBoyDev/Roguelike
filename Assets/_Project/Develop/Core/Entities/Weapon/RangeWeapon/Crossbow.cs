

using _Project.Develop.Core.Entities;

public class Crossbow : RangeWeapon
{
    public Crossbow(string id) : base(id)
    {
        AddProjectile(new Projectile("vfx_Projectile_ArrowPoisoned"));
        isReloadable = true;
    }

}