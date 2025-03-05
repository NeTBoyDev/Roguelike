using System;
using System.Linq;
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
        public GameObject[] Shields,SpellBooks;

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
            var weapon = GenerateContainer(weaponType, rarity);
        
            // Создаём модель оружия в зависимости от типа
            BaseEntity weaponModel = GenerateWeapon(weaponType, rarity, weapon.name);

            weapon.SetEntity(weaponModel);
            return weapon;
        }

        public EntityContainer GenerateContainer(WeaponType weaponType, Rarity rarity)
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
                case WeaponType.Shield:
                    weapon = Shields[Random.Range(0, Shields.Length)];
                    break;
                case WeaponType.SpellBook:
                    weapon = Shields[Random.Range(0, Shields.Length)];
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, null);
            }

            var weaponContainer = Instantiate(weapon).AddComponent<EntityContainer>();
            weaponContainer.name = weapon.name;
            return weaponContainer;
        }
        public EntityContainer GenerateContainer(Item weapon)
        {
            GameObject obj = null;
            if(weapon is MeeleWeapon)
                obj = Melee.FirstOrDefault(m =>weapon.Id == m.name); // Исправлено с Range на Melee
            if(weapon is RangeWeapon)
                obj = Range.FirstOrDefault(m =>weapon.Id == m.name); // Исправлено с Range на Melee
            if(weapon is UseableItem)
                obj = UseableItems.FirstOrDefault(m =>weapon.Id == m.name); // Исправлено с Range на Melee
            if(weapon is SecondaryWeapon && weapon is Shield)
                obj = Shields.FirstOrDefault(m =>weapon.Id == m.name); // Исправлено с Range на Melee
            if(weapon is SecondaryWeapon && weapon is Spellbook)
                obj = SpellBooks.FirstOrDefault(m =>weapon.Id == m.name); // Исправлено с Range на Melee
            

            var weaponContainer = Instantiate(obj).AddComponent<EntityContainer>();
            weaponContainer.name = obj.name;
            weaponContainer.SetEntity(weapon);
            return weaponContainer;
        }
        
        public BaseEntity GenerateWeapon(WeaponType weaponType, Rarity rarity, string name)
        {
            BaseEntity weaponModel = weaponType switch
            {
                WeaponType.MeeleWeapon => new MeeleWeapon(name),
                WeaponType.RangeWeapon => new RangeWeapon(name),
                WeaponType.UseableItems => new UseableItem(name),
                WeaponType.Shield => new Shield(name),
                WeaponType.SpellBook =>new Spellbook(name),
                _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, null)
            };

            weaponModel.Rarity = rarity;

            Effect[] effectsArray = weaponType == WeaponType.MeeleWeapon ? MeleeEffects : RangeEffects;
            int effectCount = (int)rarity;
            for (int i = 0; i < effectCount; i++)
            {
                weaponModel.Effects.Add(effectsArray[Random.Range(0, effectsArray.Length)]);
            }
            /*Debug.Log($"{effectCount}");
            Debug.Log($"{weaponModel.Effects.Count}");*/
            return weaponModel;
        }
    }
    public enum WeaponType
    {
        MeeleWeapon,
        RangeWeapon,
        UseableItems,
        Shield,
        SpellBook
    }
}