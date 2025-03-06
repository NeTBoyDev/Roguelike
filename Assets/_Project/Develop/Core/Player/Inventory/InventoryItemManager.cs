using System;
using _Project.Develop.Core;
using _Project.Develop.Core.Entities;
using UnityEngine;
using System.Linq;

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

        var container = ItemGenerator.Instance.GenerateContainer(item,true);
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
