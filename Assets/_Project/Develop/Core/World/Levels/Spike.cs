using NaughtyAttributes;
using UnityEngine;

public class Spike : MonoBehaviour
{
    [SerializeField] private TweenMover TweenMover; //Spike mover
    [SerializeField] private bool UseRandomDamage;
    [SerializeField, MinValue(0), MaxValue(200), HideIf(nameof(UseRandomDamage))] private float _damage;
    [SerializeField, MinMaxSlider(0, 200), ShowIf(nameof(UseRandomDamage))] private Vector2 _randomDamage;

    private void Start()
    {
        TweenMover.OnAnimationEnded += Damage;
    }

    private void Damage(Collider other)
    {
        if(other == null) 
            return;

        if (other.TryGetComponent(out CombatSystem player))
        {
            float finalamage = 0;

            if (UseRandomDamage)
                finalamage = Random.Range(_randomDamage.x, _randomDamage.y);
            else
                finalamage = _damage;

            player.TakeDamage(finalamage);
        }
    }
}
