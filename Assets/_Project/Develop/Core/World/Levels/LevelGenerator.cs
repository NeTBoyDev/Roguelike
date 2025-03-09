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
using UnityEditor;
using _Project.Develop.Core.Enum;

public enum GenerateAlgorithm
{
    FullRandom,
    NearestDoors,
    PriorityDoors,
    LinearPath
}

public class LevelGenerator : MonoBehaviour
{
    [field: SerializeField] private OutOfBounceController outOfBounceController;

    [field: HorizontalLine(2, EColor.Green)]
    [field: SerializeField] public List<Room> StartRooms { get; private set; } = null;
    [field: SerializeField] public List<Room> LastRooms { get; private set; } = null;
    [field: HorizontalLine(2, EColor.Green)]
    [field: SerializeField] public List<Room> RoomPrefabs { get; private set; } = null;
    [field: HorizontalLine(2, EColor.Green)]

    [field: InfoBox("LinearPath = so many bugs")]
    [field: SerializeField] public GenerateAlgorithm GenerateAlgorithm { get; private set; } = GenerateAlgorithm.FullRandom;
    [field: HorizontalLine(2, EColor.Green)]
    [field: Header("Settings")]

    [field: Tooltip("MaxRoomCount exclude Start and Last room")]
    [field: MinValue(1), MaxValue(100), SerializeField] public int MaxRoomCount { get; private set; } = 1;
    [field: Tooltip("Threshold at which doors are not destroyed. The lower the value, the closer the door must be. <color=yellow>Recommended value = 1</color>")]
    [field: SerializeField] public float OverlapDoorThreshold { get; private set; } = 1f;
    [field: Tooltip("Minimum number of rooms before connecting to final room")]
    [field: MinValue(1), SerializeField] public int MinRoomsBeforeFinalConnection { get; private set; } = 5;

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
    [SerializeField, ReadOnly, ShowIf(nameof(DebugMode))] private int _currentRoomCount = 0;
    [SerializeField, ReadOnly, ShowIf(nameof(DebugMode))] private bool _finalRoomConnected = false;

    private readonly CancellationTokenSource _spawnSource = new();

    private LevelGenerationPipeline _pipeline;
    private readonly Dictionary<Room, bool> _spawnedOnceRooms = new();
    private Room _finalRoom;
    private PlayerCharacter _characterController;

    #region Initialize
    private void Awake() => astarPath = FindObjectOfType<AstarPath>();
    private void Start() => Initialize(new LevelGeneratorSettings(GenerateAlgorithm, MaxRoomCount, 2));

    public void Initialize(LevelGeneratorSettings settings)
    {
        if (GameData._map != null)
            MaxRoomCount = (int)(settings.MaxRoomCount * GameData._map.Stats[StatType.RoomCount].CurrentValue);
        else
            MaxRoomCount = settings.MaxRoomCount;

        GenerateAlgorithm = settings.GenerateAlgorithm;

        LevelContainer = GetLevelContainer();
        RoomPrefabs.RemoveAll(room => room == null);

        StartGenerate(settings);
    }
    #endregion

    #region Generate

