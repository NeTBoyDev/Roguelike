using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System;
using System.Collections.Generic;
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
    public KeyCode OpenInventoryKey;
    public KeyCode UseItemKey;

    [Header("Kit start")]
    public List<ItemTest> KitStartItems;


    private ItemTest _dragableItem;
    private InventorySlot _lastInteractSlot;
    [SerializeField]  private int _selectedHotbarIndex = -1; // -1 = ничего не выбрано

    private void Start()
    {
        SubscibeEvents();
        KitStart();

        _dragPreviewImage.gameObject.SetActive(false);
    }
    private void Update()
    {
        ToggleInventory();

        UpdatePreviewItem();

        HandleHotbarSelection();
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

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if(scroll != 0)
        {
            Debug.Log(scroll);
            int direction = scroll > 0 ? 1 : -1;
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
        ItemTest selectedItem = hotbarSlots[index].Item;

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
        }
    }

    private void UpdatePreviewItem()
    {
        if (_dragableItem != null)
        {
            _dragPreviewImage.transform.position = Input.mousePosition;
        }
    }

    private void SubscibeEvents()
    {
        foreach (var slot in _view.InventorySlots)
        {
            slot.onDrag += DragItem;
            slot.onDrop += DropItem;
        }

        foreach (var slot in _view.HotbarSlots)
        {
            slot.onDrag += DragItem;
            slot.onDrop += DropItem;
        }
    }
    public void InventorySetAcitve(bool value, float delay = 0f) => _view.InventorySetActiveAsync(value, delay).Forget();


    #region Items
    private void KitStart()
    {
        foreach (var item in KitStartItems)
        {
            AddItem(item);
        }

        UpdateAllHotbarSlots();

        SelectHotbarSlot(0);
    }

    public void AddItem(ItemTest item)
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

                if(!slot.IsEmpty() && slot.Item.Id == item.Id && slot.Item.Count < slot.Item.MaxStackSize)
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
                if(emptySlot == null)
                {
                    Debug.Log($"Невозможно добавить {remainingCount} предметов типа <color=cyan>{item.Id}</color>. Инвентарь полон!");
                    DropItem(item, remainingCount);
                    break;
                }

                ItemTest newItem = item.Clone();
                newItem.Count = Math.Min(remainingCount, item.MaxStackSize);
                remainingCount -= newItem.Count;

                _model.AddItem(newItem);
                emptySlot.InitializeSlot(newItem);
            }
        }
        else
        {
            //Логика для не стакуемых предметов
            for (int i = 0; i < item.Count; i++)
            {
                var emptySlot = _view.GetFirstEmptySlot();
                if (emptySlot == null)
                {
                    Debug.Log($"Невозможно добавить нестакуемый предмет типа <color=cyan>{item.Id}</color>. Инвентарь полон!");
                    DropItem(item);
                    break;
                }

                ItemTest newItem = item.Clone();
                newItem.Count = 1;

                _model.AddItem(newItem);
                emptySlot.InitializeSlot(newItem);
            }
        }
        UpdateAllHotbarSlots();
    }

    //После слияния веток сделать логику
    public void DropItem(ItemTest item, int amount = 1)
    {

    }

    public void RemoveItem(ItemTest item)
    {
        var slot = _view.GetSlotWithItem(item);
        _view.ClearSlot(slot);

        _model.RemoveItem(item);
    }

    public ItemTest FindItem(string Id) => _model.FindItem(Id);
    #endregion

    #region Drag&Drop

    private void DragItem(PointerEventData eventData, ItemTest item, InventorySlot slot)
    {
        if (item == null) 
            return;

        _dragableItem = item;
        _lastInteractSlot = slot;

        _dragPreviewImage.sprite = item.Sprite;
        _dragPreviewImage.gameObject.SetActive(true);
    }
    private void DropItem(PointerEventData eventData, ItemTest item, InventorySlot targetSlot)
    {
        if (_dragableItem == null || _lastInteractSlot == null || targetSlot == _lastInteractSlot)
        {
            ResetDrag();
            return;
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
            ItemTest targetItem = targetSlot.Item;
            targetSlot.InitializeSlot(_dragableItem);

            _lastInteractSlot.InitializeSlot(targetItem);
        }

        if (_view.HotbarSlots.Contains(targetSlot))
        {
            if (_selectedHotbarIndex == _view.HotbarSlots.IndexOf(targetSlot))
            {
                SelectHotbarSlot(_selectedHotbarIndex);
            }
        }
        else if(_view.HotbarSlots.Contains(_lastInteractSlot) && _lastInteractSlot.IsEmpty())
        {
            int clearedIndex = _view.HotbarSlots.IndexOf(_lastInteractSlot);
            if(clearedIndex == _selectedHotbarIndex)
            {
                _selectedHotbarIndex = -1;
                _model.SetSelectedItem(null);
            }
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


[Serializable]
public class ItemTest
{
    [field: SerializeField] public string Id { get; private set; }
    [field: SerializeField] public Sprite Sprite { get; private set; }

    [field: SerializeField, ResizableTextArea, AllowNesting] 
    public string Description {  get; private set; }

    [field: SerializeField] public bool IsStackable { get; private set; }

    [field: SerializeField, MinValue(1), AllowNesting]
    public int Count { get;  set; } = 1;

    [field: Tooltip("Для одинаковых предметов MaxStackSize должен быть одинаковым")]
    [field: SerializeField, MinValue(1), MaxValue(1000), ShowIf(nameof(IsStackable)), AllowNesting] 
    public int MaxStackSize { get; private set; } = 99;

    public ItemTest Clone()
    {
        return new ItemTest
        {
            Id = this.Id,
            Sprite = this.Sprite,
            Description = this.Description,
            IsStackable = this.IsStackable,
            Count = this.Count,
            MaxStackSize = this.MaxStackSize
        };
    }
}