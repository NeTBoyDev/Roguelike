using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Develop.Core;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [SerializeField] private Image _dragPreviewImage;
    [HorizontalLine(2, EColor.Green)]
    [SerializeField] private InventoryView _view;
    [SerializeField] private InventoryModel _model;
    [HorizontalLine(2, EColor.Green)]

    [Header("Input Settings")]
    public KeyCode OpenInventoryKey = KeyCode.Tab;
    public KeyCode UseItemKey = KeyCode.E;
    public KeyCode PickItemKey = KeyCode.F;

    [Header("Kit start")]
    public List<Item> KitStartItems;


    private Item _dragableItem;
    private InventorySlot _lastInteractSlot;
    [SerializeField] private int _selectedHotbarIndex = 0;

    private CombatSystem CombatSystem;

    public static event Action<bool> OnInventoryStateChange;

    private void Start()
    {
        SubscribeEvents();
        CombatSystem = GetComponentInChildren<CombatSystem>();
        //KitStart();

        _dragPreviewImage.gameObject.SetActive(false);
        SelectHotbarSlot(0);
        /*var weapon = ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.RangeWeapon, Rarity.Common);
        SetWeapon(weapon.ContainedEntity as Weapon);*/
    }
    private void Update()
    {
        ToggleInventory();

        UpdatePreviewItem();

        HandleHotbarSelection();
        
        CheckItemsForPickUp();

        if (Input.GetKeyDown(UseItemKey))
        {
            Debug.Log($"Item = {_model.SelectedItem}, ItemCount = {(_model.SelectedItem != null ? _model.SelectedItem.Count : 0)}");
        }
    }

    private void CheckItemsForPickUp()
    {
        if (TryGetContainer(out var container) && Input.GetKeyDown(PickItemKey))
        {
            AddItem(container.ContainedEntity as Item);
            Destroy(container.gameObject);
        }
    }

    private bool TryGetContainer(out EntityContainer container)
    {
        var raycast = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward,
            out RaycastHit hit, 5);
        if (hit.collider != null && hit.collider.gameObject.TryGetComponent(out EntityContainer cont))
        {
            
            container = cont;
            return true;
        }

        container = null;
        return false;
    }

    private void HandleHotbarSelection()
    {
        var hotbarSlots = _view.HotbarSlots;
        if (hotbarSlots.Count <= 0)
            return;

        for (int i = 0; i < hotbarSlots.Count && i < 9; i++) //Ограничение до 9 слотов
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectHotbarSlot(i);
                return;
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel"); //CD
        if(scroll != 0)
        {
            int direction = scroll > 0 ? -1 : 1;
            int newIndex = _selectedHotbarIndex + direction;

            if (newIndex < 0) 
                newIndex = hotbarSlots.Count - 1;

            if (newIndex >= hotbarSlots.Count)
                newIndex = 0;

            SelectHotbarSlot(newIndex);
            
        }
    }

   private void SelectHotbarSlot(int index)
    {
        var hotbarSlots = _view.HotbarSlots;
        if (index < 0 || index >= hotbarSlots.Count) 
            return;

        _selectedHotbarIndex = index;
        Item selectedItem = hotbarSlots[index].Item;

        _model.SetSelectedItem(selectedItem);
        
        UpdateAllHotbarSlots();
    }

    private void ToggleInventory()
    {
        if (Input.GetKeyDown(OpenInventoryKey))
        {
            bool enabled = !_view.Inventory.activeInHierarchy;

            InventorySetAcitve(enabled);
            Cursor.visible = enabled;
            Cursor.lockState = enabled ? CursorLockMode.None : CursorLockMode.Locked;

            if (!enabled)
                ResetDrag();
            
            OnInventoryStateChange?.Invoke(enabled);
        }
    }

    private void UpdatePreviewItem()
    {
        if (_dragableItem != null)
        {
            _dragPreviewImage.transform.position = Input.mousePosition;
        }
    }

    private void SubscribeEvents()
    {
        foreach (var slot in _view.InventorySlots)
        {
            slot.onDrag += DragItem;
            slot.onDrop += DropItem;
            slot.onDrop += DropItemOutOfInventory;
            
        }

        foreach (var slot in _view.HotbarSlots)
        {
            slot.onDrag += DragItem;
            slot.onDrop += DropItem;
            slot.onDrop += DropItemOutOfInventory;
        }
    }

    private void DropItemOutOfInventory(PointerEventData data, Item item, InventorySlot slot)
    {
        
        if (!RectTransformUtility.RectangleContainsScreenPoint(_view.Inventory.GetComponent<RectTransform>(),data.position))
        {
            var container = ItemGenerator.Instance.GenerateContainer(item);
            container.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
            container.GetComponent<Rigidbody>().AddForce(Camera.main.transform.forward*3,ForceMode.Impulse);
            RemoveItem(slot);

            if (slot.SlotType == SlotType.Weapon)
            {
                CombatSystem.RemoveWeapon();
            }
        }
        
    }

    public void SetWeapon(Weapon weapon)
    {
        var weaponSlot = _view.InventorySlots.First(s => s.SlotType == SlotType.Weapon);
        weaponSlot.InitializeSlot(weapon);
        CombatSystem.SetWeapon(weapon);
    }

    public void SetSecondaryWeapon(SecondaryWeapon weapon)
    {
        var weaponSlot = _view.InventorySlots.First(s => s.SlotType == SlotType.SecondaryWeapon);
        weaponSlot.InitializeSlot(weapon);
        CombatSystem.SetSecondaryWeapon(weapon);
    }
    public void InventorySetAcitve(bool value, float delay = 0f) => _view.InventorySetActiveAsync(value, delay).Forget();


    #region Items
    public void AddItem(Item item)
    {
        if (item == null || item.Count <= 0)
            return;

        if (item.IsStackable)
        {
            int remainingCount = item.Count;

            foreach (var slot in _view.InventorySlots)
            {
                if (remainingCount <= 0)
                    break;

                if(!slot.IsEmpty() && slot.Item.Id == item.Id && slot.Item.Count < slot.Item.MaxStackSize && slot.Item.Rarity == item.Rarity)
                {
                    int availableSpace = slot.Item.MaxStackSize - slot.Item.Count;
                    int amountToAdd = Mathf.Min(availableSpace, remainingCount);

                    slot.Item.Count += amountToAdd;
                    slot.UpdateVisual();
                    remainingCount -= amountToAdd;
                }
            }
            //Если остались предметы, добавляем их в пустые слоты
            while(remainingCount > 0)
            {
                var emptySlot = _view.GetFirstEmptySlot();
                if(emptySlot == null || !IsValid(item,emptySlot))
                {
                    Debug.Log($"Невозможно добавить {remainingCount} предметов типа <color=cyan>{item.Id}</color>. Инвентарь полон!");
                    item.Count = remainingCount;
                    var container = ItemGenerator.Instance.GenerateContainer(item);
                    container.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
                    container.GetComponent<Rigidbody>().AddForce(Camera.main.transform.forward*3,ForceMode.Impulse);
                    
                    //ЛОгика выбрасывания предметов
                    break;
                }

                item.Count = Math.Min(remainingCount, item.MaxStackSize);
                remainingCount -= item.Count;

                _model.AddItem(item);
                emptySlot.InitializeSlot(item);
            }
        }
        else
        {
            //Логика для не стакуемых предметов
            for (int i = 0; i < item.Count; i++)
            {
                var emptySlot = _view.GetFirstEmptySlot();
                if (emptySlot == null || !IsValid(item,emptySlot))
                {
                    Debug.Log($"Невозможно добавить нестакуемый предмет типа <color=cyan>{item.Id}</color>. Инвентарь полон!");
                    //ЛОгика выбрасывания предметов
                    var container = ItemGenerator.Instance.GenerateContainer(item);
                    container.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
                    container.GetComponent<Rigidbody>().AddForce(Camera.main.transform.forward*3,ForceMode.Impulse);
                    break;
                }

                Item newItem = item; // ТУТ БЫЛ КЛОУН
                newItem.Count = 1;

                _model.AddItem(newItem);
                emptySlot.InitializeSlot(newItem);
            }
        }
        UpdateAllHotbarSlots();
    }

    public void RemoveItem(Item item)
    {
        var slot = _view.GetSlotWithItem(item);
        _view.ClearSlot(slot);

        _model.RemoveItem(item);
    }
    
    public void RemoveItem(InventorySlot slot)
    {
        var item = slot.Item;
        _view.ClearSlot(slot);

        _model.RemoveItem(item);
    }

    public Item FindItem(string Id) => _model.FindItem(Id);
    #endregion

    #region Drag&Drop

    private void DragItem(PointerEventData eventData, Item item, InventorySlot slot)
    {
        if (item == null) 
            return;

        _dragableItem = item;
        _lastInteractSlot = slot;

        _dragPreviewImage.sprite = item.Sprite;
        _dragPreviewImage.gameObject.SetActive(true);
    }
    bool IsValid(Item item1, InventorySlot targetSlot1)
    {
        Debug.Log($"type {targetSlot1.SlotType}. item {item1 is MeeleWeapon} ");

        if ((targetSlot1.SlotType == SlotType.Hotbar && item1 is UseableItem)
            || (targetSlot1.SlotType == SlotType.Weapon && (item1 is MeeleWeapon || item1 is RangeWeapon))
            || (targetSlot1.SlotType == SlotType.SecondaryWeapon && item1 is SecondaryWeapon)
            || (targetSlot1.SlotType == SlotType.Artifact && item1 is Artifact)
            || (targetSlot1.SlotType == SlotType.Default)
            || item1 == null)
        {
            return true;
        }

        return false;
    }
    private void DropItem(PointerEventData eventData, Item item, InventorySlot targetSlot)
    {
        

        if (_dragableItem == null || _lastInteractSlot == null || targetSlot == _lastInteractSlot 
            || (targetSlot.SlotType == SlotType.Hotbar && _dragableItem is not UseableItem)
            || (targetSlot.SlotType == SlotType.Weapon && (_dragableItem is not Weapon && _dragableItem is not RangeWeapon))
            || (targetSlot.SlotType == SlotType.SecondaryWeapon && _dragableItem is not SecondaryWeapon)
            || (targetSlot.SlotType == SlotType.Artifact && _dragableItem is not Artifact))
        {
            ResetDrag();
            return;
        }
        if (!IsValid(targetSlot.Item, _lastInteractSlot))
        {
            ResetDrag();
            return;
        }

        //Добавить проверку на типы слотов
        if (!IsValid(_lastInteractSlot.Item, targetSlot) && !IsValid(targetSlot.Item, _lastInteractSlot) || 
            (!IsValid(_lastInteractSlot.Item, targetSlot)))
        {
            ResetDrag();
            return;
        }

        if ((_dragableItem is MeeleWeapon || _dragableItem is RangeWeapon) && targetSlot.SlotType == SlotType.Weapon)
        {
            CombatSystem.SetWeapon((Weapon)_dragableItem);
        }
        if (_lastInteractSlot.SlotType == SlotType.Weapon)
        {
            CombatSystem.RemoveWeapon();
            if (targetSlot.Item is Weapon w)
            {
                CombatSystem.SetWeapon(w);
            }
        }
        
        if ((_dragableItem is Shield || _dragableItem is Spellbook) && targetSlot.SlotType == SlotType.SecondaryWeapon)
        {
            CombatSystem.SetSecondaryWeapon((SecondaryWeapon)_dragableItem);
        }
        if (_lastInteractSlot.SlotType == SlotType.SecondaryWeapon)
        {
            CombatSystem.RemoveSecondaryWeapon();
            if (targetSlot.Item is SecondaryWeapon w)
            {
                CombatSystem.SetSecondaryWeapon(w);
            }
        }


        //Если слот пустой
        if (targetSlot.IsEmpty())
        {
            targetSlot.InitializeSlot(_dragableItem);
            _lastInteractSlot.ClearSlot();
        }
        //Если предметы стакаются и одинаковые
        else if (_dragableItem.IsStackable && targetSlot.Item.Id == _dragableItem.Id)
        {
            if(targetSlot.Item.Count == targetSlot.Item.MaxStackSize || _dragableItem.Count == _dragableItem.MaxStackSize 
                                                                     || _dragableItem.Rarity != targetSlot.Item.Rarity)
            {
                var item1 = targetSlot.Item;
                var item2 = _dragableItem;

                var slot1 = targetSlot;
                var slot2 = _lastInteractSlot;
                
                

                slot1.InitializeSlot(item2);
                slot2.InitializeSlot(item1);

                slot1.UpdateVisual();
                slot2.UpdateVisual();
                
                if (slot1.SlotType == SlotType.Hotbar || slot2.SlotType == SlotType.Hotbar)
                {
                    //SelectHotbarSlot( _view.HotbarSlots.IndexOf(targetSlot));
                    SelectHotbarSlot(_selectedHotbarIndex);
                }

                ResetDrag();
                return;
            }

            int newStackCount = targetSlot.Item.Count + _dragableItem.Count;
            int overflow = Mathf.Max(0, newStackCount - targetSlot.Item.MaxStackSize);

            targetSlot.Item.Count = Mathf.Min(newStackCount, targetSlot.Item.MaxStackSize);
            targetSlot.UpdateVisual();
            
            

            if (overflow > 0)
            {
                _dragableItem.Count = overflow;
                _lastInteractSlot.UpdateVisual();
            }
            else
            {
                _lastInteractSlot.ClearSlot();
            }
            
            
        }
        //Если слот занят и предметы разные - свап
        else if (!targetSlot.IsEmpty())
        {
            Item targetItem = targetSlot.Item;
            targetSlot.InitializeSlot(_dragableItem);

            _lastInteractSlot.InitializeSlot(targetItem);
        }

        //Hotbar
        if(_lastInteractSlot.SlotType == SlotType.Hotbar && _lastInteractSlot.IsEmpty())
        {
            int clearedIndex = _view.HotbarSlots.IndexOf(_lastInteractSlot);
            if(clearedIndex == _selectedHotbarIndex)
            {
                //_selectedHotbarIndex = 0;
                _model.SetSelectedItem(null);
            }
        }
        Debug.Log($"target slot {_view.HotbarSlots.IndexOf(targetSlot)}, selected slot {_selectedHotbarIndex}");
        if (targetSlot.SlotType == SlotType.Hotbar && _view.HotbarSlots.IndexOf(targetSlot) == _selectedHotbarIndex)
        {
            //Debug.Log($"put item in {_selectedHotbarIndex}, count {_dragableItem.Count}");
            _model.SetSelectedItem(_dragableItem);
        }
        if (targetSlot.SlotType == SlotType.Hotbar || _lastInteractSlot.SlotType == SlotType.Hotbar)
        {
            //SelectHotbarSlot( _view.HotbarSlots.IndexOf(targetSlot));
            SelectHotbarSlot(_selectedHotbarIndex);
        }
        ResetDrag();
    }
    private void ResetDrag()
    {
        Debug.Log("Reset drag");
        _dragableItem = null;
        _lastInteractSlot = null;
        _dragPreviewImage.gameObject.SetActive(false);
    }

    public void UpdateAllHotbarSlots()
    {
        var hotbarSlots = _view.HotbarSlots;

        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            hotbarSlots[i].UpdateVisual(true);
            hotbarSlots[i].SetSelected(i == _selectedHotbarIndex);
        }
    }
    #endregion
}