    private async void StartGenerate(LevelGeneratorSettings settings)
    {
        if (RoomPrefabs.Count <= 0)
        {
            Debug.LogError("RoomPrefabs count = 0");
            return;
        }

        _pipeline = LevelGenerationPipeline.Create().AddDelay(StartSpawnDelay)
            .AddStep(GenerateFloorAsync)
            .AddStep(SpawnPlayerAsync).AddDelay(2).AddDelay(settings.BakeFrameCount)
            .AddStep(SetupGraphDynamically).AddDelay(PlayerSpawnDelay)
            .AddStep(SpawnVendor).AddDelay(1) //TEST VENDOR (NEED TO DELETE) (fake vendor)
            .AddStep(RemoveCollidersInSpawnedRooms);

        await _pipeline.Execute();
    }
    private void RemoveCollidersInSpawnedRooms()
    {
        foreach (var room in SpawnedRooms)
        {
            room.RemoveAllRoomColliders();
        }
    }
    public async void SpawnPlayerAsync()
    {
        var startRoom = GetStartRoom();

        //CheckNull(PlayerPrefab, "Player prefab is null!");
        //CheckNull(startRoom.Spawnpoint, "Spawnpoint is null!");
        //CheckNull(VendorPrefab, "Vendor prefab is null!");

        while (_characterController == null)
        {
            _characterController = FindObjectOfType<PlayerCharacter>();
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
        }

        _characterController.SetPosition(startRoom.Spawnpoint.position);
        //Instantiate(PlayerPrefab, startRoom.Spawnpoint.position, Quaternion.identity);


        outOfBounceController.Initialize(_characterController);
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
        if (startRoom != null)
        {
            var spawnPosition = startRoom.RoomColliders[0].bounds.center - Vector3.one;
            Instantiate(VendorPrefab, spawnPosition, Quaternion.identity);
        }
    }

    public void GenerateStartRoom()
    {
        var randomStartRoom = StartRooms
            .OrderBy(_ => UnityEngine.Random.value)
            .FirstOrDefault();

        if (randomStartRoom == null)
        {
            Debug.LogError("Failed to generate the start room!");
            return;
        }

        SpawnRoom(randomStartRoom);
        _currentRoomCount++;
    }

    public void GenerateFinalRoomInitial()
    {
        var randomLastRoom = LastRooms
            .OrderBy(_ => UnityEngine.Random.value)
            .FirstOrDefault();

        if (randomLastRoom == null)
        {
            Debug.LogError("Failed to generate the final room!");
            return;
        }

        Vector3 startPos = GetStartRoom().transform.position;
        Vector3 direction = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized;
        Vector3 finalPosition = startPos + direction * 100f;

        _finalRoom = Instantiate(randomLastRoom, finalPosition, GetRandomRotation(), LevelContainer.transform);
        SpawnedRooms.Add(_finalRoom);

        Debug.Log($"<color=green>[Level Generator]:</color> Final room spawned at distance {Vector3.Distance(startPos, finalPosition)} from start room.");
    }

