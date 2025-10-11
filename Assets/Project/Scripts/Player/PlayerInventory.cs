using System;
using System.Collections.Generic;
using UnityEngine;
using AutoForge.Core;

namespace AutoForge.Player
{
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        public List<InventoryItem> items = new List<InventoryItem>();
        public int inventorySize = 20;

        public event Action OnInventoryChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            for (int i = 0; i < inventorySize; i++)
            {
                items.Add(null);
            }
        }

        public bool AddItem(ResourceType type, int amount)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].itemType == type)
                {
                    items[i].amount += amount;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                {
                    items[i] = new InventoryItem(type, amount);
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
            return false;
        }

        // --- NEW METHODS ---

        // Add this new method inside the PlayerInventory class:

        public void SwapItems(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= items.Count || indexB < 0 || indexB >= items.Count)
            {
                Debug.LogError("Invalid index for item swap.");
                return;
            }

            // Simple swap logic
            InventoryItem temp = items[indexA];
            items[indexA] = items[indexB];
            items[indexB] = temp;

            // IMPORTANT: Notify the UI that the inventory has changed so it can redraw.
            OnInventoryChanged?.Invoke();
        }


        public bool HasItem(ResourceType type, int amount)
        {
            foreach (var item in items)
            {
                if (item != null && item.itemType == type && item.amount >= amount)
                {
                    return true;
                }
            }
            return false;
        }

        public void RemoveItem(ResourceType type, int amount)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].itemType == type)
                {
                    items[i].amount -= amount;
                    if (items[i].amount <= 0)
                    {
                        items[i] = null; // Remove the item if stack is empty
                    }
                    OnInventoryChanged?.Invoke();
                    return;
                }
            }
        }

        public int GetItemAmount(ResourceType type)
        {
            foreach (var item in items)
            {
                if (item != null && item.itemType == type)
                {
                    return item.amount;
                }
            }
            return 0;
        }
    }
}
