using _Project.Develop.Core;
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
        return mob;
    }
}