using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Player;
using UnityEngine;

public class StartGameMusic : MonoBehaviour
{
    private SoundManager manager;

    public AudioClip clip;
    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1;
        manager = new();
        manager.ProduceDDOLSound(clip);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
