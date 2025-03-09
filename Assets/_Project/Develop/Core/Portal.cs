using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [field:SerializeField]public bool inDungeon { get; set; } = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out CombatSystem c))
        {
            if(inDungeon)
                SceneManager.LoadSceneAsync(1);
            else
                SceneManager.LoadSceneAsync(2);
            
        }
    }
}
