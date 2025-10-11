using UnityEngine;
using AutoForge.Player;

namespace AutoForge.Core
{
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

        public void AddResource(ResourceType type, int amount)
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.AddItem(type, amount);
            }
        }

        public bool HasResource(ResourceType type, int amount)
        {
            if (PlayerInventory.Instance != null)
            {
                return PlayerInventory.Instance.GetItemAmount(type) >= amount;
            }
            return false;
        }

        public void SpendResource(ResourceType type, int amount)
        {
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.RemoveItem(type, amount);
            }
        }

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

