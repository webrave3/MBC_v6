using UnityEngine;
using AutoForge.Player;

namespace AutoForge.Core
{
    public class Building : MonoBehaviour
    {
        // Public variable to hold its defining data
        public BuildingData data;

        private void Start()
        {
            // Check if data has been assigned
            if (data == null)
            {
                Debug.LogError("Building has no BuildingData assigned!", this);
                return;
            }

            // Apply its effect to the player
            if (PlayerStats.Instance != null)
            {
                // Read the damage bonus FROM THE DATA
                PlayerStats.Instance.AddDamageBoost(data.damageBonus);
            }
        }
    }
}