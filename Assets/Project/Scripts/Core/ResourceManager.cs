using UnityEngine;
using AutoForge.Player; // Required to access PlayerInventory

namespace AutoForge.Core
{
    /// <summary>
    /// Acts as a central point for managing resource transactions.
    /// It receives requests from other scripts and directs them to the actual inventory system.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Pass-through method to add a resource to the player's inventory.
        /// </summary>
        public void AddResource(ResourceType type, int amount)
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.AddItem(type, amount);
            }
        }

        /// <summary>
        /// Pass-through method to check if the player has enough of a resource.
        /// </summary>
        public bool HasResource(ResourceType type, int amount)
        {
            if (PlayerInventory.Instance != null)
            {
                return PlayerInventory.Instance.HasItem(type, amount);
            }
            return false;
        }

        /// <summary>
        /// Pass-through method to spend a resource from the player's inventory.
        /// </summary>
        public void SpendResource(ResourceType type, int amount)
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.RemoveItem(type, amount);
            }
        }

        /// <summary>
        /// Pass-through method to get the amount of a specific resource.
        /// </summary>
        public int GetResourceAmount(ResourceType type)
        {
            if (PlayerInventory.Instance != null)
            {
                return PlayerInventory.Instance.GetItemAmount(type);
            }
            return 0;
        }
    }
}
