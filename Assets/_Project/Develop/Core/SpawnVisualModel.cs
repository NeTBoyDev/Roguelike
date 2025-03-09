using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class SpawnVisualModel : MonoBehaviour
{
    public Transform point;
    private GameObject spawnedObject;

    private void Awake()
    {
        if (spawnedObject == null)
        {
            spawnedObject = Instantiate(GameData._preset.VisualPrefab, point.position,Quaternion.identity,point);
            spawnedObject.AddComponent<RotateObject>();
        }
    }
}
