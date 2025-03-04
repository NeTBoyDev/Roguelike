using _Project.Develop.Core.Entities;
using UnityEngine.UI;
public interface IInventorySlot
{
    public Image Image { get; }
    public void InitializeSlot(ItemTest item);
    public void ClearSlot();
}
