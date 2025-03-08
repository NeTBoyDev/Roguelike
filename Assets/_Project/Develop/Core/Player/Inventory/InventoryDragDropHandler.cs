using _Project.Develop.Core.Entities;
using UnityEngine;
using UnityEngine.EventSystems;

public static class InventoryDragDropHandler
{
    public static void HandleDrop(PointerEventData eventData, Item item, InventorySlot targetSlot, Inventory inventory)
    {
        if (!IsValidDrop(inventory.DragableItem, inventory.LastInteractSlot, targetSlot))
        {
            inventory.ResetDrag();
            return;
        }

        if (inventory.LastInteractSlot.Item is Gem && targetSlot.Item is Weapon && targetSlot.Item.Effects.Count < (int)inventory.LastInteractSlot.Item.Rarity)
        {
            targetSlot.Item.ApplyEffect(inventory.LastInteractSlot.Item.Effects[0]);
            inventory.RemoveItem(inventory.LastInteractSlot.Item);
            inventory.ResetDrag();
            return;     
        }

        HandleWeaponSlots(inventory, targetSlot);
        HandleSecondaryWeaponSlots(inventory, targetSlot);
        HandleArtifactSlots(inventory,targetSlot);


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
            SlotType.Artifact1 => item is Artifact,
            SlotType.Artifact2 => item is Artifact,
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
    
    private static void HandleArtifactSlots(Inventory inventory, InventorySlot targetSlot)
    {
        if (inventory.DragableItem is Artifact art1 && targetSlot.SlotType == SlotType.Artifact1)
            inventory.CombatSystem.SetFirstArtifact(art1);
        
        if (inventory.DragableItem is Artifact art2 && targetSlot.SlotType == SlotType.Artifact2)
            inventory.CombatSystem.SetSecondArtifact(art2);

        if (inventory.LastInteractSlot.SlotType == SlotType.Artifact1)
        {
            inventory.CombatSystem.RemoveFirstArtifact();
            if (targetSlot.Item is Artifact a)
                inventory.CombatSystem.SetFirstArtifact(a);
        }
        
        if (inventory.LastInteractSlot.SlotType == SlotType.Artifact2)
        {
            inventory.CombatSystem.RemoveSecondArtifact();
            if (targetSlot.Item is Artifact a2)
                inventory.CombatSystem.SetSecondArtifact(a2);
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