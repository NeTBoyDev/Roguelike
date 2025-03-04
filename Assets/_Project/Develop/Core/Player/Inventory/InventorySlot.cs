using NaughtyAttributes;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IInventorySlot, IDragHandler, IDropHandler, IEndDragHandler
{
    [field: SerializeField] public ItemTest Item { get; private set; } = null;
    [field: SerializeField] public Image Image { get; private set; } = null;
    [field: SerializeField] public Sprite DefaultSprite { get; private set; } = null;
    [field: SerializeField] public TMP_Text CountText { get; private set; } = null;

    [field: Header("Hotbar")]
    [field: SerializeField] public bool IsHotBar {  get; private set; } = false;
    [field: SerializeField, Required, ShowIf(nameof(IsHotBar))] public Image OuterHotbarImage { get; private set; } = null;
    [field: SerializeField, Required, ShowIf(nameof(IsHotBar))] public Image OuterHotbarIcon { get; private set; } = null;
    [field: SerializeField, Required, ShowIf(nameof(IsHotBar))] public TMP_Text OuterHotbarCountText { get; private set; } = null;
    [field: SerializeField, Required, ShowIf(nameof(IsHotBar))] public Image SelectionHighlight {  get; private set; } = null;

    public event Action<PointerEventData, ItemTest, InventorySlot> onDrag;
    public event Action<PointerEventData, ItemTest, InventorySlot> onDrop;

    public void InitializeSlot(ItemTest item)
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
        if(IsHotBar && SelectionHighlight != null)
        {
            SelectionHighlight.enabled = isSelected;
        }
    }

    public void UpdateVisual(bool isHotbar = false)
    {
        if(Item == null)
        {
            Image.color = new Color(0.8f, 0.8f, 0.8f, 1);

            if (CountText != null)
                CountText.text = "";

            if (IsHotBar)
            {
                OuterHotbarIcon.color = new Color(0.8f, 0.8f, 0.8f, 1);

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

        if (IsHotBar)
        {
            OuterHotbarIcon.sprite = Item.Sprite == null ? DefaultSprite : Item.Sprite;
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

}
