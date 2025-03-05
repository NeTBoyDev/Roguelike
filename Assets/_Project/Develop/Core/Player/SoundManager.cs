using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        public void ProduceSound(Vector3 position,AudioClip sound,bool infinite = false)
        {
            var source = new GameObject("Sound").AddComponent<AudioSource>();
            source.transform.position = position;
            source.pitch += Random.Range(-0.15f, 0.15f);
            source.clip = sound;
            source.volume = volume;
            if (!infinite)
            {
                source.PlayOneShot(sound);
                Object.Destroy(source.gameObject,sound.length);
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
            if (source != null)
            {
                _sources.Remove(source);
                Object.Destroy(source.gameObject);
            }
        }
    }
}