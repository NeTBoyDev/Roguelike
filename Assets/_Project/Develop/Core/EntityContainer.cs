using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Entities;
using UnityEngine;
using UnityEngine.Animations;

[RequireComponent(typeof(Rigidbody),typeof(MeshCollider))]
public class EntityContainer : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<MeshCollider>().convex = true;
        
    }

    public BaseEntity ContainedEntity { get; private set; }
    private PositionConstraint effectConstraint;

    public void SetEntity(BaseEntity entity)
    {
        ContainedEntity = entity;
        ((Item)entity).Mesh = GetComponent<MeshFilter>().mesh;

        ConstraintSource source = new ConstraintSource
        {
            sourceTransform = transform, 
            weight = 1f 
        };

        var effectPrefab = Resources.Load<GameObject>($"Effects/{entity.Rarity}");
        if (effectPrefab != null)
        {
            var effectInstance = Instantiate(effectPrefab, transform.position, effectPrefab.transform.rotation); // Делаем дочерним
            effectConstraint = effectInstance.GetComponent<PositionConstraint>();

            if (effectConstraint != null)
            {
                effectConstraint.AddSource(source);
                effectConstraint.constraintActive = true; 

                effectConstraint.translationAtRest = Vector3.zero; 
                effectConstraint.translationOffset = Vector3.zero; 
                effectConstraint.locked = true; 
            }
            else
            {
                Debug.LogError($"PositionConstraint не найден на объекте Effects/{entity.Rarity}!");
                Destroy(effectInstance); 
            }
        }
        else
        {
            Debug.LogError($"Эффект Effects/{entity.Rarity} не найден в Resources!");
        }
    }

    private void OnDestroy()
    {
        if (effectConstraint != null)
        {
            Destroy(effectConstraint.gameObject); // Уничтожаем объект с PositionConstraint
        }
    }
}
