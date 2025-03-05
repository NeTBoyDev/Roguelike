using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;


public class LevelGenerator : MonoBehaviour
{
    [field: SerializeField] public List<Room> RoomPrefabs { get; private set; } = null;


    [field: Header("Settings")]

    [field: Tooltip("Не включая стартовую и финальную комнаты")]
    [field: MinValue(1), MaxValue(1000), SerializeField] public int MaxRoomCount { get; private set; } = 1;
    [field: MinValue(0), SerializeField] public float StartSpawnDelay { get; private set; } = 1;
    [field: MinValue(0), SerializeField] public float RoomSpawnDelay { get; private set; } = 0.1f;
    [field: MinValue(0), SerializeField] public float LastRoomSpawnDelay { get; private set; } = 1f;
    [field: MinValue(0), SerializeField] public float PlayerSpawnDelay { get; private set; } = 0f;

    [field: SerializeField] public bool DebugMode { get; private set; } = false;

    [field: Header("Debug")]
    [field: SerializeField, ReadOnly, ShowIf(nameof(DebugMode))] public GameObject LevelContainer { get; private set; } = null;
    [field: SerializeField, ReadOnly, ShowIf(nameof(DebugMode))] public List<Room> SpawnedRooms { get; private set; } = new(1);
    [SerializeField, ReadOnly, ShowIf(nameof(DebugMode))] private Vector3 _offset = new(100, 0, 0);
    [SerializeField, ReadOnly, ShowIf(nameof(DebugMode))] private int _currentRoomCount = 0;

    private CancellationTokenSource _spawnSource = new();
    private Vector3 _lastSpawnPosition;

    #region Initialize
    private void Start()
    {
        Initialize();
        StartGenerate();
    }

    private void Initialize()
    {
        LevelContainer = GetLevelContainer();
        foreach (var room in RoomPrefabs)
        {
            if (room == null)
            {
                RoomPrefabs.Remove(room);
            }
        }
    }
    #endregion

    #region Generate
   
    public async void StartGenerate()
    {
        if (RoomPrefabs.Count <= 0)
        {
            Debug.LogError("Нет префабов комнат!");
            return;
        }

        GenerateStartRoom();

        await GenerateFloorAsync().AttachExternalCancellation(_spawnSource.Token);

        GenerateLastRoom().Forget();

        SpawnPlayer();
    }

    public async void SpawnPlayer()
    {
        await Delay(PlayerSpawnDelay, _spawnSource.Token);

        var startRoom = GetStartRoom();

        var playerPrefab = startRoom.PlayerPrefab;
        var spawnpoint = startRoom.Spawnpoint;

        if(playerPrefab == null)
        {
            Debug.LogError("Player prefab is null!");
            return;
        }
        if(spawnpoint == null)
        {
            Debug.LogError("Spawnpoint is null!");
            return;
        }

        Instantiate(playerPrefab, spawnpoint.position, Quaternion.identity);
    }

    public async UniTask GenerateFloorAsync()
    {
        await Delay(StartSpawnDelay, _spawnSource.Token);

        while (_currentRoomCount < MaxRoomCount)
        {
            bool placedAnyRoom = false;

            var allBaseRooms = GetAllRandomBaseRooms().ToArray();

            foreach (var room in allBaseRooms)
            {
                if (room == null)
                    continue;

                if (_currentRoomCount >= MaxRoomCount)
                    break;

                var spawnedRoom = SpawnRoom(room);
                if (spawnedRoom != null)
                {
                    placedAnyRoom = true;

                    await Delay(RoomSpawnDelay, _spawnSource.Token);
                }
            }

            if (!placedAnyRoom)
            {
                Debug.LogWarning($"Не удалось разместить больше комнат. Текущее количество: {_currentRoomCount}/{MaxRoomCount}");
                break;
            }
        }

        Debug.Log($"<color=yellow>[Генерация уровня]:</color> Пыталось создать комнат: <color=cyan>{_currentRoomCount}</color>. Успешно создано: <color=yellow>{SpawnedRooms.Count - 1}</color>");
        return;
    }

    public void GenerateStartRoom()
    {
        var randomStartRoom = RoomPrefabs.Where(r => r != null && r.Type == RoomType.StartRoom)
            .OrderBy(_ => UnityEngine.Random.value)
            .FirstOrDefault();

        if (randomStartRoom == null)
        {
            Debug.LogError("Не найдено стартовой комнаты!");
            return;
        }

        SpawnRoom(randomStartRoom);
        _lastSpawnPosition = _offset;
    }

    public async UniTask GenerateLastRoom()
    {
        await Delay(LastRoomSpawnDelay, _spawnSource.Token);

        var randomLastRoom = RoomPrefabs.Where(r => r != null && r.Type == RoomType.LastRoom)
            .OrderBy(_ => UnityEngine.Random.value)
            .FirstOrDefault();

        if (randomLastRoom == null)
        {
            Debug.LogError("Не найдено финальной комнаты!");
            return;
        }

        SpawnRoom(randomLastRoom);
    }

