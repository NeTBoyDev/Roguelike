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

public class Anvil : MonoBehaviour
{
    [field: Header("Main Settings")]
    [field: SerializeField] public float AnvilCloseDistance { get; private set; } = 3f;
    [SerializeField] private GameObject _anvilPanel;
    [SerializeField, ReadOnly] private Inventory _playerInventory;

    [field: HorizontalLine(2, EColor.Green)]
    [field: Header("Upgrade Settings")]
    [field: SerializeField, MinValue(0), MaxValue(200)] public int BaseUpgradeChance { get; private set; } = 100; //Уменьшить до 50%

    [Header("UI")]
    [SerializeField] private InventorySlot _gemSlot; //Слот для гема
    [SerializeField] private InventorySlot _weaponSlot; //Слот для оружия
    [SerializeField] private InventorySlot _finalSlot; //Слот после успешного улучшения (разницы нет где будет предмет, все равно вернется в инвентарь)

    [SerializeField] private Button _upgradeButton;
    [SerializeField] private Image _progressImage; //Картинка прогресса улучшения
    [SerializeField] private TMP_Text _statusText; //Текст статуса (опционально)

    [field: Tooltip("Время заполнения прогресса в секундах")]
    [field: SerializeField, MinValue(0)] public float UpgradeDuration { get; private set; } = 2f;

    private bool _isUpgrading = false;
    private Tween _upgradeTween;

    #region Initialize
    private void Start()
    {
        FindPlayer();
        SubscribeEvents();

        _anvilPanel.SetActive(false);
        _progressImage.fillAmount = 0f;
        UpdateButtonState();
    }

    private void FindPlayer()
    {
        if (_playerInventory == null)
            _playerInventory = FindObjectOfType<Inventory>();
    }

    private void OnEnable()
    {
        _upgradeButton.onClick.RemoveAllListeners();
        _upgradeButton.onClick.AddListener(StartUpgrade);
    }

    private void OnDisable()
    {
        InventorySlot[] allSlots = { _gemSlot, _weaponSlot, _finalSlot };

        foreach (var slot in allSlots)
        {
            slot.onDrag -= OnAnvilDrag;
            slot.onDrop -= OnAnvilDrop;
            slot.onPointerEnter -= ShowItemInfo;
            slot.onPointerExit -= CloseItemInfo;
        }
        CancelUpgrade();
    }

    private void SubscribeEvents()
    {
        InventorySlot[] allSlots = {_gemSlot, _weaponSlot, _finalSlot};

        foreach (var slot in allSlots)
        {
            slot.onDrag += OnAnvilDrag;
            slot.onDrop += OnAnvilDrop;
            slot.onPointerEnter += ShowItemInfo;
            slot.onPointerExit += CloseItemInfo;
        }
    }
    #endregion

    #region Anvil Panel Logic
    public void OpenAnvil()
    {
        FindPlayer();

        _progressImage.fillAmount = 0f;

        _anvilPanel.SetActive(true);

        _playerInventory.InventorySetAcitve(true);
        _playerInventory.UpdateCursorState(true);

        UpdateButtonState();
        UpdateTextsState();
    }

    public void CloseAnvil()
    {
        if (_isUpgrading)
        {
            CancelUpgrade();
            TryCompleteUpgrade();
        }

        _anvilPanel.SetActive(false);

        ReturnItemsToPlayer();

        _playerInventory.InventorySetAcitve(false);
        _playerInventory.UpdateCursorState(false);
        _playerInventory.ResetDrag();
    }

    private void ReturnItemsToPlayer()
    {
        if (!_gemSlot.IsEmpty())
        {
            _playerInventory.AddItem(_gemSlot.Item);
            _gemSlot.ClearSlot();
        }
        if (!_weaponSlot.IsEmpty())
        {
            _playerInventory.AddItem(_weaponSlot.Item);
            _weaponSlot.ClearSlot();
        }

        if (!_finalSlot.IsEmpty())
        {
            _playerInventory.AddItem(_finalSlot.Item);
            _finalSlot.ClearSlot();
        }
    }
    #endregion

    #region Drag & Drop
    private void OnAnvilDrag(PointerEventData data, Item item, InventorySlot slot)
    {
        if (item == null || slot == null || _isUpgrading)
        {
            return;
        }
        _playerInventory.DragItem(data, item, slot);
    }

    private void OnAnvilDrop(PointerEventData data, Item item, InventorySlot targetSlot)
    {
        if (_isUpgrading)
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
            Debug.Log("This item cannot be placed in the anvil slot!");
            _playerInventory.ResetDrag();
            return;
        }

        if(draggedItem is not Gem && targetSlot == _gemSlot)
        {
            Debug.Log("This is not a gem.");
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

        if (draggedItem is Weapon && sourceSlot.SlotType == SlotType.Weapon)
        {
            _playerInventory.RemoveWeapon();
        }

        _playerInventory.ResetDrag();
    }
    #endregion

