using _Project.Develop.Core;
using _Project.Develop.Core.Enum;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "MobSpawner")]
public class MobSpawner : ScriptableObject
{
    [SerializeField] private AIBase[] mobPrefabs;
    private static MobSpawner instance;

    public static MobSpawner Instance
    {
        get
        {
            if (instance == null)
            {
                // Пытаемся загрузить существующий экземпляр из ресурсов
                instance = Resources.Load<MobSpawner>("MobSpawner");

                // Если не найден, создаём новый (в редакторе или в рантайме)
                if (instance == null)
                {
                    instance = CreateInstance<MobSpawner>();
#if UNITY_EDITOR
                    // Сохраняем как ассет в редакторе
                    AssetDatabase.CreateAsset(instance, "Assets/Resources/MobSpawner.asset");
                    AssetDatabase.SaveAssets();
#endif
                    Debug.LogWarning("ItemGenerator не был найден в Resources. Создан новый экземпляр.");
                }
            }

            return instance;
        }
    }

    public AIBase GetRandomMob()
    {
        return Instantiate(mobPrefabs[Random.Range(0, mobPrefabs.Length)]);
    }
    public AIBase GetRandomMob(float multiplyer)
    {
        var mob = Instantiate(mobPrefabs[Random.Range(0, mobPrefabs.Length)]);
        mob.ModifyDamage(multiplyer);
        mob.ModifyHP(multiplyer);
        mob.ModifySpeed(multiplyer);
        mob.ModifyDropCount(multiplyer);
        mob.ModifyDropQuality(multiplyer);
        return mob;
    }
    
    public AIBase GetRandomMob(Map map)
    {
        var mob = Instantiate(mobPrefabs[Random.Range(0, mobPrefabs.Length)]);
        mob.ModifyDamage(map.Stats[StatType.MobPower].CurrentValue);
        mob.ModifyHP(map.Stats[StatType.MobPower].CurrentValue);
        mob.ModifySpeed(map.Stats[StatType.MobPower].CurrentValue);
        mob.ModifyDropCount(map.Stats[StatType.DropCount].CurrentValue);
        mob.ModifyDropQuality(map.Stats[StatType.DropQuality].CurrentValue);
        return mob;
    }
}