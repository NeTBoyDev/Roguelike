using _Project.Develop.Core;
using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;
using DG.Tweening;
using NaughtyAttributes;
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
    [field: Header("Main settings")]
    [field: SerializeField] public float VendorCloseDistance { get; private set; } = 3f;
    [SerializeField] private InventoryView _vendorView = null;
    [SerializeField] private InventoryModel _vendorModel = null;

    [HorizontalLine(2, EColor.Green)]
    [field: Header("Sell settings")]
    public GameObject SellPanel;
    [Space(10)]

    [SerializeField] private Button OpenSellPanelButton;
    [SerializeField] private Button _sellAllButton;
    [SerializeField] private TMP_Text _totalGoldText;

    [field: Tooltip("Sell multiplier for items (1 - standard price) (0 - free sell)")]
    [field: SerializeField, MinValue(0)] public float SellMultiplier { get; private set; } = 1;

    [HorizontalLine(2, EColor.Green)]
    [field: Header("Buy settings")]
    public GameObject BuyPanel;
    [Space(10)]

    [SerializeField] private Button OpenBuyPanelButton;
    [SerializeField] private TMP_Text _itemGoldText;
    [field: Tooltip("Purchase multiplier for items (1 - standard price) (0 - free buy)")]
    [field: SerializeField, MinValue(0)] public float BuyMultiplier { get; private set; } = 2;
    [field: HorizontalLine(2, EColor.Green)]
    [field: SerializeField] public List<InventorySlot> BuySlots { get; private set; }//Vendor buy slots
    [field: SerializeField] public List<Item> BuyItems { get; private set; } = new(1);

    [field: SerializeField] public Vector2Int RandomItemsCount { get; private set; } = Vector2Int.one;


    private Inventory _playerInventory;
    [field: SerializeField, ReadOnly] public TradeMode LastSelectedMode { get; private set; } = TradeMode.Buy;
    [field: SerializeField, ReadOnly] public TradeMode CurrentMode { get; private set; } = TradeMode.Buy;

    private Vector3 _initialButtonPos;

    [Header("Phrases Change to sounds")]
    #region Phrases
    [SerializeField] private string[] _greetings = { "Welcome, traveler!", "Good to see you!", "What do you seek today?" };
    [SerializeField] private string[] _farewells = { "Farewell!", "Come back soon!", "Safe travels!" };
    [SerializeField] private string[] _buyPhrases = { "A fine purchase!", "Enjoy your new item!", "Good choice!" };
    [SerializeField] private string[] _noMoneyPhrases = { "Not enough gold!", "You’re short on coin!", "Come back with more gold!" };
    [SerializeField] private string[] _sellPhrases = { "Thanks for the goods!", "A fair trade!", "I’ll take that off your hands!" };
    #endregion

    #region Initialize
    private void Start()
    {
        FindPlayer();

        SubscribeEvents();
        InitializeVendorInventory();
        UpdatePlayerGoldText();

        _vendorView.Inventory.SetActive(false);
    }

    private void FindPlayer()
    {
        if(_playerInventory == null)
            _playerInventory = FindObjectOfType<Inventory>();
    }

    private void OnValidate()
    {
        if (BuySlots.Count > 0)
        {
            RandomItemsCount = new Vector2Int(
                Mathf.Clamp(RandomItemsCount.x, 1, BuySlots.Count),
                Mathf.Clamp(RandomItemsCount.y, Mathf.Max(1, RandomItemsCount.x), BuySlots.Count)
            );
        }
    }

    private void OnEnable()
    {
        if(_sellAllButton != null)
        {
            _sellAllButton.onClick.RemoveAllListeners();
            _sellAllButton.onClick.AddListener(SellAllItems);
        }
    }
    private void OnDisable()
    {
        foreach (var slot in _vendorView.GetAllConcatSlots())
        {
            slot.onDrag -= OnVendorDrag;
            slot.onDrop -= OnVendorDrop;

            slot.onPointerEnter -= ShowVendorItemInfo;
            slot.onPointerExit -= CloseVendorItemInfo;
        }

        foreach (var slot in BuySlots)
        {
            slot.onDrag -= OnVendorDrag;
            slot.onDrop -= OnVendorDrop;

            slot.onPointerEnter -= ShowVendorItemInfo;
            slot.onPointerExit -= CloseVendorItemInfo;
        }
    }
    private void InitializeVendorInventory()
    {
        foreach (var slot in _vendorView.GetAllConcatSlots().Concat(BuySlots))
        {
            slot.onDrag += OnVendorDrag;
            slot.onDrop += OnVendorDrop;

            slot.onPointerEnter += ShowVendorItemInfo;
            slot.onPointerExit += CloseVendorItemInfo;
        }

        KitStart();
    }

    private void KitStart()
    {
        var randomCount = Random.Range(RandomItemsCount.x, RandomItemsCount.y + 1);
        Debug.Log($"Random count = {randomCount}");

        for (int i = 0; i < randomCount; i++)
        {
            if (BuySlots[i].IsEmpty())
            {
                var obj = ItemGenerator.Instance.GenerateRandomGameobject();
                var item = obj.ContainedEntity as Item;
                Destroy(obj.gameObject);

                BuyItems.Add(item);
                BuySlots[i].InitializeSlot(item);
            }
        }
    }
    private void SubscribeEvents()
    {
        OpenBuyPanelButton.onClick.RemoveAllListeners();
        OpenSellPanelButton.onClick.RemoveAllListeners();

        _initialButtonPos = OpenBuyPanelButton.transform.localPosition;

        OpenBuyPanelButton.onClick.AddListener(() => SetPanel(TradeMode.Buy, OpenBuyPanelButton, OpenSellPanelButton));
        OpenSellPanelButton.onClick.AddListener(() => SetPanel(TradeMode.Sell, OpenSellPanelButton, OpenBuyPanelButton));
    }
    #endregion

    #region Vendor panel logic
    private void SetPanel(TradeMode mode, Button pressedButton, Button otherButton)
    {
        BuyPanel.SetActive(mode == TradeMode.Buy);
        SellPanel.SetActive(mode == TradeMode.Sell);

        LastSelectedMode = mode;
        CurrentMode = mode;

        pressedButton.interactable = false;
        pressedButton.transform.DOLocalMoveY(_initialButtonPos.y - 10f, 0.2f);

        otherButton.interactable = true;
        otherButton.transform.DOLocalMoveY(_initialButtonPos.y, 0.2f);

        UpdateTotalGoldText();
        UpdateSellAllButtonState();
    }

    public void OpenVendor()
    {
        FindPlayer();

        CurrentMode = LastSelectedMode;
        _vendorView.Inventory.SetActive(true);

        bool sell = CurrentMode == TradeMode.Sell;

        SellPanel.SetActive(sell);
        BuyPanel.SetActive(!sell);

        SetPanel(CurrentMode, sell ? OpenSellPanelButton : OpenBuyPanelButton, sell ? OpenBuyPanelButton : OpenSellPanelButton);

        _playerInventory.InventorySetAcitve(true);

        Debug.Log(GetRandomPhrase(_greetings));

        UpdateTotalGoldText();
        UpdateSellAllButtonState();
    }

    public void CloseVendor()
    {
        _vendorView.Inventory.SetActive(false);

        if (_playerInventory != null)
        {
            ReturnAllItemsToPlayer();
            _playerInventory.InventorySetAcitve(false);
            _playerInventory.UpdateCursorState(false);
        }

        Debug.Log(GetRandomPhrase(_farewells));
    }

    #endregion

    #region Handlers
    private int CalculateItemPrice(Item item, float multiplier = 1)
    {
        if (item == null)
            return 0;

        int basePrice = 30;
        int rarityMultiplier = (int)item.Rarity;
        int effectCount = item.Effects.Count;

        int finalPrice;

        switch (item)
        {
            case MeeleWeapon _:
                basePrice = 20;
                finalPrice = basePrice + rarityMultiplier * 5 + effectCount * 5;
                break;

            case RangeWeapon _:
                basePrice = 20;
                finalPrice = basePrice + rarityMultiplier * 5 + effectCount * 5;
                break;

            case SecondaryWeapon _:
                basePrice = 20;
                finalPrice = basePrice + rarityMultiplier * 5 + effectCount * 5;
                break;

            case Artifact _:
                basePrice = 80;
                finalPrice = basePrice + rarityMultiplier * 30 + effectCount * 10;
                break;

            case UseableItem _:
                basePrice = 10;
                finalPrice = basePrice + rarityMultiplier * 5 + effectCount * 5;
                break;

            case Gem _:
                basePrice = 50;
                finalPrice = basePrice + rarityMultiplier * 4 + effectCount * 7;
                break;

            default:
                finalPrice = basePrice + rarityMultiplier + effectCount * 5;
                break;
        }

        if (item.IsStackable)
            finalPrice *= item.Count;

        return Mathf.RoundToInt(finalPrice * multiplier);
    }
    private void UpdateTotalGoldText()
    {
        if (_totalGoldText == null) return;

        if (CurrentMode == TradeMode.Sell) //Sell mode
        {
            int total = 0;
            foreach (var slot in _vendorView.GetAllConcatSlots())
            {
                if (slot.Item != null)
                    total += CalculateItemPrice(slot.Item, SellMultiplier);
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
        if ( _playerInventory != null && _playerInventory.GoldText != null)
            _playerInventory.GoldText.text = $"{_playerInventory.PlayerGold}";
    }
    private string GetRandomPhrase(string[] phrases) => phrases[UnityEngine.Random.Range(0, phrases.Length)];

    public void SellAllItems()
    {
        if (CurrentMode != TradeMode.Sell)
            return;

        int totalValue = 0;
        List<Item> itemsToRemove = new();

        foreach (var slot in _vendorView.GetAllConcatSlots())
        {
            if (slot.Item != null)
            {
                int itemPrice = CalculateItemPrice(slot.Item, SellMultiplier);
                totalValue += itemPrice;
                itemsToRemove.Add(slot.Item);
            }
        }

        _playerInventory.ChangePlayerGold(totalValue);

        foreach (var item in itemsToRemove)
        {
            _vendorModel.RemoveItem(item);
            _vendorView.GetSlotWithItem(item).ClearSlot();
        }

        Debug.Log($"<color=green>Sold all items for <color=cyan>{totalValue}</color> gold!</color>");
        Debug.Log(GetRandomPhrase(_sellPhrases));

        UpdatePlayerGoldText();
        UpdateTotalGoldText();
        UpdateSellAllButtonState();
    }

    private void UpdateSellAllButtonState()
    {
        if (_sellAllButton != null && CurrentMode == TradeMode.Sell)
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
    public void SetBuyMultiplicator(int amount) => BuyMultiplier = Mathf.Max(0, amount);
    public void SetSellMultiplicator(int amount) => SellMultiplier = Mathf.Max(0, amount);
    public void SetItemToBuy(int minAmount, int maxAmount) => RandomItemsCount = new Vector2Int(minAmount, maxAmount);
    public bool IsVendorOpen() => _vendorView.Inventory.activeInHierarchy;
    #endregion

    #region Vendor logic
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
            var slot = slots.FirstOrDefault(s => s.Item != null && s.Item.Id == item.Id && !s.IsEmpty());
            if (slot != null)
            {
                _playerInventory.AddItem(item);
                _vendorModel.RemoveItem(item);

                slot.ClearSlot();
                LogReturnedItem(item);
            }
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
        Debug.Log($"<color=yellow>[Returned item]</color>: <color=cyan>{item.Id}</color>, Count: <color=cyan>{item.Count}</color>, Rarity: <color={rarityColor}>{item.Rarity}</color>, Type: {item.GetType()}");
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


    private void CloseVendorItemInfo()
    {
        if (CurrentMode == TradeMode.Buy)
        {
            _itemGoldText.gameObject.SetActive(false);
            _itemGoldText.text = "";
        }
        else
        {

        }
        _playerInventory.CloseItemInfo();
    }

    private void ShowVendorItemInfo(Item item)
    {
        _playerInventory.ShowItemInfo(item);

        if (CurrentMode == TradeMode.Buy)
        {
            var itemPrice = CalculateItemPrice(item, BuyMultiplier);

            _itemGoldText.gameObject.SetActive(true);
            _itemGoldText.text = $"Item price: {CalculateItemPrice(item, BuyMultiplier)}";

            bool noMoney = itemPrice > _playerInventory.PlayerGold;
            _itemGoldText.color = noMoney ? Color.red : Color.white;

            if (noMoney)
                AnimateNoMoney();
        }
        else
        {
            _playerInventory._itemDescriptionText.text = $"Price: {CalculateItemPrice(item, SellMultiplier)} gold";
        }

    }
    #endregion

    #region Drag & Drop
    private void OnVendorDrag(PointerEventData data, Item item, InventorySlot slot)
    {
        if (item == null || slot == null)
        {
            return;
        }

        if (CurrentMode == TradeMode.Buy)
        {
            if (!BuySlots.Contains(slot))
            {
                Debug.Log("In Buy mode, you can only drag items from the purchase slots!");
                return;
            }
            int itemPrice = CalculateItemPrice(item, BuyMultiplier);
            if (_playerInventory.PlayerGold < itemPrice)
            {
                Debug.Log($"<color=red>Not enough <color=cyan>{itemPrice - _playerInventory.PlayerGold}</color> gold</color>");
                _playerInventory.ResetDrag();
                AnimateNoMoney();
                return;
            }
            _playerInventory.DragItem(data, item, slot);
        }
        else if (CurrentMode == TradeMode.Sell)
        {
            _playerInventory.DragItem(data, item, slot);
        }
    }

    private void OnVendorDrop(PointerEventData data, Item item, InventorySlot targetSlot)
    {

        Item draggedItem = _playerInventory.DragableItem;
        var sourceSlot = _playerInventory.LastInteractSlot;

        if (draggedItem == null || sourceSlot == null)
        {
            UpdateTotalGoldText();
            UpdateSellAllButtonState();
            _playerInventory.ResetDrag();
            return;
        }

        if (targetSlot == sourceSlot || targetSlot == null)
        {
            if (CurrentMode == TradeMode.Buy)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(_playerInventory.View.Inventory.GetComponent<RectTransform>(), data.position))
                {
                    Debug.Log("Drag the item to the inventory area to sell it.");
                    _playerInventory.ResetDrag();
                    return;
                }

                HandleBuy(data, item, targetSlot);
                return;
            }


            Debug.Log("Item dropped on itself or outside inventory - ignoring");
            _playerInventory.ResetDrag();
            return;
        }

        Debug.Log($"Dragging: {draggedItem}, Target: {targetSlot}, Source: {sourceSlot}");

        if (CurrentMode == TradeMode.Buy)
        {
            if (!BuySlots.Contains(sourceSlot))
            {
                Debug.Log("In Buy mode, you can only drag items from vendor's Buy slots!");
                _playerInventory.ResetDrag();
                return;
            }

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



    private void AnimateNoMoney()
    {
        if (DOTween.IsTweening(_itemGoldText.transform))
            return;

        _itemGoldText.transform.DOShakePosition(0.5f, strength: 5f, vibrato: 10, randomness: 90, snapping: false, fadeOut: true);
    }
    private void HandleBuy(PointerEventData data, Item item, InventorySlot targetSlot)
    {
        int itemPrice = CalculateItemPrice(item, BuyMultiplier);

        var emptySlot = _playerInventory.View.GetFirstEmptySlot();
        if (emptySlot == null )
        {
            Debug.Log("<color=yellow>[Vendor]</color>: <color=red>Not empty space in inventory!</color>");
            _playerInventory.ResetDrag();
            return;
        }



        if(!InventoryDragDropHandler.IsValid(item, emptySlot))
        {
            Debug.Log("<color=yellow>[Vendor]</color>: <color=red>SLOT IS NOT VALID</color>");
            _playerInventory.ResetDrag();
            return;
        }


        emptySlot.InitializeSlot(item);
        BuyItems.Remove(item);
        targetSlot.ClearSlot();

        _playerInventory.ChangePlayerGold(-itemPrice);
        UpdatePlayerGoldText();
        UpdateTotalGoldText();

        if (emptySlot.SlotType == SlotType.Weapon && item is Weapon weapon)
        {
            _playerInventory.CombatSystem.SetWeapon(weapon);
        }
        if (emptySlot.SlotType == SlotType.SecondaryWeapon && item is SecondaryWeapon secondaryWeapon)
        {
            _playerInventory.CombatSystem.SetSecondaryWeapon(secondaryWeapon);
        }
        if (emptySlot.SlotType == SlotType.Artifact1 && item is Artifact artifact1)
        { 
            _playerInventory.CombatSystem.SetFirstArtifact(artifact1);
        }
        if(emptySlot.SlotType == SlotType.Artifact2 && item is Artifact artifact2)
        {
            _playerInventory.CombatSystem.SetSecondArtifact(artifact2);
        }

        Debug.Log(GetRandomPhrase(_buyPhrases));
        _playerInventory.ResetDrag();
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
            Debug.Log("test");
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
    #endregion
}
