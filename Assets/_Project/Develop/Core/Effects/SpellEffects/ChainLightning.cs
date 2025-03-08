using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Enum;
using UnityEngine;

namespace _Project.Develop.Core.Effects
{
    public class ChainLightning : SpellEffect
    {
        private int chainCount;
        private float chainRange;
        private float chainDamage;

        public ChainLightning(int chainCount = 3, float chainRange = 3f, float chainDamage = 5f)
        {
            this.chainCount = chainCount;
            this.chainRange = chainRange;
            this.chainDamage = chainDamage;
            Name = "Chain Lightning";
        }

        public override void Apply(ProjectileObject target, ref List<ProjectileObject> affectedObjects)
        {
            target.OnHit += () =>
            {
                List<AIBase> hitEntities = new List<AIBase>();
                Vector3 lastPosition = target.transform.position;

                Collider[] initialHit = Physics.OverlapSphere(lastPosition, 0.1f);
                foreach (Collider col in initialHit)
                {
                    if (col.TryGetComponent<AIBase>(out var entity) && !hitEntities.Contains(entity))
                    {
                        hitEntities.Add(entity);
                        entity.TakeDamage(-chainDamage);
                        break;
                    }
                }

                for (int i = 0; i < chainCount - 1 && hitEntities.Count > 0; i++)
                {
                    Collider[] nearby = Physics.OverlapSphere(lastPosition, chainRange);
                    AIBase nextTarget = null;
                    float minDist = float.MaxValue;

                    foreach (Collider col in nearby)
                    {
                        if (col.TryGetComponent<AIBase>(out var entity) && !hitEntities.Contains(entity))
                        {
                            float dist = Vector3.Distance(lastPosition, col.transform.position);
                            if (dist < minDist)
                            {
                                minDist = dist;
                                nextTarget = entity;
                            }
                        }
                    }

                    if (nextTarget != null)
                    {
                        hitEntities.Add(nextTarget);
                        nextTarget.TakeDamage(-chainDamage);
                        lastPosition = nextTarget.transform.position;
                    }
                    else
                    {
                        break;
                    }
                }
            };
            affectedObjects = new List<ProjectileObject> { target };
        }
        public override Effect Clone()
        {
            return new ChainLightning((int)magnitude);
        }
    }
}