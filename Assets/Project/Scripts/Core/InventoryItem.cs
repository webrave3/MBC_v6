using UnityEngine;
using AutoForge.Core; // Assuming ResourceType is in here

[System.Serializable]
public class InventoryItem
{
    public ResourceType itemType;
    public int amount;

    public InventoryItem(ResourceType type, int amt)
    {
        itemType = type;
        amount = amt;
    }
}