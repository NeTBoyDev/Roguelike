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
        private static ObjectPool<AudioSource> _pool = new(()=>new GameObject("Sound").AddComponent<AudioSource>()
            ,s=>
            {
                if(s != null)
                    s.enabled = true;
            },
            s=>s.enabled = false,
            s=> { Object.Destroy(s.gameObject); },
            true,
            25,
            100
            );

        public async void ProduceSound(Vector3 position,AudioClip sound,bool infinite = false)
        {
            var source = _pool.Get();
            source.transform.position = position;
            source.pitch = Random.Range(0.85f, 1.15f);
            source.clip = sound;
            source.volume = volume;
            int length = (int)sound.length * 1000;
            if (!infinite)
            {
                source.PlayOneShot(sound);
                UniTask.Run(async () =>
                {
                    await UniTask.Delay(length);
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

        public void StopPlaying(AudioClip sound)
        {
            var source = _sources.FirstOrDefault(s => s.clip == sound);
            _sources.Remove(source);
            if (source != null)
            {
                _pool.Release(source);
            }
        }
    }
}