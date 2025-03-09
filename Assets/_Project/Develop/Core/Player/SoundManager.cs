using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

namespace _Project.Develop.Core.Player
{
    public class SoundManager
    {

        public SoundManager(float volume = 1)
        {
            this.volume = volume;
        }

        private float volume;
        private List<AudioSource> _sources = new();
        private AudioSource _phrase;
        private static ObjectPool<AudioSource> _pool = new(()=>new GameObject("Sound").AddComponent<AudioSource>()
            ,s=>
            {
                if(s != null)
                    s.enabled = true;
            },
            s=>s.enabled = false,
            s=> { UnityEngine.Object.Destroy(s.gameObject); },
            true,
            25,
            100
            );

        public async void ProduceSound(Vector3 position,AudioClip sound,bool infinite = false)
        {
            if (sound == null)
                return;

            var source = _pool.Get();

            if (source == null)
                return;

            source.transform.position = position;
            source.pitch = UnityEngine.Random.Range(0.85f, 1.15f);
            source.clip = sound;
            source.volume = volume;

            float length = sound.length;
            if (!infinite)
            {
                source.PlayOneShot(sound);
                UniTask.Run(async () =>
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(length));
                    _pool.Release(source);
                });
            }
            else
            {
                _sources.Add(source);
                source.loop = true;
                source.Play();
            }
                
        }

        public void ProduceDDOLSound(AudioClip clip)
        {
            if (GameObject.Find("DDOLSound") != null)
                return;
            var source = new GameObject("DDOLSound").AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = 0.1f;
            source.loop = true;
            source.Play();
            GameObject.DontDestroyOnLoad(source);
        }
        
        public async void ProducePhrase(Vector3 position,AudioClip sound,bool infinite = false)
        {
            if (sound == null)
                return;

            _phrase ??= new GameObject("Phare").AddComponent<AudioSource>();
            
            _phrase.Stop();

            _phrase.transform.position = position;
            _phrase.clip = sound;
            _phrase.volume = volume;
            _phrase.PlayOneShot(sound);
                
        }

        public void StopPlaying(AudioClip sound)
        {
            var source = _sources.FirstOrDefault(s => s.clip == sound);
            if (source != null)
            {
                _sources.Remove(source);
                _pool.Release(source);
            }
        }
    }
}