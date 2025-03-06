using System;
using System.Collections.Generic;
using System.Linq;
using _Project.Develop.Core.Entities;
using UnityEngine;

[Serializable]
public class InventoryModel
{
    [field: SerializeField] public List<Item> Items { get; private set; } = new(1);
    [field: SerializeField] public Item SelectedItem { get; private set; } = null;

    #region Items

    public void AddItem(Item item)
    {
        if (Items.Contains(item))
            return;

        Items.Add(item);
    }

    public void RemoveItem(Item item)
    {
        if (item == null || !Items.Remove(item))
            return;

        if (SelectedItem == item)
            SelectedItem = null;
    }

    public bool ContainsItem(Item item) => Items.Contains(item);
    public Item FindItem(string itemId) => Items.FirstOrDefault(i => i != null && i.Id == itemId);

    public void SetSelectedItem(Item item) => SelectedItem = item;

    public void RemoveAllItems()
    {
        if (Items.Count <= 0)
            return;

        Items.Clear();
    }

    #endregion
}
