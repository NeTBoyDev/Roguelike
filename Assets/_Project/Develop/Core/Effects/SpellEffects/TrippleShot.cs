using System.Collections.Generic;
using _Project.Develop.Core.Effects.Base;
using UnityEngine;

namespace _Project.Develop.Core.Effects.SpellEffects
{
    public class TrippleShot : SpellEffect
    {
        public TrippleShot(int additionalShots = 1)
        {
            magnitude = additionalShots * 2; 
        }

        public override void Apply(GameObject target, ref List<GameObject> affectedObjects)
        {
            List<GameObject> newObjects = new List<GameObject> { target }; 

            for (int i = 1; i <= magnitude / 2; i++)
            {
                float angle = 12f * i;
                Quaternion directionRight = Quaternion.Euler(0, angle, 0) *
                                            Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up);
                Quaternion directionLeft = Quaternion.Euler(0, -angle, 0) *
                                           Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up);

                Vector3 offsetRight = target.transform.right * i * 0.5f;
                Vector3 offsetLeft = -target.transform.right * i * 0.5f;

                var projectileRight =
                    Object.Instantiate(target, target.transform.position + offsetRight, directionRight);
                var projectileLeft =
                    Object.Instantiate(target, target.transform.position + offsetLeft, directionLeft);

                newObjects.Add(projectileRight);
                newObjects.Add(projectileLeft);
            }

            affectedObjects = newObjects; // Обновляем список
        }
    }
}
