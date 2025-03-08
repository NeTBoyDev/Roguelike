using _Project.Develop.Core.Enum;
using UnityEngine;

namespace _Project.Develop.Core.Entities
{
    public class Gem : Item
    {
        public Projectile projectile;
        public ProjectileObject obj;
        public Gem(string id) : base(id)
        {
            
        }
        
        public void AddProjectile(Projectile p)
        {
            projectile = p;
            projectile[StatType.Strength].Modify(p[StatType.Strength].CurrentValue /* * Effects.Count*/); //СДЕЛАТЬ ФОРМУЛУ
            obj = Resources.Load<GameObject>($"Projectiles/{p.Id}").GetComponent<ProjectileObject>();
        }
    }
}