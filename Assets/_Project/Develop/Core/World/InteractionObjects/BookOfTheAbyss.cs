using _Project.Develop.Core.Entities;
using _Project.Develop.Core.Enum;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using NaughtyAttributes;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BookOfTheAbyss : MonoBehaviour
{
    [field: Header("Main Settings")]
    [field: SerializeField] public float BookCloseDistance { get; private set; } = 3f;
    [SerializeField] private GameObject _bookPanel;
    [SerializeField, ReadOnly] private Inventory _playerInventory;
    [SerializeField, MinValue(0)] private float _useDuration;

    [Header("UI")]
    [SerializeField] private InventorySlot _mapSlot;
    [SerializeField] private Button _useButton;
    [SerializeField] private Image _progressImage;
    [SerializeField] private TMP_Text _statusText;

    [field: SerializeField] public ParticleSystem PortalEffect { get; private set; } = null;

    private Tween _upgradeTween;

    private bool _isUsing = false;

    #region Initialize
    private void Start()
    {
        FindPlayer();
        SubscribeEvents();

        _bookPanel.SetActive(false);
        UpdateButtonState();
    }

    private void FindPlayer()
    {
        if (_playerInventory == null)
            _playerInventory = FindObjectOfType<Inventory>();
    }

    private void OnEnable()
    {
        _useButton.onClick.RemoveAllListeners();
        _useButton.onClick.AddListener(StartUse);
    }

    private void OnDisable()
    {
        _mapSlot.onDrag -= OnBookDrag;
        _mapSlot.onDrop -= OnBookDrop;
        _mapSlot.onPointerEnter -= ShowItemInfo;
        _mapSlot.onPointerExit -= CloseItemInfo;
    }

    private void SubscribeEvents()
    {
        _mapSlot.onDrag += OnBookDrag;
        _mapSlot.onDrop += OnBookDrop;
        _mapSlot.onPointerEnter += ShowItemInfo;
        _mapSlot.onPointerExit += CloseItemInfo;
    }
    #endregion

    #region Book Panel Logic
    public void OpenBook()
    {
        FindPlayer();

        _bookPanel.SetActive(true);

        _playerInventory.InventorySetAcitve(true);
        _playerInventory.UpdateCursorState(true);

        UpdateButtonState();
        UpdateTextsState();
    }

    public void CloseBook()
    {
        _bookPanel.SetActive(false);

        if(!_isUsing)
            ReturnItemsToPlayer();

        _playerInventory.InventorySetAcitve(false);
        _playerInventory.UpdateCursorState(false);
        _playerInventory.ResetDrag();
    }

    private void ReturnItemsToPlayer()
    {
        if (!_mapSlot.IsEmpty())
        {
            _playerInventory.AddItem(_mapSlot.Item);
            _mapSlot.ClearSlot();
        }
    }
    #endregion

    #region Drag & Drop
    private void OnBookDrag(PointerEventData data, Item item, InventorySlot slot)
    {
        if (item == null || slot == null || _isUsing)
        {
            return;
        }
        _playerInventory.DragItem(data, item, slot);
    }

    private void OnBookDrop(PointerEventData data, Item item, InventorySlot targetSlot)
    {
        if (_isUsing)
        {
            _playerInventory.ResetDrag();
            return;
        }

        Item draggedItem = _playerInventory.DragableItem;
        var sourceSlot = _playerInventory.LastInteractSlot;

        if (draggedItem == null || sourceSlot == null)
        {
            _playerInventory.ResetDrag();
            UpdateButtonState();
            return;
        }

        if (targetSlot == sourceSlot || targetSlot == null)
        {
            _playerInventory.ResetDrag();
            UpdateButtonState();
            return;
        }

        if (!InventoryDragDropHandler.IsValid(draggedItem, targetSlot))
        {
            Debug.Log("This item cannot be placed in the book slot!");
            _playerInventory.ResetDrag();
            return;
        }

        if (draggedItem is not Map)
        {
            Debug.Log("This is not a map.");
            _playerInventory.ResetDrag();
            return;
        }

        if (targetSlot.IsEmpty())
        {
            var item2 = draggedItem;

            targetSlot.InitializeSlot(item2);
            _playerInventory.RemoveItem(item2);

            sourceSlot.ClearSlot();
        }
        else
        {
            Item targetItem = targetSlot.Item;
            targetSlot.InitializeSlot(draggedItem);
            sourceSlot.InitializeSlot(targetItem);
        }

        targetSlot.UpdateVisual();
        sourceSlot.UpdateVisual();

        UpdateButtonState();

        _playerInventory.ResetDrag();
    }
    #endregion

    #region Use Logic
    private async void UpdateButtonState(int delay = 0)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));

        var slotFilled = SlotFilled();
        _progressImage.fillAmount = 0f;

        UpdateTextsState();
    }

    private void UpdateTextsState()
    {
        var item = _mapSlot.Item;
        if (item == null)
        {
            _statusText.text = "";
            return;
        }

        if (SlotFilled())
        {
            _statusText.text = "Ready to use";
        }
        else if (!IsMapValid(item, out string notValidMessage))
        {
            _statusText.text = $"{notValidMessage}";
        }
    }
    private bool IsMapValid(Item item, out string ValidMessage)
    {
        if (item == null)
        {
            ValidMessage = string.Empty;
            return false;
        }

        if (item is not Map)
        {
            ValidMessage = "<color=red>The item must be a map!</color>";
            return false;
        }

        ValidMessage = string.Empty;
        return true;
    }
    private bool SlotFilled() => !_isUsing && !_mapSlot.IsEmpty() && _mapSlot.Item is Map;

    private void StartUse()
    {
        if (_isUsing)
            return;

        if (_mapSlot.IsEmpty())
        {
            var defaultMap = new Map(Rarity.Common, "Map");
            _mapSlot.SetItem(defaultMap);
        }

        _isUsing = true;
        _useButton.interactable = false;

        if (_statusText != null)
        {
            _statusText.text = "Using...";
        }

        Item item = _mapSlot.Item;
        float durationMultiplier = (item != null) ? GameData.Rarity[item.Rarity] : 1f;

        float useDuration = _useDuration * durationMultiplier;

        _upgradeTween = _progressImage.DOFillAmount(1f, useDuration).OnComplete(() => CompleteUse());
        
        GameData._map = _mapSlot.Item as Map;
        
    }

    private void CompleteUse()
    {
        _isUsing = false;

        if (_mapSlot.Item is not Map map)
            return;

        if (_statusText != null)
            _statusText.text = "<color=green>Portal is open!</color>";

        _mapSlot.ClearSlot();

        if(PortalEffect != null)
            PortalEffect.Play();

        UpdateButtonState(5);
    }

    public bool IsBookOpen() => _bookPanel.activeInHierarchy;
    #endregion

    #region Handlers
    private void ShowItemInfo(Item item) => _playerInventory.ShowItemInfo(item);
    private void CloseItemInfo() => _playerInventory.CloseItemInfo();
    #endregion
}