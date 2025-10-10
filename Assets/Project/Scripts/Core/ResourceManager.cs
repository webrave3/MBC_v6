using System.Collections.Generic;
using UnityEngine;
using System; // For the Action

namespace AutoForge.Core
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        // The core of our inventory: a dictionary that maps a ResourceType to an amount.
        private Dictionary<ResourceType, int> resourceInventory = new Dictionary<ResourceType, int>();

        // Event to notify other scripts (like the UI) when the inventory changes
        public static event Action OnResourcesChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void AddResource(ResourceType type, int amount)
        {
            if (resourceInventory.ContainsKey(type))
            {
                resourceInventory[type] += amount;
            }
            else
            {
                resourceInventory.Add(type, amount);
            }
            Debug.Log($"Added {amount} {type.resourceName}. New total: {resourceInventory[type]}");
            OnResourcesChanged?.Invoke(); // Fire the event
        }

        public bool HasResource(ResourceType type, int amount)
        {
            return resourceInventory.ContainsKey(type) && resourceInventory[type] >= amount;
        }

        public void SpendResource(ResourceType type, int amount)
        {
            if (HasResource(type, amount))
            {
                resourceInventory[type] -= amount;
                Debug.Log($"Spent {amount} {type.resourceName}. Remaining: {resourceInventory[type]}");
                OnResourcesChanged?.Invoke(); // Fire the event
            }
        }

        public int GetResourceAmount(ResourceType type)
        {
            if (resourceInventory.ContainsKey(type))
            {
                return resourceInventory[type];
            }
            return 0;
        }
    }
}