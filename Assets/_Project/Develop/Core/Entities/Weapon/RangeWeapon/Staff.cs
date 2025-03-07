using _Project.Develop.Core.Entities;

public class Staff : RangeWeapon
{
    public Staff(string id) : base(id)
    {
        AddProjectile(new Projectile("vfx_Projectile_SwordFire"));
        isReloadable = false;
    }
}