using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Pathfinding;
using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks.Linq;


public class LevelGenerator : MonoBehaviour
{
    [field: HorizontalLine(2, EColor.Green)]
    [field: SerializeField] public List<Room> StartRooms { get; private set; } = null;
    [field: SerializeField] public List<Room> LastRooms { get; private set; } = null;
    [field: HorizontalLine(2, EColor.Green)]
    [field: SerializeField] public List<Room> RoomPrefabs { get; private set; } = null;
    [field: HorizontalLine(2, EColor.Green)]

    [field: Header("Settings")]

    [field: Tooltip("MaxRoomCount exclude Start and Last room")]
    [field: MinValue(1), MaxValue(100), SerializeField] public int MaxRoomCount { get; private set; } = 1;
    [field: Tooltip("Threshold at which doors are not destroyed. The lower the value, the closer the door must be. <color=yellow>Recommended value = 1</color>")]
    [field: SerializeField] public float OverlapDoorThreshold { get; private set; } = 1f;

    [field: Header("Delays")]
    [field: MinValue(0), SerializeField] public float StartSpawnDelay { get; private set; } = 1;
    [field: MinValue(0), SerializeField] public float RoomSpawnDelay { get; private set; } = 0.1f;
    [field: MinValue(0), SerializeField] public float LastRoomSpawnDelay { get; private set; } = 1f;
    [field: MinValue(0), SerializeField] public float PlayerSpawnDelay { get; private set; } = 0f;


    [field: Header("Object Spawn settings")]
    [field: SerializeField] public GameObject PlayerPrefab { get; private set; } = null;
    [field: SerializeField] public GameObject VendorPrefab { get; private set; } = null;


    [field: SerializeField] public bool DebugMode { get; private set; } = false;

    [field: Header("Debug")]
    [field: SerializeField, ReadOnly, ShowIf(nameof(DebugMode))] public GameObject LevelContainer { get; private set; } = null;
    [field: SerializeField, ReadOnly, ShowIf(nameof(DebugMode))] public List<Room> SpawnedRooms { get; private set; } = new(1);
    [SerializeField, ReadOnly, ShowIf(nameof(DebugMode))] private Vector3 _offset = new(100, 0, 0);
    [SerializeField, ReadOnly, ShowIf(nameof(DebugMode))] private int _currentRoomCount = 0;


    private readonly CancellationTokenSource _spawnSource = new();

    private LevelGenerationPipeline _pipeline;
    private Dictionary<Room, bool> _spawnedOnceRooms = new();

    #region Initialize
    private void Start()
    {
        Initialize();
        StartGenerate();
        
        astarPath = FindObjectOfType<AstarPath>();
    }

    private void Initialize()
    {
        LevelContainer = GetLevelContainer();
        RoomPrefabs.RemoveAll(room => room == null);
    }
    #endregion

    #region Generate
   
    public async void StartGenerate()
    {
        if (RoomPrefabs.Count <= 0)
        {
            Debug.LogError("RoomPrefabs count = 0");
            return;
        }

        _pipeline = LevelGenerationPipeline.Create()
            .AddDelay(StartSpawnDelay)
            .AddStep(GenerateStartRoom)
            .AddDelay(RoomSpawnDelay)
            .AddStep(GenerateFloorAsync)
            .AddDelay(LastRoomSpawnDelay)
            .AddStep(GenerateLastRoom)
            .AddDelay(2)
            .AddStep(SetupGraphDynamically)
            .AddDelay(PlayerSpawnDelay)
            .AddStep(SpawnPlayer)
            .AddDelay(1)
            .AddStep(SpawnVendor);

        await _pipeline.Execute();
    }

    public void SpawnPlayer()
    {
        var startRoom = GetStartRoom();


        CheckNull(PlayerPrefab, "Player prefab is null!");
        CheckNull(startRoom.Spawnpoint, "Spawnpoint is null!");
        CheckNull(VendorPrefab, "Vendor prefab is null!");

        Instantiate(PlayerPrefab, startRoom.Spawnpoint.position, Quaternion.identity);
    }
    private void CheckNull(object obj, string errorMessage)
    {
        if (obj == null)
        {
            Debug.LogError(errorMessage, this);
            throw new System.ArgumentNullException();
        }
    }
    public void SpawnVendor()
    {
        var startRoom = GetStartRoom();
        if(startRoom != null)
        {
            var spawnPosition = startRoom.RoomColliders[0].bounds.center - Vector3.one;
            Instantiate(VendorPrefab, spawnPosition, Quaternion.identity);
        }
    }

