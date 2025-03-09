using _Project.Develop.Core;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;

public class Staff : RangeWeapon
{
    public Staff(string id,Rarity rarity) : base(id,rarity)
    {
        AddProjectile(ItemGenerator.Instance.GetRandomProjectile(WeaponType.Staff));
        isReloadable = false;
    }
}