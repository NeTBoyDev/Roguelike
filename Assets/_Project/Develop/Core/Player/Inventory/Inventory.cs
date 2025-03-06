using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System;
using _Project.Develop.Core;
using _Project.Develop.Core.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using TMPro;

public class Inventory : MonoBehaviour
{
    [SerializeField] private Image _dragPreviewImage;
    
    [SerializeField] private RectTransform _itemInfoPanel;
    [SerializeField] private TMP_Text _itemNameText;
    [SerializeField] private TMP_Text _itemStatText;
    [SerializeField] private TMP_Text _itemEffectText;
    [SerializeField] public TMP_Text _itemDescriptionText;
    
    [SerializeField] private TMP_Text _playerStatsText;
        
    [field: HorizontalLine(2, EColor.Green)]
    [field: SerializeField] public InventoryView View { get; private set; } = null;
    [field: SerializeField] public InventoryModel Model { get; private set; } = null;
    [HorizontalLine(2, EColor.Green)]
    [Header("Input Settings")]
    public KeyCode OpenInventoryKey = KeyCode.Tab;
    public KeyCode UseItemKey = KeyCode.E;
    public KeyCode PickItemKey = KeyCode.F; //And open vendorPanel
    public KeyCode CloseKey = KeyCode.Escape;

    public Item DragableItem { get; private set; } = null;
    public InventorySlot LastInteractSlot { get; private set; } = null;
    public int SelectedHotbarIndex { get; private set; }  = 0;
    public CombatSystem CombatSystem { get; private set; } = null;

    private bool InventoryState;

    public static event Action<bool> OnInventoryStateChange;

    [field: Tooltip("Item pickup & vendor interact distance")]
    [field: SerializeField] public float InteractionDistance { get; private set; } = 5f;
    [SerializeField,ReadOnly] private Vendor _currentVendor = null;

    #region Initialize
    private void Start()
    {
        CombatSystem = GetComponentInChildren<CombatSystem>();
        Initialize();

        KitStart();
    }

    private void Initialize()
    {
        SubscribeEvents();
        _dragPreviewImage.gameObject.SetActive(false);

        SelectHotbarSlot(0);
    }

    private void SubscribeEvents()
    {
        foreach (var slot in View.GetAllConcatSlots())
        {
            slot.onDrag += DragItem;
            slot.onDrop += DropItem;
            slot.onDrop += DropItemOutOfInventory;

            slot.onPointerEnter += ShowItemInfo;
            slot.onPointerExit += CloseItemInfo;
        }

        OnInventoryStateChange += value =>
        {
            InventoryState = value;
            if (!value)
                CloseItemInfo();
        };
    }
    private void KitStart()
    {

    }
    #endregion 

    #region Handlers
    private void Update()
    {
        HandleInput();
        UpdatePreviewItem();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(OpenInventoryKey)) ToggleInventory();
        if (Input.GetKeyDown(PickItemKey)) TryPickUpItem();
        if (Input.GetKeyDown(UseItemKey)) LogSelectedItem();

        if (Input.GetKeyDown(CloseKey) || Input.GetKeyDown(OpenInventoryKey) && _currentVendor != null) CloseVendorInterface();

        TryOpenVendor();

