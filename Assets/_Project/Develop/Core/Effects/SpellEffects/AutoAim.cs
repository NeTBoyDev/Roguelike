using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Effects.Base;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AutoAim : SpellEffect
{
    public AutoAim(float additionalShots = 1)
    {
        magnitude = additionalShots; // Радиус поиска и множитель масштаба
        Name = "Auto aim";
    }

    public override void Apply(ProjectileObject target, ref List<ProjectileObject> affectedObjects)
    {
        List<ProjectileObject> newObjects = new List<ProjectileObject>(affectedObjects);

        foreach (var projectile in newObjects)
        {
            HandleAutoAim(projectile).Forget(); 
        }

        affectedObjects = newObjects;
    }

    private async UniTaskVoid HandleAutoAim(ProjectileObject obj)
    {
        float rotationSpeed = 360f; 
        float searchInterval = 0.1f; 
        float maxDuration = 5f; 

        float elapsedTime = 0f;

        while (elapsedTime < maxDuration && obj != null) 
        {
            SkeletonAI nearestEnemy = FindNearestSkeletonAI(obj.transform.position, magnitude);

            if (nearestEnemy != null)
            {
                Vector3 targetPosition = nearestEnemy.transform.position;

                while (obj != null && Vector3.Distance(obj.transform.position, targetPosition) > 0.1f)
                {
                    Vector3 direction = (targetPosition - obj.transform.position).normalized;

                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    obj.transform.rotation = Quaternion.RotateTowards(obj.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                    await UniTask.Yield(PlayerLoopTiming.Update);

                    if (nearestEnemy == null || nearestEnemy.gameObject == null)
                        break;
                }

                break;
            }

            await UniTask.Delay(System.TimeSpan.FromSeconds(searchInterval));
            elapsedTime += searchInterval;
        }
    }

    private SkeletonAI FindNearestSkeletonAI(Vector3 position, float radius)
    {
        SkeletonAI nearest = null;
        float minDistance = radius;

        Collider[] colliders = Physics.OverlapSphere(position, radius);
        foreach (var collider in colliders)
        {
            SkeletonAI enemy = collider.GetComponent<SkeletonAI>();
            if (enemy != null)
            {
                float distance = Vector3.Distance(position, enemy.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = enemy;
                }
            }
        }

        return nearest;
    }
    
}
