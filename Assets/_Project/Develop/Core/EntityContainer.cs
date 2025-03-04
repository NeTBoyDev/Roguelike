using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityContainer : MonoBehaviour
{
    public BaseEntity ContainedEntity { get; private set; }

    public void SetEntity(BaseEntity entity)
    {
        ContainedEntity = entity;
    }
}