        CheckForItem();
        HandleHotbarInput();
    }

    private void TryOpenVendor()
    {
        Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, InteractionDistance);

        if (Input.GetKeyDown(PickItemKey) && hit.collider != null && hit.collider.TryGetComponent(out Vendor vendor))
        {
            if (_currentVendor == vendor)
            {
                CloseVendorInterface();
            }
            else
            {
                OpenVendorInterface(vendor, Vendor.TradeMode.Sell);
            }
        }
        else if (_currentVendor != null && Vector3.Distance(transform.GetChild(0).position, _currentVendor.transform.position) > _currentVendor.VendorCloseDistance)
        {
            CloseVendorInterface();
        }
    }

    private void CheckForItem()
    {
        Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, InteractionDistance);


        if (hit.collider != null && hit.collider.TryGetComponent(out EntityContainer cont))
        {
            ShowItemInfo((Item)cont.ContainedEntity);
        }
        else if(!InventoryState)
            CloseItemInfo();
    }

    private void TryPickUpItem()
    {
        if (TryGetContainer(out var container))
        {
            AddItem(container.ContainedEntity as Item);
            Destroy(container.gameObject);
        }

    }

    private bool TryGetContainer(out EntityContainer container)
    {
        Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, InteractionDistance);

        if (hit.collider != null && hit.collider.TryGetComponent(out EntityContainer cont))
        {
            container = cont;
            return true;
        }
        container = null;
        return false;
    }

    private void HandleHotbarInput()
    {
        var hotbarSlots = View.HotbarSlots;
        if (hotbarSlots.Count == 0) return;

        HandleNumberKeySelection(hotbarSlots);
        HandleScrollWheelSelection(hotbarSlots);
    }

    private void HandleNumberKeySelection(List<InventorySlot> hotbarSlots)
    {
        for (int i = 0; i < Math.Min(hotbarSlots.Count, 9); i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectHotbarSlot(i);
                return;
            }
        }
    }

    private void HandleScrollWheelSelection(List<InventorySlot> hotbarSlots)
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll == 0) return;

        int direction = scroll > 0 ? -1 : 1;
        int newIndex = (SelectedHotbarIndex + direction + hotbarSlots.Count) % hotbarSlots.Count;
        SelectHotbarSlot(newIndex);
    }

    public void SelectHotbarSlot(int index)
    {
        var hotbarSlots = View.HotbarSlots;
        if (index < 0 || index >= hotbarSlots.Count) return;

        SelectedHotbarIndex = index;
        Model.SetSelectedItem(hotbarSlots[index].Item);
        UpdateAllHotbarSlots();
    }

    private void ToggleInventory()
    {
        bool enabled = !View.Inventory.activeInHierarchy;

        if (_currentVendor != null)
            return;

        InventorySetAcitve(enabled);
        UpdateCursorState(enabled);
        ResetDragIfClosing(!enabled);

        OnInventoryStateChange?.Invoke(enabled);
    }

    public void UpdateCursorState(bool enabled)
    {
        Cursor.visible = enabled;
        Cursor.lockState = enabled ? CursorLockMode.None : CursorLockMode.Locked;
    }

    private void UpdatePreviewItem()
    {
        if (DragableItem != null)
            _dragPreviewImage.transform.position = Input.mousePosition;
    }
    public void InventorySetAcitve(bool value, float delay = 0f)
    {
        if (View.Inventory.activeInHierarchy == value) return;

        View.InventorySetActiveAsync(value, delay).Forget();
    }

    private void LogSelectedItem() => Debug.Log($"Item = {Model.SelectedItem}, ItemCount = {(Model.SelectedItem?.Count ?? 0)}");
    #endregion

    #region Item Management
    public void AddItem(Item item) => InventoryItemManager.AddItem(item, View, Model, CombatSystem);
    public void RemoveItem(Item item) => InventoryItemManager.RemoveItem(item, View, Model);
    public void RemoveItem(InventorySlot slot) => InventoryItemManager.RemoveItem(slot, View, Model);
    public Item FindItem(string Id) => Model.FindItem(Id);
    #endregion

    #region Drag & Drop
    public void DragItem(PointerEventData eventData, Item item, InventorySlot slot)
    {
        if (item == null) return;

        DragableItem = item;
        LastInteractSlot = slot;
        _dragPreviewImage.sprite = item.Sprite;
        _dragPreviewImage.gameObject.SetActive(true);
    }

    public void DropItem(PointerEventData eventData, Item item, InventorySlot targetSlot) =>
        InventoryDragDropHandler.HandleDrop(eventData, item, targetSlot, this);

    public void ResetDrag()
    {
        DragableItem = null;
        LastInteractSlot = null;
        _dragPreviewImage.gameObject.SetActive(false);
    }

    private void ResetDragIfClosing(bool isInventoryOpen)
    {
        if (!isInventoryOpen) ResetDrag();
    }

    public void UpdateAllHotbarSlots()
    {
        var hotbarSlots = View.HotbarSlots;
        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            hotbarSlots[i].UpdateVisual();
            hotbarSlots[i].SetSelected(i == SelectedHotbarIndex);
        }
    }

    private void DropItemOutOfInventory(PointerEventData data, Item item, InventorySlot slot)
    {
        if (!RectTransformUtility.RectangleContainsScreenPoint(View.Inventory.GetComponent<RectTransform>(), data.position) &&
            !VendorIsActive())
        {
            DropItemInWorld(item);
            RemoveItem(slot);

            if (slot.SlotType == SlotType.Hotbar && View.HotbarSlots.IndexOf(slot) == SelectedHotbarIndex)
            {
                Model.SetSelectedItem(null);
            }

            if (slot.SlotType == SlotType.Weapon)
                CombatSystem.RemoveWeapon();
            else if (slot.SlotType == SlotType.SecondaryWeapon)
                CombatSystem.RemoveSecondaryWeapon();
            //etc.

            ResetDrag();
            UpdateAllHotbarSlots();
        }
    }

    private bool VendorIsActive() => _currentVendor != null && _currentVendor.IsVendorOpen();

    private void DropItemInWorld(Item item)
    {
        var container = ItemGenerator.Instance.GenerateContainer(item);
        container.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
        container.GetComponent<Rigidbody>().AddForce(Camera.main.transform.forward * 3, ForceMode.Impulse);
    }
    #endregion

    public void ShowItemInfo(Item item)
    {
        _itemInfoPanel.gameObject.SetActive(true);
        _itemNameText.text = item.Id;
        string stats = string.Empty;
        foreach (var stat in item.Stats)
        {
            stats += $"{stat.Key}:{stat.Value.CurrentValue}\n";
        }

        _itemStatText.text = stats;
        
        string effects = string.Empty;
        foreach (var effect in item.Effects)
        {
            print(effect.Name);
            effects += $"{effect.Name}\n";
        }

        _itemEffectText.text = effects;
        _itemDescriptionText.text = "No description yet";
    }

    public void CloseItemInfo()
    {
        if (_itemInfoPanel == null)
            return;
        _itemInfoPanel.gameObject.SetActive(false);
    }

    public void OpenVendorInterface(Vendor vendor, Vendor.TradeMode mode)
    {
        if(_currentVendor != null && _currentVendor != vendor)
        {
            CloseVendorInterface();
        }
        _currentVendor = vendor;
        _currentVendor.OpenVendor(mode);

        InventorySetAcitve(true);
        UpdateCursorState(true);

        OnInventoryStateChange?.Invoke(true);
    }
    public void CloseVendorInterface()
    {
        if (_currentVendor != null)
        {
            _currentVendor.CloseVendor();
            _currentVendor = null;

            InventorySetAcitve(false);
            UpdateCursorState(false);

            OnInventoryStateChange?.Invoke(false);
        }
    }
}

