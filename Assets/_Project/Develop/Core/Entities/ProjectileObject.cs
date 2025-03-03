using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class ProjectileObject : MonoBehaviour
{
    public float Damage { get; private set; }

    public void SetDamage(float value)
    {
        Damage = value;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            SkeletonAI enemy = other.gameObject.GetComponent<SkeletonAI>();
            if (enemy != null)
            {
                enemy.TakeDamage(Damage);
            }
            Destroy(gameObject);
        }
        OnHit?.Invoke();
    }

    public event Action OnHit;
}
