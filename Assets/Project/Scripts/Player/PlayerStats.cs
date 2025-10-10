using UnityEngine;
using System; // Required for Actions

namespace AutoForge.Player
{
    public class PlayerStats : MonoBehaviour
    {
        // Singleton pattern to make stats easily accessible from anywhere
        public static PlayerStats Instance { get; private set; }

        [Header("Base Stats")]
        [SerializeField] private float baseDamage = 25f;

        [Header("Live Modifiers")]
        private float damageBoost = 0f;

        // Public property that other scripts can read from
        public float TotalDamage => baseDamage + damageBoost;

        // Event that fires whenever stats change, so the UI can update
        public static event Action<PlayerStats> OnStatsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        public void AddDamageBoost(float amount)
        {
            damageBoost += amount;
            // Announce that stats have changed
            OnStatsChanged?.Invoke(this);
        }
    }
}