public static class InventoryItemManager
{
    public static void AddItem(Item item, InventoryView view, InventoryModel model, CombatSystem combatSystem)
    {
        if (item == null || item.Count <= 0) return;

        if (item.IsStackable)
        {
            AddStackableItem(item, view, model);
        }
        else
        {
            AddNonStackableItem(item, view, model, combatSystem);
        }

        view.GetSlotWithItem(item)?.UpdateVisual();
    }

    private static void AddStackableItem(Item item, InventoryView view, InventoryModel model)
    {
        int remainingCount = item.Count;

        foreach (var slot in view.InventorySlots.Where(s => !s.IsEmpty() &&
            s.Item.Id == item.Id &&
            s.Item.Count < s.Item.MaxStackSize &&
            s.Item.Rarity == item.Rarity))
        {
            if (remainingCount <= 0) break;

            int amountToAdd = Mathf.Min(slot.Item.MaxStackSize - slot.Item.Count, remainingCount);
            slot.Item.Count += amountToAdd;

            slot.UpdateVisual();
            remainingCount -= amountToAdd;
        }

        while (remainingCount > 0)
        {
            var emptySlot = view.GetFirstEmptySlot();
            if (emptySlot == null || !IsValidForSlot(item, emptySlot))
            {
                DropExcessItem(item, remainingCount);
                break;
            }

            int countToAdd = Math.Min(remainingCount, item.MaxStackSize);
            item.Count = countToAdd;

            remainingCount -= countToAdd;

            model.AddItem(item);
            emptySlot.InitializeSlot(item);
        }
    }