    #region Upgrade Logic
    private async void UpdateButtonState(int delay = 0)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));

        var bothSlotFilled = BothSlotsFilled();
        _upgradeButton.interactable = bothSlotFilled && IsWeaponValid(_weaponSlot.Item, out string _);

        UpdateTextsState();
    }

    private void UpdateTextsState()
    {
        var item = _weaponSlot.Item;
        if (item == null)
        {
            _statusText.text = "";
            return;
        }

        if (BothSlotsFilled() && IsWeaponValid(item, out string validMessage))
        {
            int chance = CalculateUpgradeChance(_gemSlot.Item);
            _statusText.text = chance == 0 ? "" : $"Upgrade chance {chance}%";
        }
        else if(!IsWeaponValid(item, out string noValidMessage))
        {
            _statusText.text = $"<color=red>{noValidMessage}</color>";
        }
        else
        {
            _statusText.text = "";
        }
    }
    private bool BothSlotsFilled() => !_isUpgrading && !_gemSlot.IsEmpty() && !_weaponSlot.IsEmpty() &&
                                      _gemSlot.Item is Gem && _weaponSlot.Item is Weapon;

    private bool IsWeaponValid(Item item, out string ValidMessage)
    {
        if (item == null)
        {
            ValidMessage = string.Empty;
            return false;
        }

        if (item.Rarity == _Project.Develop.Core.Enum.Rarity.Common)
        {
            ValidMessage = "Item rarity cannot be common!";
            return false;
        }

        if (item.Effects.Count >= (int)item.Rarity)
        {
            ValidMessage = "No available gem slots!";
            return false;
        }

        ValidMessage = string.Empty;
        return true;
    }

    private int CalculateUpgradeChance(Item gem)
    {
        if (gem == null)
            return 0;

        int baseChance = BaseUpgradeChance;
        int finalChance = baseChance - ((int)gem.Rarity * 10);
        return finalChance;
    }

    private void StartUpgrade()
    {
        // Проверяем условия для выхода
        if (_isUpgrading || _gemSlot.IsEmpty() || _weaponSlot.IsEmpty())
        {;
            return;
        }

        // Проверяем, что _progressImage задан
        if (_progressImage == null)
        {
            return;
        }

        _isUpgrading = true;
        _upgradeButton.interactable = false;
        _progressImage.fillAmount = 0f;

        if (_statusText != null)
        {
            _statusText.text = "Process...";
        }

        Item item = _weaponSlot.Item;
        float durationMultiplier = (item != null) ? GameData.Rarity[item.Rarity] : 1f;

        float upgradeDuration = UpgradeDuration * durationMultiplier;

        _upgradeTween = _progressImage.DOFillAmount(1f, upgradeDuration)
            .OnComplete(() => TryCompleteUpgrade());
    }
    public void CancelUpgrade()
    {
        if (_upgradeTween != null && _upgradeTween.IsActive())
        {
            _upgradeTween.Kill();
            _progressImage.fillAmount = 0f;
        }

        _isUpgrading = false;
        _upgradeButton.interactable = true;

        if (_statusText != null)
            _statusText.text = "Upgrade canceled";

        ReturnItemsToPlayer();
    }
    private void TryCompleteUpgrade()
    {
        _isUpgrading = false;
        bool success = CalculateUpgradeSuccess(_gemSlot.Item);

        Weapon weapon = _weaponSlot.Item as Weapon;
        Gem gem = _gemSlot.Item as Gem;

        if (weapon == null || gem == null)
            return;

        if (success)
        {
            if (weapon.Effects.Count < (int)weapon.Rarity)
            {
                weapon.ApplyEffect(gem.Effects[0]);

                if (gem.projectile != null)
                    weapon.AddProjectile(gem.projectile);
            }

            if (_statusText != null)
                _statusText.text = "<color=green>Success!</color>";

            MoveItemInFinalSlot(weapon);
        }
        else
        {
            if (_statusText != null)
                _statusText.text = "<color=red>Failed!</color>";
        }

        _gemSlot.ClearSlot();
        _progressImage.fillAmount = 0f;

        UpdateButtonState(2);
    }

    private bool CalculateUpgradeSuccess(Item gem)
    {
        if (gem == null)
            return false;

        int successChance = CalculateUpgradeChance(gem);
        int randomValue = UnityEngine.Random.Range(0, 101);

        return randomValue < successChance;
    }

    private void MoveItemInFinalSlot(Item item)
    {
        if (item == null)
            return;

        _finalSlot.InitializeSlot(item);
        _weaponSlot.ClearSlot();
    }

    public bool IsAnvilOpen() => _anvilPanel.activeInHierarchy;
    #endregion

    #region Handlers

    private void ShowItemInfo(Item item) => _playerInventory.ShowItemInfo(item);
    private void CloseItemInfo() => _playerInventory.CloseItemInfo();

    #endregion
}
