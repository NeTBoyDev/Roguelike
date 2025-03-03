using _Project.Develop.Core.Base;
using _Project.Develop.Core.Enum;

namespace _Project.Develop.Core.Entities
{
    public class Projectile : BaseEntity
    {
        public Projectile(string id) : base(id)
        {
            Stats[StatType.Strength] = new Stat(StatType.Strength, 10f);

        }
    }
}
