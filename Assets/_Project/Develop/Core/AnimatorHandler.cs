using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorHandler : MonoBehaviour
{
    [SerializeField] private CombatSystem _system;

    public void Attack()
    {
        _system.HandleAttack();
    }
}
