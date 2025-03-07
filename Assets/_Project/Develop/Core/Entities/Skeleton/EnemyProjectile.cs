using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private float damage;
    public void SetDamage(float Damage)
    {
        damage = Damage;
    }
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.TryGetComponent(out CombatSystem player))
        {
            player.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
