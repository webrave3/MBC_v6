using UnityEngine;

namespace AutoForge.Core
{
    [CreateAssetMenu(fileName = "New BuildingData", menuName = "AutoForge/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("Info")]
        public string buildingName;
        public GameObject buildingPrefab;

        [Header("Building Cost")]
        public ResourceType costType;
        public int costAmount;

        [Header("Stats")]
        public float damageBonus;
        // We can add many more stats here later, like:
        // public float health;
        // public float powerConsumption;
        // public float fireRate;
    }
}