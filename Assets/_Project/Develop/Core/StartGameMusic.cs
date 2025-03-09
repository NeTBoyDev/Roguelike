using System;
using System.Collections;
using System.Collections.Generic;
using _Project.Develop.Core.Player;
using UnityEngine;

public class StartGameMusic : MonoBehaviour
{
    private SoundManager manager;

    public AudioClip clip;
    // Start is called before the first frame update
    private void Awake()
    {
        Time.timeScale = 1;
    }

    void Start()
    {
        
        manager = new();
        manager.ProduceDDOLSound(clip);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