    public async UniTask GenerateFloorAsync()
    {
        bool generationSuccessful = false;
        int maxAttempts = 10; // Ограничение попыток
        int attemptCount = 0;

        while (!generationSuccessful && attemptCount < maxAttempts)
        {
            attemptCount++;
            Debug.Log($"<color=yellow>[Level Generator]:</color> Generation attempt {attemptCount}/{maxAttempts}");

            // Очищаем текущий уровень перед новой попыткой
            ClearCurrentLevel();

            // Генерируем стартовую комнату
            GenerateStartRoom();
            if (SpawnedRooms.Count == 0)
            {
                Debug.LogError("Failed to spawn start room. Retrying...");
                continue;
            }
            await Delay(RoomSpawnDelay, _spawnSource.Token);

            // Основной цикл генерации промежуточных комнат
            while (_currentRoomCount < MaxRoomCount && !_finalRoomConnected)
            {
                bool placedAnyRoom = false;
                var allBaseRooms = GetAllRandomBaseRooms().ToArray();

                if (allBaseRooms.Length == 0 && _currentRoomCount < MaxRoomCount)
                {
                    Debug.LogWarning($"No available rooms to spawn at count {_currentRoomCount}. Possible range restrictions or all unique rooms used.");
                    if (_currentRoomCount >= MinRoomsBeforeFinalConnection)
                    {
                        Debug.Log("<color=yellow>[Level Generator]:</color> No more rooms available. Connecting to final room...");
                        TryConnectToFinalRoom(forceConnect: true);
                    }
                    break;
                }

                foreach (var room in allBaseRooms)
                {
                    if (_currentRoomCount >= MaxRoomCount || _finalRoomConnected)
                        break;

                    if (room == null)
                        continue;

                    var spawnedRoom = SpawnRoom(room);
                    if (spawnedRoom != null)
                    {
                        placedAnyRoom = true;
                        _currentRoomCount++;
                        await Delay(RoomSpawnDelay, _spawnSource.Token);
                    }
                }

                if (!placedAnyRoom && !_finalRoomConnected)
                {
                    Debug.LogWarning($"Cannot generate more rooms. Generated rooms: {_currentRoomCount}/{MaxRoomCount}");
                    if (_currentRoomCount >= MinRoomsBeforeFinalConnection)
                    {
                        Debug.Log("<color=yellow>[Level Generator]:</color> Connecting to final room as no more regular rooms can be placed.");
                        TryConnectToFinalRoom(forceConnect: true);
                    }
                    break;
                }
            }

            // Генерируем финальную комнату, если она еще не создана
            if (_finalRoom == null)
            {
                GenerateFinalRoomInitial();
                if (_finalRoom == null)
                {
                    Debug.LogError("Failed to spawn final room. Retrying...");
                    continue;
                }
                await Delay(RoomSpawnDelay, _spawnSource.Token);
            }

            // Подключаем финальную комнату, если еще не подключена
            if (!_finalRoomConnected && _currentRoomCount >= MinRoomsBeforeFinalConnection)
            {
                Debug.Log("<color=yellow>[Level Generator]:</color> Generation complete. Now connecting final room...");
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    if (TryConnectToFinalRoom(forceConnect: true))
                    {
                        _finalRoomConnected = true;
                        Debug.Log($"<color=green>[Level Generator]:</color> Successfully connected final room on attempt {attempt + 1}!");
                        break;
                    }
                    await Delay(0.2f, _spawnSource.Token);
                }
            }

            // Проверяем успешность генерации
            if (_currentRoomCount >= MaxRoomCount && _finalRoomConnected)
            {
                generationSuccessful = true;
            }
            else
            {
                Debug.LogWarning($"<color=red>[Level Generator]:</color> Failed to spawn required rooms. Spawned: {_currentRoomCount}/{MaxRoomCount}. Final room connected: {_finalRoomConnected}. Retrying...");
            }
        }

        if (!generationSuccessful)
        {
            Debug.LogError($"<color=red>[Level Generator]:</color> Failed to generate level after {maxAttempts} attempts!");
        }

