using NaughtyAttributes;
using System;
using _Project.Develop.Core.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum SlotType
{
    Default,
    Hotbar,
    Weapon,
    SecondaryWeapon,
    Artifact,
}

public class InventorySlot : MonoBehaviour, IInventorySlot, IDragHandler, IDropHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [field: SerializeField] public SlotType SlotType {  get; private set; } = SlotType.Default;
    [field: SerializeField] public Item Item { get; private set; } = null;
    [field: SerializeField] public Image Image { get; private set; } = null;
    [field: SerializeField] public Sprite DefaultSprite { get; private set; } = null;
    [field: SerializeField] public TMP_Text CountText { get; private set; } = null;

    [field: Header("Hotbar")]
    [field: SerializeField, Required, ShowIf(nameof(SlotType), SlotType.Hotbar)] public Image OuterHotbarImage { get; private set; } = null;
    [field: SerializeField, Required, ShowIf(nameof(SlotType), SlotType.Hotbar)] public Image OuterHotbarIcon { get; private set; } = null;
    [field: SerializeField, Required, ShowIf(nameof(SlotType), SlotType.Hotbar)] public TMP_Text OuterHotbarCountText { get; private set; } = null;
    [field: SerializeField, Required, ShowIf(nameof(SlotType), SlotType.Hotbar)] public Image SelectionHighlight {  get; private set; } = null;

    public event Action<PointerEventData, Item, InventorySlot> onDrag;
    public event Action<PointerEventData, Item, InventorySlot> onDrop;
    public event Action<PointerEventData, Item, InventorySlot> onRightClick;
    

    public void InitializeSlot(Item item)
    {
        if (item == null) 
            return;

        Item = item;
        UpdateVisual();
    }

    public void ClearSlot()
    {
        Item = null;
        UpdateVisual();
    }

    public void SetSelected(bool isSelected)
    {
        if(SlotType == SlotType.Hotbar && SelectionHighlight != null)
        {
            SelectionHighlight.enabled = isSelected;
        }
    }

    public void UpdateVisual()
    {
        if(Item == null)
        {
            Image.color = new Color(0.8f, 0.8f, 0.8f, 1);
            Image.sprite = DefaultSprite;
            if (CountText != null)
                CountText.text = "";

            if (SlotType == SlotType.Hotbar)
            {
                OuterHotbarIcon.color = new Color(0.8f, 0.8f, 0.8f, 1);
                OuterHotbarIcon.sprite = DefaultSprite;
                OuterHotbarCountText.text = "";
            }
            return;
        }

        Image.sprite = Item.Sprite == null ? DefaultSprite : Item.Sprite;
        Image.color = new Color(0.8f, 0.8f, 0.8f, 1);

        if (CountText != null)
        {
            CountText.text = (Item.Count > 1) ? Item.Count.ToString() : "";
        }

        if (SlotType == SlotType.Hotbar)
        {
            OuterHotbarIcon.sprite = Item == null ? DefaultSprite : Item.Sprite;
            OuterHotbarIcon.color = new Color(0.8f, 0.8f, 0.8f, 1);

            OuterHotbarCountText.text = (Item.Count > 1) ? Item.Count.ToString() : "";
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsEmpty() || string.IsNullOrWhiteSpace(Item.Id))
            return;

        onDrag?.Invoke(eventData, Item, this);
    }
    public void OnEndDrag(PointerEventData eventData) => onDrop?.Invoke(eventData, Item, this);

    public void OnDrop(PointerEventData eventData) => onDrop?.Invoke(eventData, Item, this);
    public bool IsEmpty() => Item == null;

    public event Action<Item> onPointerEnter;
    public event Action onPointerExit;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(Item != null)
            onPointerEnter?.Invoke(Item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onPointerExit?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Right)
        {
            onRightClick?.Invoke(eventData, Item, this);
        }
    }
}