    public async UniTask GenerateFloorAsync()
    {
        while (_currentRoomCount < MaxRoomCount)
        {
            bool placedAnyRoom = false;

            var allBaseRooms = GetAllRandomBaseRooms().ToArray();

            if (allBaseRooms.Length == 0 && _currentRoomCount < MaxRoomCount)
            {
                Debug.LogWarning($"No available rooms to spawn at count {_currentRoomCount}. Possible range restrictions or all unique rooms used.");
                break;
            }

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
            if (!placedAnyRoom && _currentRoomCount < MaxRoomCount)
            {
                Debug.LogWarning($"No rooms placed in this iteration. Current: {_currentRoomCount}/{MaxRoomCount}. Continuing...");
                continue;
            }

            if (!placedAnyRoom)
            {
                Debug.LogWarning($"Cannot generate more rooms. Generated rooms: {_currentRoomCount}/{MaxRoomCount}");
                break;
            }
        }

        Debug.Log($"<color=yellow>[Level Creator]:</color> Attempted to create room: <color=cyan>{_currentRoomCount}</color>. Rooms created: <color=yellow>{SpawnedRooms.Count}</color>");
    }

    public void GenerateStartRoom()
    {
        var randomStartRoom = StartRooms
            .OrderBy(_ => UnityEngine.Random.value)
            .FirstOrDefault();

        if (randomStartRoom == null)
        {
            Debug.LogError("Failed to generate the room!");
            return;
        }

        SpawnRoom(randomStartRoom);
        _currentRoomCount++;
    }

    public void GenerateLastRoom()
    {
        var randomLastRoom = LastRooms
            .OrderBy(_ => UnityEngine.Random.value)
            .FirstOrDefault();


        if (randomLastRoom == null)
        {
            Debug.LogError("Failed to generate the level!");
            return;
        }

        SpawnRoom(randomLastRoom);
    }

    public Room SpawnRoom(Room room, bool addRoomCount = true)
    {
        if (room == null || room.SpawnChance < UnityEngine.Random.value)
            return null;

        if (room.SpawnOnlyOnce && _spawnedOnceRooms.ContainsKey(room) && _spawnedOnceRooms[room])
        {
            Debug.Log($"<color=darkred>The room has already been spawned.</color>");
            return null;
        }

        if (!CanRoomSpawnBasedOnRange(room))
        {
            Debug.Log($"<color=red>The room {room} is out of range!</color>");
            return null;
        }


        var rotation = GetRandomRotation();
        var spawnedRoom = Instantiate(room, new Vector3(100, 0, 0), rotation, LevelContainer.transform);

        if (SpawnedRooms.Count > 0 && !TryConnectRoomWithRotation(spawnedRoom, addRoomCount))
        {
            Destroy(spawnedRoom.gameObject);
            return null;
        }

        SpawnedRooms.Add(spawnedRoom);

        if (room.SpawnOnlyOnce)
        {
            _spawnedOnceRooms[room] = true;
        }

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

        _currentRoomCount = Mathf.Clamp(_currentRoomCount--, 0, 100);
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
    {
        return RoomPrefabs
            .Where(r => r != null && r.Type == RoomType.BaseRoom)
            .Where(r => !r.SpawnOnlyOnce || !_spawnedOnceRooms.ContainsKey(r) || !_spawnedOnceRooms[r])
            .OrderBy(_ => UnityEngine.Random.value);
    }
    private bool CanRoomSpawnBasedOnRange(Room room)
    {
        int currentRoomIndex = _currentRoomCount;
        float minSpawn = room.SpawnRange.x;
        float maxSpawn = room.SpawnRange.y;

        return currentRoomIndex >= minSpawn && currentRoomIndex <= maxSpawn;
    }
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
                        //Debug.Log($"Warning: Room {room.name} collided with {spawnedRoom.name}, distance: {distance}");
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

        var availableDoors = SpawnedRooms
            .Where(r => r != currentRoom)
            .SelectMany(r => r.Doors)
            .Where(d => !d.Connected)
            //.OrderBy(d => UnityEngine.Random.value)
            .OrderBy(d => Vector3.Distance(d.ConnectPoint.position, currentRandomDoor.ConnectPoint.position) <= 20)
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

                float distanceBetweenDoors = Vector3.Distance(currentRandomDoor.ConnectPoint.position, doorToConnect.ConnectPoint.position);

                if (DebugMode)
                {
                    Debug.Log($"Расстояние между дверями: {distanceBetweenDoors} (порог: {OverlapDoorThreshold})");
                }

                if (distanceBetweenDoors <= OverlapDoorThreshold)
                {
                    doorToConnect.gameObject.SetActive(false);
                    if (DebugMode)
                    {
                        Debug.Log($"Дверь {currentRandomDoor.name} отключена (расстояние: {distanceBetweenDoors})", currentRandomDoor);
                    }
                }

                if (addRoomCount)
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

    private AstarPath astarPath;

    private void SetupGraphDynamically()
    {
        //Initialize boundaries
        Vector3 minBounds = Vector3.positiveInfinity;
        Vector3 maxBounds = Vector3.negativeInfinity;

        //Collect the boundaries of all rooms
        foreach (var room in SpawnedRooms)
        {
            Bounds bounds = room.GetComponent<Collider>().bounds;
            minBounds = Vector3.Min(minBounds, bounds.min);
            maxBounds = Vector3.Max(maxBounds, bounds.max);
        }

        //Add a small padding
        float padding = 25f;
        minBounds -= Vector3.one * padding;
        maxBounds += Vector3.one * padding;

        //Configure the Grid Graph
        GridGraph gridGraph = astarPath.data.gridGraph;
        gridGraph.center = (minBounds + maxBounds) / 2f; // Set graph center
        gridGraph.center = new Vector3(gridGraph.center.x, -10, gridGraph.center.z);
        gridGraph.SetDimensions(
            Mathf.CeilToInt((maxBounds.x - minBounds.x) / gridGraph.nodeSize),
            Mathf.CeilToInt((maxBounds.z - minBounds.z) / gridGraph.nodeSize),
            gridGraph.nodeSize
        );

        astarPath.Scan();

        //For async method

        //var enumerator = astarPath.ScanAsync().GetEnumerator();
        //while (enumerator.MoveNext())
        //{
        //    await UniTask.Yield(PlayerLoopTiming.Update); // Уступаем управление Unity
        //}

        Debug.Log("Graph scanning completed!");
    }


    #endregion

    private void OnDestroy()
    {
        _spawnSource?.Cancel();
        _spawnSource?.Dispose();
        _pipeline?.Cancel();
    }
}

public class LevelGenerationPipeline
{
    private readonly List<Func<UniTask>> _steps = new();
    private readonly CancellationTokenSource _cts = new();

