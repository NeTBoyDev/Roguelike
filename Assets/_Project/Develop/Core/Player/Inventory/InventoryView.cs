using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Develop.Core.Entities;
using UnityEngine;

[Serializable]
public class InventoryView
{
    [field: SerializeField] public GameObject Inventory = null;
    [field: SerializeField] public List<InventorySlot> InventorySlots { get; private set; } = null;
    [field: SerializeField] public List<InventorySlot> HotbarSlots { get; private set; } = null;

    public async UniTask InventorySetActiveAsync(bool value, float delay = 0)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(delay));

        Inventory.SetActive(value);
    }

    public InventorySlot AddItemToFirstEmptySLot(Item item)
    {
        var freeSlot = InventorySlots.FirstOrDefault(s => s.Item == null || string.IsNullOrWhiteSpace(s.Item.Id));
        if (freeSlot == null)
            return null;

        freeSlot.InitializeSlot(item);
        return freeSlot;
    }

    public void ClearSlot(InventorySlot slot) => slot.ClearSlot();

    #region Utilities

    public IEnumerable<InventorySlot> GetAllConcatSlots() => InventorySlots.Concat(HotbarSlots);
    public InventorySlot GetFirstEmptySlot()
    {
        var slot = InventorySlots.FirstOrDefault(s => s.IsEmpty());
        if (slot != null)
            return slot;

        return HotbarSlots.FirstOrDefault(s => s.IsEmpty());
    }

    public InventorySlot GetSlotWithItem(Item item)
    {
        var slot = InventorySlots.FirstOrDefault(s => s.Item?.Id == item.Id);
        if (slot != null)
            return slot;

        return HotbarSlots.FirstOrDefault(s => s.Item?.Id == item.Id);
    }

    public InventorySlot GetSlotWithStackableItem(Item item)
    {
        var slot = InventorySlots.FirstOrDefault(s => s.Item?.Id == item.Id && s.Item.MaxStackSize > 1 && s.Item.Count < s.Item.MaxStackSize);
        if (slot != null)
            return slot;

        return HotbarSlots.FirstOrDefault(s => s.Item?.Id == item.Id && s.Item.MaxStackSize > 1 && s.Item.Count < s.Item.MaxStackSize);
    }

    public InventorySlot GetHotbarSlotWithItem(Item item) 
        => HotbarSlots.FirstOrDefault(s => !s.IsEmpty() && !string.IsNullOrWhiteSpace(s.Item?.Id) && s.Item.Id == item.Id);

    public IEnumerable<InventorySlot> GetAllHotbarSlots() => InventorySlots.Where(s => s.SlotType == SlotType.Hotbar);

    #endregion
}
