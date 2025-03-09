using _Project.Develop.Core.Enum;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public enum RoomEventType
{
    None,
    MobWave,
    Boss
}

public class EventRoom : MonoBehaviour
{
    [field: SerializeField] public Transform[] SpawnPoints { get; private set; } = null;
    [field: SerializeField, MinValue(0), ShowIf(nameof(EventType), RoomEventType.MobWave)] private float _firstSpawnDelay;
    [field: SerializeField, MinValue(0), ShowIf(nameof(EventType), RoomEventType.MobWave)] private float _spawnDelay;

    [field: SerializeField] public RoomEventType EventType { get; private set; } = RoomEventType.None;
    [field: SerializeField] public List<Room> LinkedRoom { get; private set; } = null;

    private List<AIBase> _entities = new(1);
    private GameEvent _roomEvent = null;
    private Inventory _playerInventory;

    public UnityEvent OnRoomEnter;
    public UnityEvent OnRoomCleared;

    private void Start()
    {
        switch (EventType)
        {
            case RoomEventType.None:
                Debug.Log($"<color=red>ROOM EVENT IS NONE!</color>");
                break;

            case RoomEventType.MobWave:
                _roomEvent = EventBuilder.Create()
                    .AddEvent(CloseAllDoors)
                    .AddDelay(_firstSpawnDelay)
                    .AddEvent(SpawnEntities)
                    .AddEvent(WaitForRoomCleared)
                    .AddEvent(ReOpenDoors)
                    .AddEvent(GiveReward)
                    .AddEvent(DestroyAllEntities)
                    .Build();
                break;
            case RoomEventType.Boss:
                _roomEvent = EventBuilder.Create()
                    .AddEvent(CloseAllDoors)
                    .AddDelay(_firstSpawnDelay)
                    .AddEvent(SpawnBoss)
                    .AddEvent(WaitForRoomCleared)
                    .AddEvent(ReOpenDoors)
                    .AddEvent(GiveReward)
                    .AddEvent(DestroyAllEntities)
                    .Build();
                break;
            default:
                break;
        }
    }
    public void AddToLinkedRooms(Room room) => LinkedRoom.Add(room);
    private async UniTaskVoid ExecuteRoomEvents()
    {
        Debug.Log("START EVENT!");
        await _roomEvent.Execute();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out CombatSystem system))
        {
            OnRoomEnter?.Invoke();
            ExecuteRoomEvents().Forget();
            GetComponent<Collider>().enabled = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
    }

    #region Events
    #region MobWaveEvent

    private async void SpawnEntities()
    {
        int entityCount = UnityEngine.Random.Range(1, SpawnPoints.Length);

        for (int i = 0; i < entityCount; i++)
        {
            var spawnedEnemy = MobSpawner.Instance.GetRandomMob(GameData._map);
            spawnedEnemy.transform.position = SpawnPoints[i].position;
            _entities.Add(spawnedEnemy);


            await UniTask.Delay(TimeSpan.FromSeconds(_spawnDelay));
        }
    }
    
    private async void SpawnBoss()
    {
        var spawnedEnemy = MobSpawner.Instance.GetRandomMob(GameData._map);
        spawnedEnemy.transform.position = SpawnPoints[0].position;
        spawnedEnemy.transform.localScale *= 2;
        spawnedEnemy.ModifyDamage(4);
        spawnedEnemy.ModifySpeed(1.5f);
        spawnedEnemy.ModifyHP(10);
        spawnedEnemy.ModifyDropQuality(2);
        _entities.Add(spawnedEnemy);


        await UniTask.Delay(TimeSpan.FromSeconds(_spawnDelay));
        
    }

    private async UniTask WaitForRoomCleared(CancellationToken token)
    {
        while (!RoomCleared() && !token.IsCancellationRequested)
        {
            await UniTask.Yield();
            Debug.Log("Waiting for room to be cleared...");
        }
        Debug.Log("Room cleared!");
    }
    private bool RoomCleared()
    {
        if (_entities.Count == 0)
            return true;

        foreach (var entity in _entities)
        {
            if (entity.transform.position.y < -100)
            {
                _entities.Remove(entity);
                Destroy(entity.gameObject);
            }    

            if (entity != null && !entity.IsDead)
            {
                return false;
            }
        }
        return true;
    }

    private async void DestroyAllEntities()
    {
        List<UniTask> sinkTasks = new List<UniTask>();
        foreach (var entity in _entities)
        {
            if (entity != null)
            {
                sinkTasks.Add(SinkEntityAsync(entity));
            }
        }

        await UniTask.WhenAll(sinkTasks);

        foreach (var entity in _entities)
        {
            if (entity != null)
            {
                Destroy(entity.gameObject);
            }
        }

        _entities.Clear();
    }
    private async UniTask SinkEntityAsync(AIBase entity)
    {
        if (entity == null) return;

        Vector3 startPosition = entity.transform.position;
        Vector3 targetPosition = startPosition + Vector3.down * 5f;
        float duration = 1f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            if (entity == null) break;

            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            entity.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            await UniTask.Yield();
        }

        if (entity != null)
        {
            entity.transform.position = targetPosition;
        }
    }
    #endregion
    #endregion

    private void ForEachDoorInLinkedRooms(Action<Door> doorAction) 
        => LinkedRoom.SelectMany(room => room.Doors).ToList().ForEach(door => doorAction(door));

    private void ReOpenDoors()
    {
        ForEachDoorInLinkedRooms(door => door.DoorEvent.ReopenDoor());
        Debug.Log("Doors reopened!");
    }

    private void CloseAllDoors() => ForEachDoorInLinkedRooms(door => door.DoorEvent.CloseDoor());

    private void GiveReward()
    {
        Debug.Log("give reward");
        OnRoomCleared?.Invoke();
        //_playerInventory.ChangePlayerGold(100);
    }
}