    private LevelGenerationPipeline() { }

    public static LevelGenerationPipeline Create() => new();

    public LevelGenerationPipeline AddStep(Func<UniTask> asyncStep)
    {
        _steps.Add(async () =>
        {
            if (_cts.Token.IsCancellationRequested) return;
            await asyncStep().AttachExternalCancellation(_cts.Token);
        });
        return this;
    }

    //Coroutine support
    public LevelGenerationPipeline AddStep(Func<IEnumerator> coroutineFunc, MonoBehaviour owner)
    {
        _steps.Add(async () =>
        {
            if (_cts.Token.IsCancellationRequested) return;

            var tcs = new UniTaskCompletionSource();

            owner.StartCoroutine(WrapCoroutine(coroutineFunc(), tcs));

            await tcs.Task;
        });
        return this;
    }
    private IEnumerator WrapCoroutine(IEnumerator coroutine, UniTaskCompletionSource tcs)
    {
        yield return coroutine;
        tcs.TrySetResult(); //Complete the UniTask when the coroutine finishes
    }

    public LevelGenerationPipeline AddStep(Action step)
    {
        _steps.Add(() =>
        {
            if (_cts.Token.IsCancellationRequested) return UniTask.CompletedTask;
            step?.Invoke();
            return UniTask.CompletedTask;
        });
        return this;
    }

    public LevelGenerationPipeline AddStep<T>(Func<T, UniTask> asyncStep, T param)
    {
        _steps.Add(async () =>
        {
            if (_cts.Token.IsCancellationRequested) return;
            await asyncStep(param).AttachExternalCancellation(_cts.Token);
        });
        return this;
    }

    public LevelGenerationPipeline AddDelay(float seconds)
    {
        _steps.Add(async () =>
        {
            if (_cts.Token.IsCancellationRequested) return;
            await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: _cts.Token);
        });
        return this;
    }

    public LevelGenerationPipeline AddDelay(int frameCount)
    {
        _steps.Add(async () =>
        {
            if (_cts.Token.IsCancellationRequested) return;
            for (int i = 0; i < frameCount; i++)
            {
                if (_cts.Token.IsCancellationRequested) return;
                await UniTask.Yield(PlayerLoopTiming.Update, _cts.Token);
            }
        });
        return this;
    }

    public async UniTask Execute()
    {
        try
        {
            foreach (var step in _steps)
            {
                if (_cts.Token.IsCancellationRequested) break;
                await step();
            }
        }
        catch (OperationCanceledException)
        {
            
        }
    }

    public void Cancel()
    {
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
    }
}