using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class InventoryModel
{
    [field: SerializeField] public List<ItemTest> Items { get; private set; } = new(1);
    [field: SerializeField] public ItemTest SelectedItem { get; private set; } = null;

    #region Items

    public void AddItem(ItemTest item)
    {
        if (Items.Contains(item))
            return;

        Items.Add(item);
    }

    public void RemoveItem(ItemTest item)
    {
        if (Items.Contains(item))
        {
            Items.Remove(item);
            if (SelectedItem == item)
                SelectedItem = null;
        }
    }

    public bool ContainsItem(ItemTest item) => Items.Contains(item);
    public ItemTest FindItem(string itemId) => Items.FirstOrDefault(i => i != null && i.Id == itemId);

    public void SetSelectedItem(ItemTest item) => SelectedItem = item;

    #endregion
}
