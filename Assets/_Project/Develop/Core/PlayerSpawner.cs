using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Enum;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject PlayerPrefab;
    public Chest KitStartChest;
    void Start()
    {
        if (FindObjectOfType<CombatSystem>() == null)
        {
            Instantiate(PlayerPrefab, transform.position, Quaternion.identity);
            var chest = Instantiate(KitStartChest, new Vector3(2.14100003f, -0.444525003f, 4.75199986f),
                Quaternion.Euler(new Vector3(0, 142, 0)));
            chest.isKitStart = true;
            //PlayerPrefab.GetComponentInChildren<CombatSystem>().InitializeStats(GameData._preset);
        }
        else
        {
            FindObjectOfType<CombatSystem>().GetComponent<PlayerCharacter>().SetPosition(transform.position);
        }
    }

}
