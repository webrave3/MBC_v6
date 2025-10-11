using System;
using System.Collections.Generic;
using System.Linq;
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

            // Initialize inventory with nulls to represent empty slots
            for (int i = 0; i < inventorySize; i++)
            {
                items.Add(null);
            }
        }

        public bool AddItem(ResourceType type, int amount)
        {
            // First, try to stack with existing items
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].itemType == type)
                {
                    items[i].amount += amount;
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            // If no existing stack, find the first empty slot
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                {
                    items[i] = new InventoryItem(type, amount);
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }

            Debug.LogWarning($"Inventory is full! Could not add {amount} of {type.resourceName}.");
            return false;
        }

        public void SwapItems(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= items.Count || indexB < 0 || indexB >= items.Count)
            {
                Debug.LogError("Invalid index for item swap.");
                return;
            }

            InventoryItem temp = items[indexA];
            items[indexA] = items[indexB];
            items[indexB] = temp;

            OnInventoryChanged?.Invoke();
        }

        public void RemoveItem(ResourceType type, int amount)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (amount <= 0) break; // Exit if we've removed enough

                if (items[i] != null && items[i].itemType == type)
                {
                    if (items[i].amount > amount)
                    {
                        items[i].amount -= amount;
                        amount = 0;
                    }
                    else
                    {
                        amount -= items[i].amount;
                        items[i] = null;
                    }
                }
            }
            OnInventoryChanged?.Invoke();
        }

        public int GetItemAmount(ResourceType type)
        {
            // Correctly sums up the amount from ALL stacks of the item.
            return items.Where(item => item != null && item.itemType == type).Sum(item => item.amount);
        }
    }
}

