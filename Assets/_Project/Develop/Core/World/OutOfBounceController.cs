using NaughtyAttributes;
using UnityEngine;

public class OutOfBounceController : MonoBehaviour
{
    [SerializeField, ReadOnly] private PlayerCharacter character;
    [SerializeField, MinValue(0)] private int OutOfBounceDistance;
    [SerializeField] private LevelGenerator levelGenerator;

    public void Initialize(PlayerCharacter character)
    {
        this.character = character;
    }

    private void Update()
    {
        if (character == null)
            return;

        Debug.Log($"Character velocity = {character.velocity}");

        if (character.velocity.y < -OutOfBounceDistance)
            levelGenerator.SpawnPlayer();
    }
}
