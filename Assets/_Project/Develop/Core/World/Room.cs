using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

public enum RoomType
{
    None,
    BaseRoom,
    StartRoom,
    LastRoom,
}

[RequireComponent(typeof(Rigidbody))]
public class Room : MonoBehaviour
{
    [field: SerializeField] public RoomType Type { get; private set; } = RoomType.None;

    [field: Header("Room object settings")]

    [field: MinValue(0), MaxValue(1), SerializeField] public float SpawnChance { get; private set; } = 1;
    [field: SerializeField, ShowIf(nameof(IsStartRoom))] public Transform Spawnpoint { get; private set; } = null;

    [field: Tooltip("If enabled, the room can spawn on the level only once during the entire generation.")]
    [field: SerializeField] public bool SpawnOnlyOnce { get; private set; } = false;

    [field: Tooltip("The range in which the room can spawn. <color=yellow>Example: From 4 to 25</color> means that the room can start appearing only from the 4th spawned room, and after the 25th, it will no longer be able to appear.")]
    [field: SerializeField, MinMaxSlider(0, 100)] public Vector2 SpawnRange { get; private set; } = new Vector2(0, 100);

    [field: Header("Debug")]
    [field: SerializeField, ReadOnly] public Collider[] RoomColliders { get; private set; } = null;
    [field: SerializeField, ReadOnly] public List<Door> Doors { get; private set; } = null;

    private void Awake()
    {
        RoomColliders = GetComponents<Collider>();
        Doors = GetComponentsInChildren<Door>(true).ToList();
    }
    //private async void Start()
    //{
    //    //EventBuilder use example
    //    //var chain = EventBuilder.Create()
    //    //    .AddEvent(IsStartRoom)
    //    //    .AddDelay(2)
    //    //    .AddEvent(() => Debug.Log("EVENT!")).Build();


    //    //await chain.Execute();
    //}
    public float GetRoomSize() => GetComponent<SpriteRenderer>().bounds.size.x;
    public bool IsStartRoom() => Type == RoomType.StartRoom;
}
