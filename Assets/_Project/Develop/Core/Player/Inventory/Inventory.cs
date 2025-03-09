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
using _Project.Develop.Core.Effects.Base;
using _Project.Develop.Core.Entities.Potions;
using _Project.Develop.Core.Enum;
using TMPro;
using UnityEngine.SceneManagement;

public class Inventory : MonoBehaviour
{
    [field: SerializeField] public bool dontDestroyOnLoad { get; private set; } = false;
    private static Inventory _instance;
    public int PlayerGold { get; private set; } = 1000;

    [SerializeField] private Camera _camera;
    [SerializeField] private Image _dragPreviewImage;

    [SerializeField] private RectTransform _itemInfoPanel;
    [SerializeField] private TMP_Text _itemNameText;
    [SerializeField] private TMP_Text _itemStatText;
    [SerializeField] private TMP_Text _itemEffectText;
    [SerializeField] public TMP_Text _itemDescriptionText;
    [SerializeField] public TMP_Text _interactText;
    [SerializeField] public TMP_Text _vendorText;

    [SerializeField] private TMP_Text _playerStatsText;

    [field: HorizontalLine(2, EColor.Green)]
    [field: SerializeField] public InventoryView View { get; private set; } = null;
    [field: SerializeField] public InventoryModel Model { get; private set; } = null;
    [HorizontalLine(2, EColor.Green)]
    [Header("Input Settings")]
    public KeyCode OpenInventoryKey = KeyCode.Tab;
    public KeyCode UseItemKey = KeyCode.E;
    public KeyCode PickItemKey = KeyCode.F; // And open vendor/book/anvil panels
    public KeyCode CloseKey = KeyCode.Escape;

    public Item DragableItem { get; private set; } = null;
    public InventorySlot LastInteractSlot { get; private set; } = null;
    public int SelectedHotbarIndex { get; private set; } = 0;
    public CombatSystem CombatSystem { get; private set; } = null;

    private bool InventoryState;

    public static event Action<bool> OnInventoryStateChange;

    [field: Tooltip("Item pickup & vendor interact distance")]
    [field: SerializeField] public float InteractionDistance { get; private set; } = 5f;
    [field: SerializeField] public TMP_Text GoldText { get; private set; } = null;
    [SerializeField, ReadOnly] private Vendor _currentVendor = null;
    [SerializeField, ReadOnly] private Anvil _currentAnvil = null;
    [SerializeField, ReadOnly] private BookOfTheAbyss _currentBook = null; // Добавили BookOfTheAbyss

    #region Initialize
    private void Awake()
    {
        if (dontDestroyOnLoad)
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        CombatSystem = GetComponentInChildren<CombatSystem>();
    }