        string connectionStatus = _finalRoomConnected ? "<color=green>Connected</color>" : "<color=red>Not Connected</color>";
        Debug.Log($"<color=yellow>[Level Generator]:</color> Created {_currentRoomCount} rooms. Total rooms: {SpawnedRooms.Count}. Final room status: {connectionStatus}");
    }
    private void ClearCurrentLevel()
    {
        foreach (var room in SpawnedRooms)
        {
            if (room != null)
            {
                Destroy(room.gameObject);
            }
        }

        SpawnedRooms.Clear();

        _spawnedOnceRooms.Clear();

        _currentRoomCount = 0;

        _finalRoomConnected = false;
        _finalRoom = null;
    }
    private bool TryConnectToFinalRoom(bool forceConnect = false)
    {
        var recentRooms = forceConnect
            ? SpawnedRooms
                .Where(r => r != _finalRoom)
                .OrderByDescending(r => SpawnedRooms.IndexOf(r))
                .Take(5)
            : SpawnedRooms.OrderByDescending(r => SpawnedRooms.IndexOf(r)).Take(5).ToList();

        //Attempt 1: standard door connection
        foreach (var room in recentRooms)
        {
            var availableDoors = room.Doors.Where(d => d != null && !d.Connected).ToList();
            if (availableDoors.Count == 0) continue;

            var finalRoomDoors = _finalRoom.Doors.Where(d => d != null && !d.Connected).ToList();
            if (finalRoomDoors.Count == 0)
            {
                Debug.LogWarning("Final room has no available doors for connection.");
                return false;
            }

            foreach (var sourceDoor in availableDoors)
            {
                foreach (var targetDoor in finalRoomDoors)
                {
                    Vector3 originalFinalRoomPos = _finalRoom.transform.position;
                    Vector3 offset = sourceDoor.ConnectPoint.position - targetDoor.ConnectPoint.position;

                    _finalRoom.transform.position += offset;

                    if (!IsCollided(_finalRoom))
                    {
                        ConnectDoors(sourceDoor, targetDoor, false);
                        Debug.Log($"<color=green>[Level Generator]:</color> Connected room {room.name} to final room via doors.");
                        return true;
                    }

                    _finalRoom.transform.position = originalFinalRoomPos;
                }
            }
        }

        // Попытка 2: экстренное соединение — спавн рядом со случайной комнатой
        if (forceConnect && !_finalRoomConnected)
        {
            Debug.Log("[Level Generator]: Using emergency connection method for final room...");

            // Выбираем случайную комнату с хотя бы одной свободной дверью
            var roomsWithDoors = SpawnedRooms
                .Where(r => r != _finalRoom && r.Doors.Any(d => d != null && !d.Connected))
                .OrderBy(_ => UnityEngine.Random.value) // Случайный порядок
                .ToList();

            if (roomsWithDoors.Count == 0)
            {
                Debug.LogWarning("No rooms with available doors found for emergency connection.");
                return false;
            }

            // Пробуем несколько комнат (до 3 попыток)
            for (int roomAttempt = 0; roomAttempt < Math.Min(3, roomsWithDoors.Count); roomAttempt++)
            {
                var randomRoom = roomsWithDoors[roomAttempt];
                var sourceDoor = randomRoom.Doors.FirstOrDefault(d => d != null && !d.Connected);
                var targetDoor = _finalRoom.Doors.FirstOrDefault(d => d != null && !d.Connected);

                if (sourceDoor == null || targetDoor == null)
                {
                    continue;
                }

                // Пробуем несколько позиций рядом с комнатой (до 5 попыток)
                for (int positionAttempt = 0; positionAttempt < 5; positionAttempt++)
                {
                    // Случайное направление и расстояние (от 10 до 20 единиц)
                    Vector3 randomDirection = UnityEngine.Random.insideUnitSphere;
                    randomDirection.y = 0; // Ограничиваем по горизонтали
                    randomDirection = randomDirection.normalized;
                    float randomDistance = UnityEngine.Random.Range(10f, 20f);

                    Vector3 newPosition = randomRoom.transform.position + randomDirection * randomDistance;
                    _finalRoom.transform.position = newPosition;

                    // Вычисляем угол поворота и округляем до кратного 90° в меньшую сторону
                    Quaternion lookRotation = Quaternion.LookRotation(-randomDirection);
                    float yAngle = lookRotation.eulerAngles.y; // Получаем угол по оси Y
                    float roundedAngle = Mathf.Floor(yAngle / 90f) * 90f; // Округляем в меньшую сторону
                    _finalRoom.transform.rotation = Quaternion.Euler(0, roundedAngle, 0);

                    // Проверяем коллизии и соединяем, если позиция свободна
                    if (!IsCollided(_finalRoom))
                    {
                        // Точное выравнивание дверей
                        Vector3 offset = sourceDoor.ConnectPoint.position - targetDoor.ConnectPoint.position;
                        _finalRoom.transform.position += offset;

                        if (!IsCollided(_finalRoom)) // Дополнительная проверка после выравнивания
                        {
                            ConnectDoors(sourceDoor, targetDoor, false);
                            Debug.Log($"<color=green>[Level Generator]:</color> Emergency connection successful near {randomRoom.name} at angle {roundedAngle}°!");
                            return true;
                        }
                        else
                        {
                            _finalRoom.transform.position -= offset; // Откат выравнивания
                        }
                    }
                }
            }

            Debug.LogWarning("[Level Generator]: Failed to find a valid position for final room in emergency mode.");
            return false;
        }

        return false;
    }

    public Room SpawnRoom(Room room, bool addRoomCount = true)
    {
        if (room == null || room.SpawnChance < UnityEngine.Random.value)
            return null;

        if (!CanRoomSpawnBasedOnRange(room))
        {
            Debug.Log($"The room <color=red>{room}</color> is out of range!");
            return null;
        }

        var rotation = GetRandomRotation();
        var spawnedRoom = Instantiate(room, new Vector3(100, 0, 0), rotation, LevelContainer.transform);

        if (SpawnedRooms.Count > 0 && !TryConnectRoomWithRotation(spawnedRoom, false))
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

    public Room GetStartRoom() => SpawnedRooms.Count > 0 ? SpawnedRooms[0] : null;
    public Room GetLastRoom() => _finalRoomConnected ? _finalRoom : null;

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

    #region Algorithm
    private bool TryConnectRoom(Room currentRoom, bool addRoomCount = true)
    {
        if (currentRoom == null)
        {
            if (DebugMode) Debug.LogError("currentRoom is null");
            return false;
        }

        if (!TryGetRandomDoor(currentRoom, out var currentRandomDoor))
        {
            if (DebugMode) Debug.LogWarning($"No free doors in room {currentRoom.name}");
            RemoveRoom(currentRoom);
            return false;
        }

        var algorithmDoors = GetDoorsByAlgorithm(currentRandomDoor, currentRoom);

        if (algorithmDoors == null || algorithmDoors.Count == 0)
        {
            if (DebugMode) Debug.LogWarning($"No available doors for connecting room {currentRoom.name}.");
            RemoveRoom(currentRoom);
            return false;
        }

        foreach (var doorToConnect in algorithmDoors)
        {
            if (doorToConnect == null || doorToConnect.ConnectPoint == null)
            {
                if (DebugMode) Debug.LogError("doorToConnect or its ConnectPoint is null");
                continue;
            }

            Vector3 offset = doorToConnect.ConnectPoint.position - currentRandomDoor.ConnectPoint.position;
            currentRoom.transform.position += offset;

            if (!IsCollided(currentRoom))
            {
                ConnectDoors(currentRandomDoor, doorToConnect, addRoomCount);
                return true;
            }

            currentRandomDoor.DisconnectDoor();
            currentRoom.transform.position -= offset;
        }

        if (DebugMode) Debug.LogWarning($"Failed to connect room {currentRoom.name} - removing");
        RemoveRoom(currentRoom);
        return false;
    }

    private bool TryGetRandomDoor(Room room, out Door door)
    {
        if (GenerateAlgorithm == GenerateAlgorithm.PriorityDoors)
        {
            door = room.Doors.Where(d => d != null && !d.Connected)
                            .OrderByDescending(d => d.Priority)
                            .ThenBy(_ => UnityEngine.Random.value)
                            .FirstOrDefault();
        }
        else
        {
            // Для других алгоритмов оставляем оригинальную логику
            door = room.Doors.Where(d => d != null && !d.Connected)
                            .OrderBy(_ => UnityEngine.Random.value)
                            .FirstOrDefault();
        }
        return door != null;
    }

    private List<Door> GetDoorsByAlgorithm(Door currentDoor, Room currentRoom)
    {
        if (SpawnedRooms == null || SpawnedRooms.Count == 0)
        {
            if (DebugMode) Debug.LogError("SpawnedRooms is empty or null");
            return new List<Door>();
        }

        var availableDoors = SpawnedRooms
            .Where(r => r != null && r != currentRoom && r != _finalRoom)
            .SelectMany(r => r.Doors)
            .Where(d => d != null && !d.Connected);

        if (!availableDoors.Any() && DebugMode)
        {
            Debug.LogWarning("No available doors in SpawnedRooms");
        }

        switch (GenerateAlgorithm)
        {
            case GenerateAlgorithm.FullRandom:
                return availableDoors.OrderBy(_ => UnityEngine.Random.value).ToList();

            case GenerateAlgorithm.NearestDoors:
                return availableDoors.OrderBy(d => Vector3.Distance(d.ConnectPoint.position, currentDoor.ConnectPoint.position)).ToList();

            case GenerateAlgorithm.PriorityDoors:
                return availableDoors
                    .OrderByDescending(d => d.IsImportantConnection ? 1 : 0)
                    .ThenByDescending(d => d.Priority)
                    .ThenBy(d => Vector3.Distance(d.ConnectPoint.position, currentDoor.ConnectPoint.position))
                    .ToList();

            case GenerateAlgorithm.LinearPath:
                var lastNormalRoom = SpawnedRooms
                    .Where(r => r != _finalRoom)
                    .OrderByDescending(r => SpawnedRooms.IndexOf(r))
                    .FirstOrDefault();

                if (lastNormalRoom == null || lastNormalRoom == currentRoom)
                {
                    return availableDoors.OrderBy(_ => UnityEngine.Random.value).ToList();
                }

                var lastRoomDoors = lastNormalRoom.Doors.Where(d => d != null && !d.Connected).ToList();
                if (lastRoomDoors.Count == 0)
                {
                    return availableDoors.OrderBy(d => Vector3.Distance(d.ConnectPoint.position, currentDoor.ConnectPoint.position)).ToList();
                }

                return lastRoomDoors;

            default:
                throw new ArgumentOutOfRangeException(nameof(GenerateAlgorithm), "Unknown generation algorithm");
        }
    }

    private void ConnectDoors(Door currentDoor, Door doorToConnect, bool addRoomCount)
    {
        currentDoor.ConnectDoor(doorToConnect);
        float distance = Vector3.Distance(currentDoor.ConnectPoint.position, doorToConnect.ConnectPoint.position);

        if (DebugMode)
            Debug.Log($"Distance between doors: {distance} (threshold: {OverlapDoorThreshold})");

        if (distance <= OverlapDoorThreshold)
        {
            currentDoor.gameObject.SetActive(false);
            if (DebugMode)
                Debug.Log($"Door {currentDoor.name} disabled (distance: {distance})", currentDoor);
        }

        Room currentRoom = currentDoor.GetComponentInParent<Room>(); //Комната, которую подключаем
        Room doorRoom = doorToConnect.GetComponentInParent<Room>(); //Комната, к которой подключаемся

        if (currentRoom.EventRoom != null)
        {
            currentRoom.EventRoom.AddToLinkedRooms(doorRoom);
            if (DebugMode)
                Debug.Log($"EventRoom {currentRoom.name} linked to {doorRoom.name}");
        }

        if (doorRoom == _finalRoom)
            _finalRoomConnected = true;

        if (addRoomCount)
            _currentRoomCount++;
    }
    #endregion

    #endregion

    #region Astar
    private AstarPath astarPath;

    private void SetupGraphDynamically()
    {
        Vector3 minBounds = Vector3.positiveInfinity;
        Vector3 maxBounds = Vector3.negativeInfinity;

        foreach (var room in SpawnedRooms)
        {
            Bounds bounds = room.GetComponent<Collider>().bounds;
            minBounds = Vector3.Min(minBounds, bounds.min);
            maxBounds = Vector3.Max(maxBounds, bounds.max);
        }

        float padding = 25f;
        minBounds -= Vector3.one * padding;
        maxBounds += Vector3.one * padding;

        GridGraph gridGraph = astarPath.data.layerGridGraph;
        gridGraph.center = (minBounds + maxBounds) / 2f;
        gridGraph.center = new Vector3(gridGraph.center.x, -10, gridGraph.center.z);
        gridGraph.SetDimensions(
            Mathf.CeilToInt((maxBounds.x - minBounds.x) / gridGraph.nodeSize),
            Mathf.CeilToInt((maxBounds.z - minBounds.z) / gridGraph.nodeSize),
            gridGraph.nodeSize
        );

        astarPath.Scan();

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

public struct LevelGeneratorSettings
{
    public GenerateAlgorithm GenerateAlgorithm;
    public int MaxRoomCount;
    public int BakeFrameCount;
    public LevelGeneratorSettings(GenerateAlgorithm algorithm, int maxRoomCount, int bakeFrameCount)
    {
        GenerateAlgorithm = algorithm;
        MaxRoomCount = maxRoomCount;
        BakeFrameCount = bakeFrameCount;
    }
}