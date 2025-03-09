using System;
using System.Linq;
using _Project.Develop.Core.Base;
using _Project.Develop.Core.Effects;
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Effects.SpellEffects;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Entities.Potions;
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
        public GameObject[] CrossbowProjectile,SpellProjectile;

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

        public UseableItem[] Potions;

        private void OnEnable()
        {
            Potions = new UseableItem[]
            {
                new HealingPotion("Heal"),
                new AgilityPotions("Agility"),
                new RagePotion("Rage"),
                new WisdomPotion("Wisdom")
            };
        }

        public EntityContainer GenerateWeaponGameobject(WeaponType weaponType, Rarity rarity)
        {
            EntityContainer weapon;
            if (weaponType is WeaponType.UseableItems)
            {
                var potion = GetRandomPotion();
                weapon = GenerateContainer(weaponType, potion.Id);
                Debug.Log(potion.Id);
            }
            else
                weapon = GenerateContainer(weaponType);
        
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
        

        public EntityContainer GenerateContainer(WeaponType weaponType)
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
                WeaponType.Gem => ModificatorObjects[Random.Range(0,ModificatorObjects.Length)],
                WeaponType.Artifact => Artifacts[Random.Range(0,Artifacts.Length)],
                _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, null)
            };

            var weaponObject = Instantiate(weapon);
            var weaponContainer = weaponObject.AddComponent<EntityContainer>();
            weaponContainer.name = weapon.name;
            weaponContainer.View = weapon;
            return weaponContainer;
        }
        
        
        public EntityContainer GenerateContainer(WeaponType weaponType,string name)
        {
            GameObject weapon = null;
            var value = Random.value;
            
            /*if (weaponType == WeaponType.MeeleWeapon)
                weaponType = GetRandomFlagFromGroup(WeaponType.MeeleWeapon);
            if (weaponType == WeaponType.RangeWeapon)
                weaponType = GetRandomFlagFromGroup(WeaponType.RangeWeapon);*/
            
            weapon = weaponType switch
            {
                WeaponType.UseableItems => UseableItems.FirstOrDefault(i=>i.name == name),
                WeaponType.Shield => Shields.FirstOrDefault(i=>i.name == name),
                WeaponType.SpellBook =>SpellBooks.FirstOrDefault(i=>i.name == name),
                WeaponType.Sword => Swords.FirstOrDefault(i=>i.name == name),
                WeaponType.Dagger => Daggers.FirstOrDefault(i=>i.name == name),
                WeaponType.Axe => Axes.FirstOrDefault(i=>i.name == name),
                WeaponType.Staff => Staves.FirstOrDefault(i=>i.name == name),
                WeaponType.Crossbow => CrossBows.FirstOrDefault(i=>i.name == name),
                WeaponType.Hammer => Hammers.FirstOrDefault(i=>i.name == name),
                WeaponType.Gem => ModificatorObjects.FirstOrDefault(i=>i.name == name),
                WeaponType.Artifact => Artifacts.FirstOrDefault(i=>i.name == name),
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
            if(weapon is Gem)
                obj = ModificatorObjects.FirstOrDefault(m =>weapon.Id == m.name); // Исправлено с Range на Melee
            if(weapon is Artifact)
                obj = Artifacts.FirstOrDefault(m =>weapon.Id == m.name); // Исправлено с Range на Melee
            
            var weaponContainer = Instantiate(weapon.View).AddComponent<EntityContainer>();
            weaponContainer.name = obj.name;
            weaponContainer.SetEntity(weapon,hasView);
            Debug.Log($"Generate drop item with view {weapon == null}");
            Debug.Log($"Generate drop item with view {weapon.View == null}");
            return weaponContainer;
        }

        private UseableItem GetRandomPotion()
        {
            var value = Random.value;
            if (value < .25f)
                return new HealingPotion("Healing potion");
            if (value < .5f)
                return new WisdomPotion("Potion of wisdom");
            if (value < .75f)
                return new RagePotion("Potion of rage");
            return new AgilityPotions("Potion of haste");
        }
        private UseableItem GetPotion(string name)
        {
            var value = Random.value;
            if (name == "Healing potion")
                return new HealingPotion("Healing potion");
            if (name == "Potion of wisdom")
                return new WisdomPotion("Potion of wisdom");
            if (name == "Potion of rage")
                return new RagePotion("Potion of rage");
            return new AgilityPotions("Potion of haste");
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
                WeaponType.UseableItems =>GetPotion(name),
                WeaponType.Shield => new Shield(name,rarity),
                WeaponType.SpellBook =>new Spellbook(name,rarity),
                WeaponType.Sword => new Sword(name,rarity),
                WeaponType.Dagger => new Dagger(name,rarity),
                WeaponType.Axe => new Axe(name,rarity),
                WeaponType.Staff => new Staff(name,rarity),
                WeaponType.Crossbow => new Crossbow(name,rarity),
                WeaponType.Hammer => new Hammer(name,rarity),
                WeaponType.Gem => new Gem(name),
                WeaponType.Artifact => new Artifact(name),
                
                _ => throw new ArgumentOutOfRangeException(nameof(weaponType), weaponType, null)
            };

            weaponModel.Rarity = rarity;

            Effect[] effectsArray;
            int effectCount = (int)rarity;
            var count = Random.Range(0, effectCount);
            
            if (weaponModel is MeeleWeapon || weaponModel is RangeWeapon)
            {
                effectsArray = weaponModel is MeeleWeapon ? MeleeEffects : RangeEffects;
                for (int i = 0; i < count; i++)
                {
                    weaponModel.Effects.Add(effectsArray[Random.Range(0, effectsArray.Length)]);
                }
            }
            else if(weaponModel is Gem gem)
            {
                effectsArray = Random.value > .5f ? MeleeEffects : RangeEffects;
                var effect = effectsArray[Random.Range(0, effectsArray.Length)];
                if(effect is SpellEffect e)
                    e.SetMagnitude((int)rarity);
                weaponModel.Effects.Add(effect);
                if (gem.Rarity == Rarity.Legendary)
                    gem.AddProjectile(GetRandomProjectile());
            }
            else if (weaponModel is Artifact || weaponModel is SecondaryWeapon)
            {
                for (int i = 0; i < (int)weaponModel.Rarity; i++)
                {
                    var stat = (StatType)Random.Range(1, 4);
                    if (weaponModel.Stats.ContainsKey(stat))
                    {
                        weaponModel.Stats[stat].Modify((int)weaponModel.Rarity);
                    }
                    else
                    {
                        weaponModel.Stats[stat]= new Stat(stat,(int)weaponModel.Rarity,16);
                    }
                }
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

        public Projectile GetRandomProjectile()
        {
            var array = CrossbowProjectile.Concat(SpellProjectile).ToArray();
            return new Projectile(array[Random.Range(0, array.Length)].name);
        }
        public Projectile GetRandomProjectile(WeaponType type)
        {
            var array = type == WeaponType.Crossbow ? CrossbowProjectile: SpellProjectile;
            return new Projectile(array[Random.Range(0, array.Length)].name);
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
        Gem,
        Artifact
        /*RangeWeapon = Crossbow | Staff ,
        MeeleWeapon = Sword | Dagger | Axe | Hammer,*/
        
    }
}