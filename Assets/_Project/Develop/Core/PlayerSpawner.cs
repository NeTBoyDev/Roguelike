using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject PlayerPrefab;
    void Start()
    {
        if (FindObjectOfType<CombatSystem>() == null)
        {
            Instantiate(PlayerPrefab, Vector3.zero, Quaternion.identity);
            PlayerPrefab.GetComponent<CombatSystem>().InitializeStats(GameData._preset);
        }
    }

}
