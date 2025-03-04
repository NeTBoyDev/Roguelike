using System;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects;
using _Project.Develop.Core.Effects.SpellEffects;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Develop.Core
{
    [CreateAssetMenu(menuName = "Item/Generator")]
    public class ItemGenerator : ScriptableObject
    {
        #region Singleton

        private static ItemGenerator instance;

        public static ItemGenerator Instance
        {
            get
            {
                if (instance == null)
                {
                    // Пытаемся загрузить существующий экземпляр из ресурсов
                    instance = Resources.Load<ItemGenerator>("ItemGenerator");

                    // Если не найден, создаём новый (в редакторе или в рантайме)
                    if (instance == null)
                    {
                        instance = CreateInstance<ItemGenerator>();
#if UNITY_EDITOR
                        // Сохраняем как ассет в редакторе
                        AssetDatabase.CreateAsset(instance, "Assets/Resources/ItemGenerator.asset");
                        AssetDatabase.SaveAssets();
#endif
                        Debug.LogWarning("ItemGenerator не был найден в Resources. Создан новый экземпляр.");
                    }
                }
                return instance;
            }
        }

        // Метод для явной инициализации (опционально)
        public static void Initialize()
        {
            _ = Instance; // Просто вызываем getter, чтобы инициализировать
        }

        #endregion

        public GameObject[] Melee;
        public GameObject[] Range;
        public GameObject[] Artifacts;

        public GameObject[] ModificatorObjects;
        public GameObject[] UseableItems;

        public Effect[] MeleeEffects = new Effect[]
        {
            new PoisonEffect(5, 1, 5),
            new SlowEffect(5)
        };

        public Effect[] RangeEffects = new Effect[]
        {
            new AutoAim(),
            new BigSize(),
            new ShotCount(),
            new TrippleShot()
        };

        public EntityContainer GenerateWeaponGameobject(WeaponType weaponType, Rarity rarity)
        {
            GameObject weapon = null;

            switch (weaponType)
            {
                case WeaponType.MeeleWeapon:
                    weapon = Melee[Random.Range(0, Melee.Length)]; // Исправлено с Range на Melee
                    break;
                case WeaponType.RangeWeapon:
                    weapon = Range[Random.Range(0, Range.Length)];
                    break;
                case WeaponType.UseableItems:
                    weapon = UseableItems[Random.Range(0, UseableItems.Length)];
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, null);
            }

            var weaponContainer = Instantiate(weapon).AddComponent<EntityContainer>();
            Debug.Log(weapon.name);
        
            // Создаём модель оружия в зависимости от типа
            BaseEntity weaponModel = GenerateWeapon(weaponType, rarity, weapon.name);

            // Добавляем эффекты в зависимости от типа оружия
            Effect[] effectsArray = weaponType == WeaponType.MeeleWeapon ? MeleeEffects : RangeEffects;
            for (int i = 0; i < (int)rarity; i++)
            {
                weaponModel.Effects.Add(effectsArray[Random.Range(0, effectsArray.Length)]);
            }

            weaponContainer.SetEntity(weaponModel);
            return weaponContainer;
        }
        public BaseEntity GenerateWeapon(WeaponType weaponType, Rarity rarity, string name)
        {
            BaseEntity weaponModel = weaponType switch
            {
                WeaponType.MeeleWeapon => new MeeleWeapon(name),
                WeaponType.RangeWeapon => new RangeWeapon(name),
                WeaponType.UseableItems => new UseableItem(name),
                _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, null)
            };

            weaponModel.Rarity = rarity;

            Effect[] effectsArray = weaponType == WeaponType.MeeleWeapon ? MeleeEffects : RangeEffects;
            int effectCount = (int)rarity;
            for (int i = 0; i < effectCount; i++)
            {
                weaponModel.Effects.Add(effectsArray[Random.Range(0, effectsArray.Length)]);
            }
            Debug.Log($"{effectCount}");
            Debug.Log($"{weaponModel.Effects.Count}");
            return weaponModel;
        }
    }
    public enum WeaponType
    {
        MeeleWeapon,
        RangeWeapon,
        UseableItems,
    }
}