    private void Start()
    {
        Initialize();

        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.UseableItems, Rarity.Legendary);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.UseableItems, Rarity.Legendary);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.UseableItems, Rarity.Rare);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.UseableItems, Rarity.Legendary);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.UseableItems, Rarity.Rare);
        ItemGenerator.Instance.GenerateWeaponGameobject(WeaponType.UseableItems, Rarity.Rare);

        View.Inventory.SetActive(false);
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
            slot.onDrop += (a, b, c) => UpdateStatsText();

            slot.onRightClick += QuickMoveItem;

            slot.onPointerEnter += ShowItemInfo;
            slot.onPointerExit += CloseItemInfo;
        }

        OnInventoryStateChange += value =>
        {
            InventoryState = value;
            if (!value)
            {
                CloseItemInfo();
            }

            UpdateStatsText();

            if (Model.Minimap != null)
                Model.Minimap.SetActive(!value);
        };
    }

    public void RemoveWeapon() => CombatSystem.RemoveWeapon();
    public void RemoveSecondaryWeapon() => CombatSystem.RemoveSecondaryWeapon();
    #endregion 

    #region Handlers
    private void Update()
    {
        HandleInput();
        UpdatePreviewItem();
        UpdateStatsText();
    }
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            SceneManager.LoadScene(1);
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            SceneManager.LoadScene(2);
        }
        if (Input.GetKeyDown(OpenInventoryKey)) ToggleInventory();
        if (Input.GetKeyDown(PickItemKey)) TryPickUpItem();
        if (Input.GetKeyDown(UseItemKey)) UseSelectedItem();

        if (Input.GetKeyDown(CloseKey) || Input.GetKeyDown(OpenInventoryKey) && _currentVendor != null && _currentVendor.IsVendorOpen()) CloseVendorInterface();
        if (Input.GetKeyDown(CloseKey) || Input.GetKeyDown(OpenInventoryKey) && _currentAnvil != null && _currentAnvil.IsAnvilOpen()) CloseAnvilInterface();
        if (Input.GetKeyDown(CloseKey) || Input.GetKeyDown(OpenInventoryKey) && _currentBook != null && _currentBook.IsBookOpen()) CloseBookInterface();

        TryOpenVendorOrAnvilOrBook();

        CheckForItem();
        HandleHotbarInput();
    }

    public void ChangePlayerGold(int amount) => PlayerGold += amount;

    private void TryOpenVendorOrAnvilOrBook() // Обновили метод
    {
        Physics.Raycast(_camera.transform.position, _camera.transform.forward, out RaycastHit hit, InteractionDistance);

        if (hit.collider != null && (hit.collider.TryGetComponent(out Vendor v) || hit.collider.TryGetComponent(out Anvil a) || hit.collider.TryGetComponent(out BookOfTheAbyss b)))
        {
            _vendorText.enabled = true;
        }
        else
        {
            _vendorText.enabled = false;
        }

        if (Input.GetKeyDown(PickItemKey) && hit.collider != null && hit.collider.TryGetComponent(out Vendor vendor))
        {
            if (_currentVendor == vendor)
            {
                CloseVendorInterface();
            }
            else
            {
                OpenVendorInterface(vendor);
            }
        }
        else if (_currentVendor != null && Vector3.Distance(transform.GetChild(0).position, _currentVendor.transform.position) > _currentVendor.VendorCloseDistance)
        {
            CloseVendorInterface();
        }

        if (Input.GetKeyDown(PickItemKey) && hit.collider != null && hit.collider.TryGetComponent(out Anvil anvil))
        {
            if (_currentAnvil == anvil)
            {
                CloseAnvilInterface();
            }
            else
            {
                OpenAnvilInterface(anvil);
            }
        }
        else if (_currentAnvil != null && Vector3.Distance(transform.GetChild(0).position, _currentAnvil.transform.position) > _currentAnvil.AnvilCloseDistance)
        {
            CloseAnvilInterface();
        }

        if (Input.GetKeyDown(PickItemKey) && hit.collider != null && hit.collider.TryGetComponent(out BookOfTheAbyss book))
        {
            if (_currentBook == book)
            {
                CloseBookInterface();
            }
            else
            {
                OpenBookInterface(book);
            }
        }
        else if (_currentBook != null && Vector3.Distance(transform.GetChild(0).position, _currentBook.transform.position) > _currentBook.BookCloseDistance)
        {
            CloseBookInterface();
        }
    }

    private void CheckForItem()
    {
        Physics.Raycast(_camera.transform.position, _camera.transform.forward, out RaycastHit hit, InteractionDistance);

        if (hit.collider != null && hit.collider.TryGetComponent(out EntityContainer cont))
        {
            _interactText.enabled = true;
            ShowItemInfo((Item)cont.ContainedEntity);
        }
        else if (!InventoryState)
        {
            _interactText.enabled = false;
            CloseItemInfo();
        }
        else
        {
            _interactText.enabled = false;
        }
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
        Physics.Raycast(_camera.transform.position, _camera.transform.forward, out RaycastHit hit, InteractionDistance);

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

        if (_currentVendor != null || _currentAnvil != null || _currentBook != null)
            return;

        InventorySetAcitve(enabled);
        UpdateCursorState(enabled);
        ResetDragIfClosing(enabled);

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

    private void UseSelectedItem()
    {
        if (Model.SelectedItem == null)
        {
            Debug.Log(Model.SelectedItem);
            return;
        }

        ((UseableItem)Model.SelectedItem).Use(CombatSystem.playerModel);
        if (((UseableItem)Model.SelectedItem).Count > 1)
        {
            ((UseableItem)Model.SelectedItem).Count--;
        }
        else
            RemoveHotbarItem(Model.SelectedItem);

        UpdateAllHotbarSlots();
        Debug.Log($"Item: {Model.SelectedItem.Id}, ItemCount: {Model.SelectedItem?.Count ?? 0}, Rarity: {Model.SelectedItem.Rarity}");
    }
    #endregion  

    #region Item Management
    public void AddItem(Item item) => InventoryItemManager.AddItem(item, View, Model, CombatSystem, _camera);
    public void RemoveItem(Item item) => InventoryItemManager.RemoveItem(item, View, Model);
    public void RemoveHotbarItem(Item item) => InventoryItemManager.RemoveHotbarItem(item, View, Model);
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

    public void DropItem(PointerEventData eventData, Item item, InventorySlot targetSlot)
    {
        if (DragableItem == null || LastInteractSlot == null)
        {
            ResetDrag();
            return;
        }

        if (_currentVendor != null && _currentVendor.CurrentMode == Vendor.TradeMode.Buy &&
            _currentVendor.BuySlots.Contains(LastInteractSlot))
        {
            return;
        }

        InventoryDragDropHandler.HandleDrop(eventData, item, targetSlot, this);
    }
    public void ResetDrag()
    {
        DragableItem = null;
        LastInteractSlot = null;

        if (_dragPreviewImage == null)
            return;
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
            !VendorIsActive() && !AnvilIsActive() && !BookIsActive()) // Добавили проверку на книгу
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

            ResetDrag();
            UpdateAllHotbarSlots();
        }
    }

    public void UpdateStatsText()
    {
        string effects = string.Empty;
        foreach (var stat in CombatSystem.playerModel.Stats)
        {
            effects += $"{stat.Value.Type.ToString()} : {(int)stat.Value.CurrentValue}\n";
        }

        _playerStatsText.text = effects;
    }

    private bool VendorIsActive() => _currentVendor != null && _currentVendor.IsVendorOpen();
    private bool AnvilIsActive() => _currentAnvil != null && _currentAnvil.IsAnvilOpen();
    private bool BookIsActive() => _currentBook != null && _currentBook.IsBookOpen(); // Добавили проверку активности книги

    private void DropItemInWorld(Item item)
    {
        var container = ItemGenerator.Instance.GenerateContainer(item, true);
        container.transform.position = _camera.transform.position + _camera.transform.forward;
        container.GetComponent<Rigidbody>().AddForce(_camera.transform.forward * 3, ForceMode.Impulse);
    }
    #endregion

    #region QuickMove logic
    private void QuickMoveItem(PointerEventData eventData, Item item, InventorySlot sourceSlot)
    {
        if (item == null || eventData.button != PointerEventData.InputButton.Right || sourceSlot.SlotType == SlotType.Hotbar) return;

        InventorySlot targetSlot = FindQuickMoveTargetSlot(item);

        if (targetSlot == null)
        {
            Debug.Log($"No suitable slot found for quick move of {item.Id}");
            return;
        }

        bool sourceWasSelected = sourceSlot.SlotType == SlotType.Hotbar &&
                                View.HotbarSlots.IndexOf(sourceSlot) == SelectedHotbarIndex;

        if (item.IsStackable && targetSlot.SlotType == SlotType.Hotbar)
        {
            InventorySlot stackableSlot = FindStackableSlotInHotbar(item);
            if (stackableSlot != null)
            {
                targetSlot = stackableSlot;
            }
        }

        if (item.IsStackable && targetSlot.SlotType == SlotType.Hotbar &&
            !targetSlot.IsEmpty() && targetSlot.Item.Id == item.Id && targetSlot.Item.Rarity == item.Rarity)
        {
            HandleStackableQuickMove(item, sourceSlot, targetSlot, sourceWasSelected);
        }
        else if (targetSlot.IsEmpty())
        {
            targetSlot.InitializeSlot(item);
            sourceSlot.ClearSlot();
            HandleQuickMoveEquip(item, targetSlot);

            if (sourceWasSelected)
            {
                Model.SetSelectedItem(null);
            }
        }
        else
        {
            Item targetItem = targetSlot.Item;
            targetSlot.InitializeSlot(item);
            sourceSlot.InitializeSlot(targetItem);
            HandleQuickMoveEquip(item, targetSlot);

            if (sourceWasSelected)
            {
                Model.SetSelectedItem(targetItem);
            }
        }

        targetSlot.UpdateVisual();
        sourceSlot.UpdateVisual();

        UpdateAllHotbarSlots();
        UpdateStatsText();
    }

    private InventorySlot FindStackableSlotInHotbar(Item item)
    {
        return View.HotbarSlots.FirstOrDefault(s => !s.IsEmpty() &&
                                                   s.Item.Id == item.Id &&
                                                   s.Item.Rarity == item.Rarity &&
                                                   s.Item.Count < s.Item.MaxStackSize);
    }

    private void HandleStackableQuickMove(Item item, InventorySlot sourceSlot, InventorySlot targetSlot, bool sourceWasSelected)
    {
        int newStackCount = targetSlot.Item.Count + item.Count;
        int maxStackSize = item.MaxStackSize;

        if (newStackCount <= maxStackSize)
        {
            targetSlot.Item.Count = newStackCount;
            sourceSlot.ClearSlot();

            if (sourceWasSelected)
            {
                Model.SetSelectedItem(null);
            }
        }
        else
        {
            int amountToAdd = maxStackSize - targetSlot.Item.Count;
            targetSlot.Item.Count = maxStackSize;
            item.Count -= amountToAdd;

            if (sourceWasSelected && View.HotbarSlots.IndexOf(targetSlot) == SelectedHotbarIndex)
            {
                Model.SetSelectedItem(targetSlot.Item);
            }
        }

        HandleQuickMoveEquip(targetSlot.Item, targetSlot);
    }
    private InventorySlot FindQuickMoveTargetSlot(Item item)
    {
        if (item is MeeleWeapon || item is RangeWeapon)
        {
            return View.InventorySlots.FirstOrDefault(s => s.SlotType == SlotType.Weapon);
        }
        else if (item is SecondaryWeapon)
        {
            return View.InventorySlots.FirstOrDefault(s => s.SlotType == SlotType.SecondaryWeapon);
        }
        else if (item is UseableItem)
        {
            return View.HotbarSlots.FirstOrDefault(s => s.IsEmpty()) ??
                   View.HotbarSlots[SelectedHotbarIndex];
        }
        else if (item is Artifact)
        {
            return View.InventorySlots.FirstOrDefault(s => s.SlotType == SlotType.Artifact1);
        }

        return null;
    }
    private void HandleQuickMoveEquip(Item item, InventorySlot targetSlot)
    {
        if (targetSlot.SlotType == SlotType.Weapon && item is Weapon weapon)
        {
            CombatSystem.SetWeapon(weapon);
        }
        else if (targetSlot.SlotType == SlotType.SecondaryWeapon && item is SecondaryWeapon secWeapon)
        {
            CombatSystem.SetSecondaryWeapon(secWeapon);
        }
        else if (targetSlot.SlotType == SlotType.Hotbar)
        {
            int targetIndex = View.HotbarSlots.IndexOf(targetSlot);
            if (targetIndex == SelectedHotbarIndex)
            {
                Model.SetSelectedItem(item);
            }
        }
        else if ((targetSlot.SlotType == SlotType.Artifact1) && item is Artifact artifact1)
        {
            CombatSystem.SetFirstArtifact(artifact1);
        }
        else if ((targetSlot.SlotType == SlotType.Artifact2) && item is Artifact artifact2)
        {
            CombatSystem.SetSecondArtifact(artifact2);
        }
    }
    #endregion

    public void ShowItemInfo(Item item)
    {
        _itemInfoPanel.gameObject.SetActive(true);
        _itemNameText.text = $"{item.Rarity} {item.Id}";
        string stats = string.Empty;
        foreach (var stat in item.Stats)
        {
            stats += $"{stat.Key}:{stat.Value.CurrentValue}\n";
        }

        _itemStatText.text = stats;
        Debug.Log($"{item.Id}");
        string effects = string.Empty;
        if (item is Artifact)
        {
            foreach (var stat in item.Stats)
            {
                effects += $"{stat.Value.Type.ToString()} : {stat.Value.CurrentValue}\n";
            }
        }
        else if (item is RagePotion || item is AgilityPotions || item is WisdomPotion)
        {
            if (((UseableItem)item).GetEffect() is ContinuousEffect cont)
            {
                effects += $"Adds {cont.magnitude}\n {item.Id.Split(' ').Last()}";
                effects += $"For {cont.Duration}\n seconds";
            }
        }
        else if (item is UseableItem && ((UseableItem)item).GetEffect() is PeriodicEffect per)
        {
            effects += $"Heals {per.magnitude * per.Duration} health\n";
            effects += $"In {per.Duration} seconds\n";
        }
        else if (item is Map)
        {
            _itemStatText.text = string.Empty;
            foreach (var stat in item.Stats)
            {
                effects += $"{stat.Value.Type.ToString()} : {stat.Value.CurrentValue * 100:f1}%\n";
            }
        }
        else
        {
            foreach (var effect in item.Effects)
            {
                effects += $"{effect.Name}\n";
            }
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

    public void OpenVendorInterface(Vendor vendor)
    {
        if (_currentVendor != null && _currentVendor != vendor)
        {
            CloseVendorInterface();
        }
        _currentVendor = vendor;
        _currentVendor.OpenVendor();

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

    public void OpenAnvilInterface(Anvil anvil)
    {
        if (_currentAnvil != null && _currentAnvil != anvil)
        {
            CloseAnvilInterface();
        }
        _currentAnvil = anvil;
        _currentAnvil.OpenAnvil();

        InventorySetAcitve(true);
        UpdateCursorState(true);

        OnInventoryStateChange?.Invoke(true);
    }
    public void CloseAnvilInterface()
    {
        if (_currentAnvil != null)
        {
            _currentAnvil.CloseAnvil();
            _currentAnvil = null;

            InventorySetAcitve(false);
            UpdateCursorState(false);

            OnInventoryStateChange?.Invoke(false);
        }
    }

    // Добавили методы для BookOfTheAbyss
    public void OpenBookInterface(BookOfTheAbyss book)
    {
        if (_currentBook != null && _currentBook != book)
        {
            CloseBookInterface();
        }
        _currentBook = book;
        _currentBook.OpenBook();

        InventorySetAcitve(true);
        UpdateCursorState(true);

        OnInventoryStateChange?.Invoke(true);
    }
    public void CloseBookInterface()
    {
        if (_currentBook != null)
        {
            _currentBook.CloseBook();
            _currentBook = null;

            InventorySetAcitve(false);
            UpdateCursorState(false);

            OnInventoryStateChange?.Invoke(false);
        }
    }
}