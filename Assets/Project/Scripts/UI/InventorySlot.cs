using UnityEngine;
using UnityEngine.EventSystems;
using AutoForge.Player;

namespace AutoForge.UI
{
    public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [HideInInspector] public int slotIndex;
        private static InventorySlot currentlyDraggedSlot;

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Check if there is an item in this slot to drag
            if (PlayerInventory.Instance.items[slotIndex] != null)
            {
                currentlyDraggedSlot = this;
                // Tell the UI to show the drag icon
                InventoryUI.Instance.StartDrag(slotIndex);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            // The InventoryUI will handle moving the icon
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            currentlyDraggedSlot = null;
            // Tell the UI to hide the drag icon
            InventoryUI.Instance.EndDrag();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (currentlyDraggedSlot != null)
            {
                // Tell the inventory backend to swap the items
                PlayerInventory.Instance.SwapItems(currentlyDraggedSlot.slotIndex, this.slotIndex);
            }
        }
    }
}
