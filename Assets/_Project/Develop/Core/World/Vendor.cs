using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Vendor : MonoBehaviour
{
    public enum TradeMode 
    { 
        Buy, 
        Sell 
    }
    [field: SerializeField] public float VendorCloseDistance { get; private set; } = 3f;
    [SerializeField] private InventoryView _vendorView;
    [SerializeField] private InventoryModel _vendorModel;


    public GameObject SellPanel;
    public GameObject BuyPanel;

    [SerializeField] private TMP_Text _totalGoldText;

    [SerializeField] private Button _sellAllButton;

    private Inventory _playerInventory;
    private int _playerGold = 1000;

    private TradeMode _currentMode = TradeMode.Buy;

    [Header("Phrases Change to sounds")]
    #region Phrases
    [SerializeField] private string[] _greetings = { "Welcome, traveler!", "Good to see you!", "What do you seek today?" };
    [SerializeField] private string[] _farewells = { "Farewell!", "Come back soon!", "Safe travels!" };
    [SerializeField] private string[] _buyPhrases = { "A fine purchase!", "Enjoy your new item!", "Good choice!" };
    [SerializeField] private string[] _noMoneyPhrases = { "Not enough gold!", "You’re short on coin!", "Come back with more gold!" };
    [SerializeField] private string[] _sellPhrases = { "Thanks for the goods!", "A fair trade!", "I’ll take that off your hands!" };
    #endregion

    private void Start()
    {
        _playerInventory = FindObjectOfType<Inventory>();
        InitializeVendorInventory();
        UpdatePlayerGoldText();

        _vendorView.Inventory.SetActive(false);
    }

    private void OnEnable()
    {
        if(_sellAllButton != null)
        {
            _sellAllButton.onClick.RemoveAllListeners();
            _sellAllButton.onClick.AddListener(SellAllItems);
        }
    }
    private void InitializeVendorInventory()
    {
        foreach (var slot in _vendorView.GetAllConcatSlots())
        {
            slot.onDrag += OnVendorDrag;
            slot.onDrop += OnVendorDrop;

            slot.onPointerEnter += ShowVendorItemInfo;
            slot.onPointerExit += CloseVendorItemInfo;
        }
    }

    public void OpenVendor(TradeMode mode)
    {
        _currentMode = mode;
        _vendorView.Inventory.SetActive(true);

        bool sell = _currentMode == TradeMode.Sell;

        SellPanel.SetActive(sell);
        BuyPanel.SetActive(!sell);

        _playerInventory.InventorySetAcitve(true);

        Debug.Log(GetRandomPhrase(_greetings));

        UpdateTotalGoldText();
        UpdateSellAllButtonState();
    }

    public void CloseVendor()
    {
        _vendorView.Inventory.SetActive(false);

        if(_playerInventory != null)
        {
            ReturnAllItemsToPlayer();
            _playerInventory.InventorySetAcitve(false);
            _playerInventory.UpdateCursorState(false);
        }

        Debug.Log(GetRandomPhrase(_farewells));
    }

    private void ReturnAllItemsToPlayer()
    {
        if (_vendorModel.Items.Count == 0)
        {
            Debug.Log("Nothing to return!");
            return;
        }

        var slots = _vendorView.GetAllConcatSlots();
        var itemsToReturn = _vendorModel.Items.ToList();

        foreach (var item in itemsToReturn)
        {
            _playerInventory.AddItem(item);
            _vendorModel.RemoveItem(item);

            var slot = slots.FirstOrDefault(s => s.Item == item);
            if (slot != null && !slot.IsEmpty())
            {
                slot.ClearSlot();
            }

            LogReturnedItem(item);
        }

        if (_vendorModel.Items.Count > 0)
        {
            Debug.LogWarning("Vendor model still contains items after return. Clearing manually!");
            _vendorModel.RemoveAllItems();
            ClearAllVendorSlots(slots);
        }
    }

    private void LogReturnedItem(Item item)
    {
        string rarityColor = GetRarityColor(item.Rarity);
        Debug.Log($"<color=yellow>[Returned item]</color>: <color=cyan>{item.Id}</color>, Count: <color=cyan>{item.Count}</color>, Rarity: <color={rarityColor}>{item.Rarity}</color>");
    }

    private string GetRarityColor(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => "white",
            Rarity.Uncommon => "green",
            Rarity.Rare => "cyan",
            Rarity.Epic => "purple",
            Rarity.Legendary => "orange",
            _ => "gray"
        };
    }
    private void ClearAllVendorSlots(IEnumerable<InventorySlot> slots)
    {
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty())
            {
                slot.ClearSlot();
            }
        }
    }

    public bool IsVendorOpen() => _vendorView.Inventory.activeInHierarchy;
    private void CloseVendorItemInfo() => _playerInventory.CloseItemInfo();

    private void ShowVendorItemInfo(Item item)
    {
        _playerInventory.ShowItemInfo(item);
        _playerInventory._itemDescriptionText.text = $"Price: {CalculateItemPrice(item)} gold";
    }

    private void OnVendorDrag(PointerEventData data, Item item, InventorySlot slot)
    {
        if(item == null || slot == null)
        {
            return;
        }

        _playerInventory.DragItem(data, item, slot);
    }

    private void OnVendorDrop(PointerEventData data, Item item, InventorySlot targetSlot)
    {
        Item draggedItem = _playerInventory.DragableItem;
        var sourceSlot = _playerInventory.LastInteractSlot;

        if (draggedItem == null || sourceSlot == null)
        {
            Debug.Log("No item being dragged");

            UpdateTotalGoldText();
            UpdateSellAllButtonState();

            _playerInventory.ResetDrag();
            return;
        }

        if (targetSlot == sourceSlot || targetSlot == null)
        {
            Debug.Log("Item dropped on itself or outside inventory - ignoring");
            _playerInventory.ResetDrag();
            return;
        }

        Debug.Log($"Dragging: {draggedItem}, Target: {targetSlot}, Source: {sourceSlot}");

        if (_currentMode == TradeMode.Buy)
        {
            HandleBuy(data, draggedItem, targetSlot);
        }
        else
        {
            HandleSell(data, draggedItem, targetSlot);
        }
        
        if (sourceSlot.SlotType == SlotType.Weapon)
        {
            print("Remove from weapon");
            _playerInventory.RemoveWeapon();
        }

        if (sourceSlot.SlotType == SlotType.SecondaryWeapon)
        {
            print("Remove from secondary");
            _playerInventory.RemoveSecondaryWeapon();
        }

        UpdateTotalGoldText();
        UpdateSellAllButtonState();
        _playerInventory.ResetDrag();
    }


    private void HandleBuy(PointerEventData data, Item item, InventorySlot targetSlot)
    {
        if (item == null || _playerInventory.DragableItem == null || targetSlot == null) return;

        int itemPrice = CalculateItemPrice(item);
        InventorySlot sourceSlot = _playerInventory.LastInteractSlot;

        if (_playerGold >= itemPrice)
        {
            if (InventoryDragDropHandler.IsValid(item, targetSlot))
            {
                if (targetSlot.IsEmpty())
                {
                    _playerInventory.AddItem(item);
                    targetSlot.InitializeSlot(item);

                    if (item.Count > 1)
                    {
                        item.Count--;
                        sourceSlot.UpdateVisual();
                    }
                    else
                    {
                        _vendorModel.RemoveItem(item);
                        _vendorView.ClearSlot(sourceSlot);
                    }

                    _playerGold -= itemPrice;
                    Debug.Log(GetRandomPhrase(_buyPhrases));
                }
                else if (item.IsStackable && targetSlot.Item.Id == item.Id && targetSlot.Item.Rarity == item.Rarity)
                {
                    int newStackCount = targetSlot.Item.Count + item.Count;
                    int overflow = Mathf.Max(0, newStackCount - targetSlot.Item.MaxStackSize);

                    targetSlot.Item.Count = Mathf.Min(newStackCount, targetSlot.Item.MaxStackSize);
                    targetSlot.UpdateVisual();

                    if (overflow > 0)
                    {
                        item.Count = overflow;
                        sourceSlot.UpdateVisual();
                    }
                    else
                    {
                        _vendorModel.RemoveItem(item);
                        _vendorView.ClearSlot(sourceSlot);
                    }

                    _playerGold -= itemPrice;
                    Debug.Log(GetRandomPhrase(_buyPhrases));
                }
                else
                {
                    Debug.Log("Cannot buy: slot is occupied by incompatible item!");
                    _playerInventory.ResetDrag();
                    return;
                }

                UpdatePlayerGoldText();
                UpdateTotalGoldText();
                _playerInventory.ResetDrag();
            }
            else
            {
                Debug.Log("Cannot buy: invalid slot for this item!");
                _playerInventory.ResetDrag();
            }
        }
        else
        {
            Debug.Log(GetRandomPhrase(_noMoneyPhrases));
            _playerInventory.ResetDrag();
        }
    }

    private void HandleSell(PointerEventData data, Item item, InventorySlot targetSlot)
    {
        if (item == null || _playerInventory.LastInteractSlot == null)
        {
            Debug.Log("Cannot sell: No item being dragged or no source slot!");
            _playerInventory.ResetDrag();
            return;
        }

        InventorySlot sourceSlot = _playerInventory.LastInteractSlot;

        if (targetSlot == null)
        {
            Debug.Log("Cannot sell: Item dropped outside of a valid slot!");
            _playerInventory.ResetDrag();
            return;
        }


        if (!InventoryDragDropHandler.IsValid(item, targetSlot) ||
            !InventoryDragDropHandler.IsValid(targetSlot.Item, sourceSlot))
        {
            Debug.Log("Cannot sell or swap: invalid slot types!");
            _playerInventory.ResetDrag();
            return;
        }

        if (targetSlot.IsEmpty())
        {
            targetSlot.InitializeSlot(item);
            _vendorModel.AddItem(item);
            _playerInventory.RemoveItem(sourceSlot);

            if (sourceSlot.SlotType == SlotType.Hotbar &&
                _playerInventory.View.HotbarSlots.IndexOf(sourceSlot) == _playerInventory.SelectedHotbarIndex)
            {
                _playerInventory.Model.SetSelectedItem(null);
                _playerInventory.UpdateAllHotbarSlots();
            }

            Debug.Log($"Item placed in vendor inventory: {item.Id}");
        }
        else if (item.IsStackable && targetSlot.Item.Id == item.Id && targetSlot.Item.Rarity == item.Rarity)
        {
            if (targetSlot.Item.Count == targetSlot.Item.MaxStackSize ||
                item.Count == item.MaxStackSize)
            {
                Item targetItem = targetSlot.Item;
                targetSlot.InitializeSlot(item);
                sourceSlot.InitializeSlot(targetItem);

                _vendorModel.RemoveItem(targetItem);
                _vendorModel.AddItem(item);

                _playerInventory.Model.RemoveItem(item);
                _playerInventory.Model.AddItem(targetItem);

                targetSlot.UpdateVisual();
                sourceSlot.UpdateVisual();

                if (sourceSlot.SlotType == SlotType.Hotbar &&
                    _playerInventory.View.HotbarSlots.IndexOf(sourceSlot) == _playerInventory.SelectedHotbarIndex)
                {
                    _playerInventory.Model.SetSelectedItem(targetItem);
                    _playerInventory.UpdateAllHotbarSlots();
                }

                Debug.Log($"Swapped full stack {item.Id} with {targetItem.Id} in vendor inventory.");
            }
            else
            {
                int newStackCount = targetSlot.Item.Count + item.Count;
                int overflow = Mathf.Max(0, newStackCount - targetSlot.Item.MaxStackSize);

                targetSlot.Item.Count = Mathf.Min(newStackCount, targetSlot.Item.MaxStackSize);
                targetSlot.UpdateVisual();

                if (overflow > 0)
                {
                    item.Count = overflow;
                    sourceSlot.UpdateVisual();
                }
                else
                {
                    _playerInventory.RemoveItem(sourceSlot);

                    if (sourceSlot.SlotType == SlotType.Hotbar &&
                        _playerInventory.View.HotbarSlots.IndexOf(sourceSlot) == _playerInventory.SelectedHotbarIndex)
                    {
                        _playerInventory.Model.SetSelectedItem(null);
                        _playerInventory.UpdateAllHotbarSlots();
                    }
                }

                Debug.Log($"Added stackable item to vendor inventory: {item.Id}");
            }
        }
        else // Простой свап для нестакаемых предметов
        {
            Item targetItem = targetSlot.Item;
            targetSlot.InitializeSlot(item);
            sourceSlot.InitializeSlot(targetItem);

            _vendorModel.RemoveItem(targetItem);
            _vendorModel.AddItem(item);
            _playerInventory.Model.RemoveItem(item);
            _playerInventory.Model.AddItem(targetItem);

            targetSlot.UpdateVisual();
            sourceSlot.UpdateVisual();

            if (sourceSlot.SlotType == SlotType.Hotbar &&
                _playerInventory.View.HotbarSlots.IndexOf(sourceSlot) == _playerInventory.SelectedHotbarIndex)
            {
                _playerInventory.Model.SetSelectedItem(targetItem);
                _playerInventory.UpdateAllHotbarSlots();
            }

            Debug.Log($"Swapped {item.Id} with {targetItem.Id} in vendor inventory.");
        }

        UpdateTotalGoldText();
        UpdateSellAllButtonState();
        _playerInventory.ResetDrag();
    }

    private int CalculateItemPrice(Item item)
    {
        if (item == null)
            return 0;

        int basePrice = 10;

        int rarityMultiplier = (int)item.Rarity * 5;
        int effectCount = item.Effects.Count;

        return basePrice + rarityMultiplier * (effectCount * 5);
    }
    private void UpdateTotalGoldText()
    {
        if (_totalGoldText == null) return;

        if (_currentMode == TradeMode.Sell) //Sell mode
        {
            int total = 0;
            foreach (var slot in _vendorView.GetAllConcatSlots())
            {
                if (slot.Item != null)
                    total += CalculateItemPrice(slot.Item);
            }

            _totalGoldText.text = $"{total}";
        }
        else //Buy mode
        {
            _totalGoldText.text = $"Vendor Stock";
        }
    }
    private void UpdatePlayerGoldText()
    {
        if (_playerInventory.GoldText != null)
            _playerInventory.GoldText.text = $"{_playerGold}";
    }
    private string GetRandomPhrase(string[] phrases) => phrases[UnityEngine.Random.Range(0, phrases.Length)];

    public void SellAllItems()
    {
        if (_currentMode != TradeMode.Sell) 
            return;

        int totalValue = 0;
        List<Item> itemsToRemove = new();

        foreach (var slot in _vendorView.GetAllConcatSlots())
        {
            if (slot.Item != null)
            {
                int itemPrice = CalculateItemPrice(slot.Item);
                totalValue += itemPrice;
                itemsToRemove.Add(slot.Item);
            }
        }

        _playerGold += totalValue;

        foreach (var item in itemsToRemove)
        {
            _vendorModel.RemoveItem(item);
            _vendorView.GetSlotWithItem(item).ClearSlot();
        }

        Debug.Log($"Sold all items for {totalValue} gold!");
        Debug.Log(GetRandomPhrase(_sellPhrases));

        UpdatePlayerGoldText();
        UpdateTotalGoldText();
        UpdateSellAllButtonState();
    }

    private void UpdateSellAllButtonState()
    {
        if (_sellAllButton != null && _currentMode == TradeMode.Sell)
        {
            _sellAllButton.interactable = HasItemsToSell();
        }
    }

    private bool HasItemsToSell()
    {
        foreach (var slot in _vendorView.GetAllConcatSlots())
        {
            if (slot.Item != null)
                return true;
        }
        return false;
    }
}