    private static void AddNonStackableItem(Item item, InventoryView view, InventoryModel model, CombatSystem combatSystem)
    {
        for (int i = 0; i < item.Count; i++)
        {
            var emptySlot = view.GetFirstEmptySlot();
            if (emptySlot == null || !IsValidForSlot(item, emptySlot))
            {
                if (item is Weapon weapon)
                {
                    DropExcessItem(weapon, 1);
                }
                else if(item is SecondaryWeapon secondaryWeapon)
                {
                    DropExcessItem(secondaryWeapon, 1);
                }
                else
                {
                    DropExcessItem(item, 1);
                }
                break;
            }

            Item newItem = item;
            newItem.Count = 1;

            model.AddItem(newItem);
            emptySlot.InitializeSlot(newItem);

            if(item is Weapon w && emptySlot.SlotType == SlotType.Weapon)
            {
                combatSystem.SetWeapon(w);
            }
            else if(item is SecondaryWeapon sw && emptySlot.SlotType == SlotType.SecondaryWeapon)
            {
                combatSystem.SetSecondaryWeapon(sw);
            }
        }
    }

    private static void DropExcessItem(Item item, int count)
    {
        item.Count = count;

        var container = ItemGenerator.Instance.GenerateContainer(item);
        container.transform.position = Camera.main.transform.position + Camera.main.transform.forward;
        container.GetComponent<Rigidbody>().AddForce(Camera.main.transform.forward * 3, ForceMode.Impulse);

        Debug.Log($"Cannot add {count} items of type <color=cyan>{item.Id}</color>. Inventory full!");
    }

    public static void RemoveItem(Item item, InventoryView view, InventoryModel model)
    {
        var slot = view.GetSlotWithItem(item);
        if (slot != null)
        {
            view.ClearSlot(slot);
            model.RemoveItem(item);
        }
    }

    public static void RemoveItem(InventorySlot slot, InventoryView view, InventoryModel model)
    {
        if (slot.Item != null)
        {
            view.ClearSlot(slot);
            model.RemoveItem(slot.Item);
        }
    }

    private static bool IsValidForSlot(Item item, InventorySlot slot) => InventoryDragDropHandler.IsValid(item, slot);
}

public static class InventoryDragDropHandler
{
    public static void HandleDrop(PointerEventData eventData, Item item, InventorySlot targetSlot, Inventory inventory)
    {
        if (!IsValidDrop(inventory.DragableItem, inventory.LastInteractSlot, targetSlot))
        {
            inventory.ResetDrag();
            return;
        }

        HandleWeaponSlots(inventory, targetSlot);
        HandleSecondaryWeaponSlots(inventory, targetSlot);


        if (targetSlot.IsEmpty())
        {
            HandleEmptySlotDrop(targetSlot, inventory);
        }
        else if (IsStackableDrop(targetSlot, inventory))
        {
            HandleStackableDrop(targetSlot, inventory);
        }
        else
        {
            HandleSwapDrop(targetSlot, inventory);
        }


        HandleHotbarSelection(targetSlot, inventory);
        inventory.ResetDrag();
    }

    private static bool IsValidDrop(Item dragableItem, InventorySlot lastSlot, InventorySlot targetSlot)
    {
        if (dragableItem == null || lastSlot == null || targetSlot == lastSlot ||
            !IsValid(dragableItem, targetSlot) || !IsValid(targetSlot.Item, lastSlot))
            return false;

        return true;
    }

    public static bool IsValid(Item item, InventorySlot targetSlot)
    {
        if (item == null) return true;

        return targetSlot.SlotType switch
        {
            SlotType.Hotbar => item is UseableItem,
            SlotType.Weapon => item is MeeleWeapon || item is RangeWeapon,
            SlotType.SecondaryWeapon => item is SecondaryWeapon,
            SlotType.Artifact => item is Artifact,
            SlotType.Default => true,
            _ => false
        };
    }

