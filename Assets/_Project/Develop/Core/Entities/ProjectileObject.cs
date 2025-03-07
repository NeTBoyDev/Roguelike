using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class ProjectileObject : MonoBehaviour
{
    private float damage;
    private List<Effect> effects = new List<Effect>();

    public float Damage => damage;

    public void SetDamage(float damageValue)
    {
        damage = damageValue;
    }

    public void SetEffects(List<Effect> weaponEffects)
    {
        print($"Set {weaponEffects.Count} effects");
        effects = new List<Effect>(weaponEffects); // Копируем эффекты
    }

    void Update()
    {
        //transform.Translate(Vector3.forward * 5f * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            AIBase enemy = other.gameObject.GetComponent<AIBase>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage); // Наносим урон
                foreach (var effect in effects)
                {
                    print("Apply");
                    enemy.skeletonModel.ApplyEffect(effect); // Применяем все эффекты
                }
            }
            Destroy(gameObject);
        }
    }
}