    public Room SpawnRoom(Room room, bool addRoomCount = true)
    {
        if (room == null || room.SpawnChance < UnityEngine.Random.value)
            return null;

        var rotation = GetRandomRotation();

        var spawnedRoom = Instantiate(room, _lastSpawnPosition, rotation, LevelContainer.transform);

        if (SpawnedRooms.Count > 0 && !TryConnectRoomWithRotation(spawnedRoom, addRoomCount))
        {
            Destroy(spawnedRoom.gameObject);
            return null;
        }

        SpawnedRooms.Add(spawnedRoom);
        _lastSpawnPosition += _offset;

        return spawnedRoom;
    }
    #endregion

    #region Handlers

    private bool TryConnectRoomWithRotation(Room currentRoom, bool addRoomCount = true)
    {
        if (currentRoom == null)
            return false;

        currentRoom.transform.localRotation = GetRandomRotation();

        if (TryConnectRoom(currentRoom, addRoomCount))
        {
            return true;
        }

        RemoveRoom(currentRoom);
        return false;
    }
    public async UniTask Delay(float delay, CancellationToken token) => await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
    private Quaternion GetRandomRotation()
    {
        int randomRotation = new int[] { 0, 90, 180, 270 }[UnityEngine.Random.Range(0, 4)];
        Quaternion rotation = Quaternion.Euler(0, randomRotation, 0);

        return rotation;
    }
    private void RemoveRoom(Room currentRoom)
    {
        SpawnedRooms.Remove(currentRoom);
        Destroy(currentRoom.gameObject);

        --_currentRoomCount;
    }
    private GameObject CreateLevelContainer()
    {
        LevelContainer = new GameObject("LevelContainer");
        LevelContainer.transform.SetParent(transform);

        return LevelContainer;
    }
    public GameObject GetLevelContainer()
    {
        if (LevelContainer == null)
            LevelContainer = CreateLevelContainer();

        return LevelContainer;
    }

    public IEnumerable<Room> GetAllRandomBaseRooms()
        => RoomPrefabs.Where(r => r != null && r.Type == RoomType.BaseRoom).OrderBy(_ => UnityEngine.Random.value);
    
    public Room GetRandomCorridor()
        => RoomPrefabs.OrderBy(_ => UnityEngine.Random.value).FirstOrDefault(r => r != null && r.Type == RoomType.Corridor);

    public Room GetStartRoom() => SpawnedRooms[0];
    public Room GetLastRoom() => SpawnedRooms[^1];

    public bool IsCollided(Room room)
    {
        foreach (var spawnedRoom in SpawnedRooms)
        {
            if (spawnedRoom == room)
                continue;

            foreach (var roomCollider in room.RoomColliders)
            {
                foreach (var spawnedRoomCollider in spawnedRoom.RoomColliders)
                {
                    if (Physics.ComputePenetration(
                        roomCollider,
                        room.transform.position,
                        room.transform.rotation,
                        spawnedRoomCollider,
                        spawnedRoom.transform.position,
                        spawnedRoom.transform.rotation,
                        out Vector3 direction, out float distance))
                    {
                        //Debug.Log($"Коллизия: Комната {room.name} сталкивается с {spawnedRoom.name}, расстояние: {distance}");
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool TryConnectRoom(Room currentRoom, bool addRoomCount = true)
    {
        if (currentRoom == null)
            return false;

        var currentRandomDoor = currentRoom.Doors
            .Where(d => !d.Connected)
            .OrderBy(_ => UnityEngine.Random.value)
            .FirstOrDefault();

        if (currentRandomDoor == null)
        {
            RemoveRoom(currentRoom);
            return false;
        }

        //Алгоритм спавна комнат
        var availableDoors = SpawnedRooms
            .Where(r => r != currentRoom)
            .SelectMany(r => r.Doors)
            .Where(d => !d.Connected)
            .OrderBy(d => Vector2.Distance(d.ConnectPoint.position, currentRandomDoor.ConnectPoint.position))
            .ToList();

        if (availableDoors.Count == 0)
        {
            RemoveRoom(currentRoom);
            return false;
        }

        foreach (var doorToConnect in availableDoors)
        {
            Vector3 offset = doorToConnect.ConnectPoint.position - currentRandomDoor.ConnectPoint.position;
            currentRoom.transform.position += offset;

            if (!IsCollided(currentRoom))
            {
                currentRandomDoor.ConnectDoor(doorToConnect);

                if(addRoomCount)
                    _currentRoomCount++;
                return true;
            }
            else
            {
                currentRandomDoor.DisconnectDoor();
                currentRoom.transform.position -= offset;
            }
        }

        RemoveRoom(currentRoom);
        return false;
    }

#endregion

    private void OnDestroy()
    {
        _spawnSource?.Cancel();
        _spawnSource?.Dispose();
    }
}