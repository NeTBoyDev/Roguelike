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

        //public GameObject[] Melee;

        #region Melee

        public GameObject[] Swords;
        public GameObject[] Axes;
        public GameObject[] Hammers;
        public GameObject[] Daggers;

        #endregion
        
        #region Range

        public GameObject[] Staves;
        public GameObject[] CrossBows;

        #endregion
        
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

        public EntityContainer GenerateWeaponGameobject(WeaponType weaponType, Rarity rarity, bool isReloadable = false)
        {
            var weapon = GenerateContainer(weaponType,isReloadable);
        
            // Создаём модель оружия в зависимости от типа
            BaseEntity weaponModel = GenerateWeapon(weaponType, rarity, weapon.name);

            weapon.SetEntity(weaponModel);
            return weapon;
        }

        public EntityContainer GenerateRandomGameobject()
        {
            var array = System.Enum.GetValues(typeof(WeaponType))
                .Cast<WeaponType>()
                .ToArray();
            var rarity = System.Enum.GetValues(typeof(Rarity))
                .Cast<Rarity>().OrderBy(v=>Random.value)
                .ToArray()[0];
            
            var item = array[Random.Range(0, array.Length)];
            return GenerateWeaponGameobject(item, rarity);
        }
        
        public EntityContainer GenerateRandomGameobject(Rarity r)
        {
            var array = System.Enum.GetValues(typeof(WeaponType))
                .Cast<WeaponType>()
                .ToArray();
            
            var item = array[Random.Range(0, array.Length)];
            return GenerateWeaponGameobject(item, r);
        }
        

        public EntityContainer GenerateContainer(WeaponType weaponType,bool isReloadable)
        {
            GameObject weapon = null;
            var value = Random.value;
            
            /*if (weaponType == WeaponType.MeeleWeapon)
                weaponType = GetRandomFlagFromGroup(WeaponType.MeeleWeapon);
            if (weaponType == WeaponType.RangeWeapon)
                weaponType = GetRandomFlagFromGroup(WeaponType.RangeWeapon);*/
            
            weapon = weaponType switch
            {
                WeaponType.UseableItems => UseableItems[Random.Range(0,UseableItems.Length)],
                WeaponType.Shield => Shields[Random.Range(0,Shields.Length)],
                WeaponType.SpellBook =>SpellBooks[Random.Range(0,SpellBooks.Length)],
                WeaponType.Sword => Swords[Random.Range(0,Swords.Length)],
                WeaponType.Dagger => Daggers[Random.Range(0,Daggers.Length)],
                WeaponType.Axe => Axes[Random.Range(0,Axes.Length)],
                WeaponType.Staff => Staves[Random.Range(0,Staves.Length)],
                WeaponType.Crossbow => CrossBows[Random.Range(0,CrossBows.Length)],
                WeaponType.Hammer => Hammers[Random.Range(0,Hammers.Length)],
                _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, null)
            };

            var weaponObject = Instantiate(weapon);
            var weaponContainer = weaponObject.AddComponent<EntityContainer>();
            weaponContainer.name = weapon.name;
            weaponContainer.View = weapon;
            return weaponContainer;
        }
        public EntityContainer GenerateContainer(Item weapon,bool hasView = false)
        {
            Debug.Log($"Generate drop item with view {weapon.View.name}");
            GameObject obj = null;
            if(weapon is MeeleWeapon)
                obj = Swords.Concat(Axes).Concat(Hammers).Concat(Daggers).FirstOrDefault(m =>weapon.Id == m.name); // Исправлено с Range на Melee
            if(weapon is RangeWeapon)
                obj = Staves.Concat(CrossBows).FirstOrDefault(m =>weapon.Id == m.name); // Исправлено с Range на Melee
            if(weapon is UseableItem)
                obj = UseableItems.FirstOrDefault(m =>weapon.Id == m.name); // Исправлено с Range на Melee
            if(weapon is SecondaryWeapon && weapon is Shield)
                obj = Shields.FirstOrDefault(m =>weapon.Id == m.name); // Исправлено с Range на Melee
            if(weapon is SecondaryWeapon && weapon is Spellbook)
                obj = SpellBooks.FirstOrDefault(m =>weapon.Id == m.name); // Исправлено с Range на Melee
            
            var weaponContainer = Instantiate(weapon.View).AddComponent<EntityContainer>();
            weaponContainer.name = obj.name;
            weaponContainer.SetEntity(weapon,hasView);
            Debug.Log($"Generate drop item with view {weapon == null}");
            Debug.Log($"Generate drop item with view {weapon.View == null}");
            return weaponContainer;
        }
        
        public BaseEntity GenerateWeapon(WeaponType weaponType, Rarity rarity, string name)
        {
            //Добавить смену типа рандомную если тип ближнее или дальнее оружие

            /*
            if (weaponType == WeaponType.MeeleWeapon)
                weaponType = GetRandomFlagFromGroup(WeaponType.MeeleWeapon);
            if (weaponType == WeaponType.RangeWeapon)
                weaponType = GetRandomFlagFromGroup(WeaponType.RangeWeapon);*/
            
            BaseEntity weaponModel = weaponType switch
            {
                WeaponType.UseableItems => new UseableItem(name),
                WeaponType.Shield => new Shield(name),
                WeaponType.SpellBook =>new Spellbook(name),
                WeaponType.Sword => new Sword(name),
                WeaponType.Dagger => new Dagger(name),
                WeaponType.Axe => new Axe(name),
                WeaponType.Staff => new Staff(name),
                WeaponType.Crossbow => new Crossbow(name),
                WeaponType.Hammer => new Hammer(name),
                _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, null)
            };

            weaponModel.Rarity = rarity;

            Effect[] effectsArray = weaponModel is MeeleWeapon ? MeleeEffects : RangeEffects;
            int effectCount = (int)rarity;
            for (int i = 0; i < effectCount; i++)
            {
                weaponModel.Effects.Add(effectsArray[Random.Range(0, effectsArray.Length)]);
            }
            /*Debug.Log($"{effectCount}");
            Debug.Log($"{weaponModel.Effects.Count}");*/
            return weaponModel;
        }
        /*public BaseEntity GenerateWeapon(WeaponType weaponType, Rarity rarity, string name, bool isReloadable = false)
        {
            BaseEntity weaponModel = weaponType switch
            {
                WeaponType.MeeleWeapon => new MeeleWeapon(name),
                WeaponType.RangeWeapon => new RangeWeapon(name),
                WeaponType.UseableItems => new UseableItem(name),
                WeaponType.Shield => new Shield(name),
                WeaponType.SpellBook =>new Spellbook(name),
                WeaponType.Sword => new Sword(name),
                WeaponType.Dagger => new Dagger(name),
                WeaponType.Axe => new Axe(name),
                WeaponType.Staff => new Staff(name),
                WeaponType.Crossbow => new Crossbow(name),
                WeaponType.Hammer => new Hammer(name),
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
            Debug.Log($"{weaponModel.Effects.Count}");#1#
            return weaponModel;
        }*/
        static WeaponType GetRandomFlagFromGroup(WeaponType group)
        {
            
            // Получаем все возможные флаги из enum
            var allFlags = System.Enum.GetValues(typeof(WeaponType))
                .Cast<WeaponType>()
                //.Where(f =>(group & f) == f) // Фильтруем по группе
                .ToArray();

            // Выбираем случайный флаг
            return allFlags[Random.Range(0, allFlags.Length)];
        }
    }
    [Flags]
    public enum WeaponType
    {
        UseableItems = 1,
        Shield = 2,
        SpellBook = 4,
        Sword = 8,
        Dagger = 16,
        Axe = 32,
        Hammer = 64,
        Staff = 128,
        Crossbow = 256,
        /*RangeWeapon = Crossbow | Staff ,
        MeeleWeapon = Sword | Dagger | Axe | Hammer,*/
        
    }
}