    private static void HandleWeaponSlots(Inventory inventory, InventorySlot targetSlot)
    {
        if (inventory.DragableItem is Weapon weapon && targetSlot.SlotType == SlotType.Weapon)
            inventory.CombatSystem.SetWeapon(weapon);

        if (inventory.LastInteractSlot.SlotType == SlotType.Weapon)
        {
            inventory.CombatSystem.RemoveWeapon();
            if (targetSlot.Item is Weapon w)
                inventory.CombatSystem.SetWeapon(w);
        }
    }

    private static void HandleSecondaryWeaponSlots(Inventory inventory, InventorySlot targetSlot)
    {
        if (inventory.DragableItem is SecondaryWeapon secWeapon && targetSlot.SlotType == SlotType.SecondaryWeapon)
            inventory.CombatSystem.SetSecondaryWeapon(secWeapon);

        if (inventory.LastInteractSlot.SlotType == SlotType.SecondaryWeapon)
        {
            inventory.CombatSystem.RemoveSecondaryWeapon();
            if (targetSlot.Item is SecondaryWeapon w)
                inventory.CombatSystem.SetSecondaryWeapon(w);
        }
    }

    private static void HandleEmptySlotDrop(InventorySlot targetSlot, Inventory inventory)
    {
        targetSlot.InitializeSlot(inventory.DragableItem);
        inventory.LastInteractSlot.ClearSlot();
    }

    private static bool IsStackableDrop(InventorySlot targetSlot, Inventory inventory) =>
        inventory.DragableItem.IsStackable &&
        targetSlot.Item.Id == inventory.DragableItem.Id &&
        targetSlot.Item.Rarity == inventory.DragableItem.Rarity;

    private static void HandleStackableDrop(InventorySlot targetSlot, Inventory inventory)
    {
        if (targetSlot.Item.Count == targetSlot.Item.MaxStackSize ||
            inventory.DragableItem.Count == inventory.DragableItem.MaxStackSize)
        {
            SwapItems(targetSlot, inventory.LastInteractSlot);
            return;
        }

        int newStackCount = targetSlot.Item.Count + inventory.DragableItem.Count;
        int overflow = Mathf.Max(0, newStackCount - targetSlot.Item.MaxStackSize);

        targetSlot.Item.Count = Mathf.Min(newStackCount, targetSlot.Item.MaxStackSize);
        targetSlot.UpdateVisual();

        if (overflow > 0)
        {
            inventory.DragableItem.Count = overflow;
            inventory.LastInteractSlot.UpdateVisual();
        }
        else
            inventory.LastInteractSlot.ClearSlot();
    }

    private static void HandleSwapDrop(InventorySlot targetSlot, Inventory inventory)
    {
        Item targetItem = targetSlot.Item;
        targetSlot.InitializeSlot(inventory.DragableItem);
        inventory.LastInteractSlot.InitializeSlot(targetItem);
    }

    private static void HandleHotbarSelection(InventorySlot targetSlot, Inventory inventory)
    {
        if (inventory.LastInteractSlot.SlotType == SlotType.Hotbar && inventory.LastInteractSlot.IsEmpty())
        {
            int clearedIndex = inventory.View.HotbarSlots.IndexOf(inventory.LastInteractSlot);
            if (clearedIndex == inventory.SelectedHotbarIndex)
                inventory.Model.SetSelectedItem(null);
        }

        if (targetSlot.SlotType == SlotType.Hotbar)
        {
            int targetIndex = inventory.View.HotbarSlots.IndexOf(targetSlot);
            if (targetIndex == inventory.SelectedHotbarIndex)
                inventory.Model.SetSelectedItem(inventory.DragableItem);
        }

        inventory.SelectHotbarSlot(inventory.SelectedHotbarIndex);
    }

    private static void SwapItems(InventorySlot slot1, InventorySlot slot2)
    {
        var item1 = slot1.Item;
        var item2 = slot2.Item;

        slot1.InitializeSlot(item2);
        slot2.InitializeSlot(item1);

        slot1.UpdateVisual();
        slot2.UpdateVisual();
    }
}