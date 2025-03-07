using System.Collections.Generic;
using _Project.Develop.Core.Effects.Base;
using UnityEngine;

namespace _Project.Develop.Core.Effects
{
    public class ReflectShot : SpellEffect
    {
        private int reflectCount;       // Количество отражаемых снарядов
        private float spreadAngle;      // Угол разброса в горизонтальной плоскости

        public ReflectShot(int reflectCount = 3, float spreadAngle = 20f)
        {
            this.reflectCount = reflectCount;
            this.spreadAngle = spreadAngle;
            Name = "Reflect Shot";
        }

        public override void Apply(ProjectileObject target, ref List<ProjectileObject> affectedObjects)
        {
            var damage = target.Damage;
            target.OnHit += () =>
            {
                Debug.Log("Reflecting");
                Vector3 reflectDirection;
                reflectDirection = -target.transform.forward.normalized;
                for (int i = 0; i < reflectCount; i++)
                {
                    // Вычисляем угол разброса
                    float angle = Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, (float)i / (reflectCount - 1));
                    Vector3 spreadDirection = Quaternion.Euler(0, angle, 0) * reflectDirection;

                    // Создаём новый снаряд
                    ProjectileObject newShot = Object.Instantiate(target, target.transform.position, Quaternion.LookRotation(spreadDirection));
                    newShot.SetDamage(damage);
                }
            };
        }
    }
}