using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum RoomType
{
    None,
    BaseRoom,
    StartRoom,
    LastRoom,
    Corridor,
}
public class Room : MonoBehaviour
{

    [field: SerializeField] public RoomType Type { get; private set; } = RoomType.None;

    [field: Header("Room object settings")]

    [field: SerializeField, ShowAssetPreview(96, 96)] public GameObject Stairs { get; private set; } = null;
    [field: SerializeField, ReadOnly] public List<Door> Doors { get; private set; } = null;
    [field: SerializeField] public float SpawnChance { get; private set; } = 0;

    [field: Header("Player Spawn settings")]
    [field: SerializeField, ShowIf(nameof(IsStartRoom))] public GameObject PlayerPrefab { get; private set; } = null;
    [field: SerializeField, ShowIf(nameof(IsStartRoom))] public Transform Spawnpoint { get; private set; } = null;


    [field: Header("Debug")]
    [field: SerializeField, ReadOnly] public Collider[] RoomColliders { get; private set; } = null;

    private void Awake()
    {
        RoomColliders = GetComponents<Collider>();
        Doors = GetComponentsInChildren<Door>(true).ToList();
    }

    public float GetRoomSize() => GetComponent<SpriteRenderer>().bounds.size.x;
    public bool IsStartRoom() => Type == RoomType.StartRoom;

}
