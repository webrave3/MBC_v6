using UnityEngine;
using UnityEngine.UI;
using TMPro; // Required for TextMeshPro elements
using AutoForge.Factory;
using AutoForge.Player;
using AutoForge.Core; // Required for ResourceType

namespace AutoForge.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("Health & Shield Displays")]
        [SerializeField] private Slider powerCoreHealthSlider;
        [SerializeField] private TextMeshProUGUI powerCoreHealthText;
        [SerializeField] private Slider playerShieldSlider;
        [SerializeField] private TextMeshProUGUI playerShieldText;

        [Header("Player & Resource Stats")]
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI scrapText;
        [SerializeField] private ResourceType scrapResourceType;

        [Header("Debug Panel")]
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private TextMeshProUGUI playerPositionText;
        [SerializeField] private TextMeshProUGUI enemyCountText;
        [SerializeField] private TextMeshProUGUI buildingCountText;

        private Transform playerTransform;

        private void OnEnable()
        {
            // Subscribe to all relevant events
            PowerCore.OnHealthChanged += UpdatePowerCoreHealth;
            PlayerStats.OnShieldChanged += UpdatePlayerShield;
            PlayerStats.OnStatsChanged += UpdateStatsUI;

            // It's good practice to null-check singletons before subscribing
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged += UpdateResourcesUI;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from all events to prevent memory leaks
            PowerCore.OnHealthChanged -= UpdatePowerCoreHealth;
            PlayerStats.OnShieldChanged -= UpdatePlayerShield;
            PlayerStats.OnStatsChanged -= UpdateStatsUI;

            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= UpdateResourcesUI;
            }
        }

        private void Start()
        {
            // Find the player's transform for the debug panel
            if (PlayerStats.Instance != null)
            {
                playerTransform = PlayerStats.Instance.transform;
            }

            // Set initial UI states
            if (powerCoreHealthSlider != null)
                powerCoreHealthSlider.gameObject.SetActive(PowerCore.Instance != null);
            if (playerShieldSlider != null)
                playerShieldSlider.gameObject.SetActive(PlayerStats.Instance != null);

            // Manually call update methods once at the start to populate UI
            UpdateResourcesUI();
            if (PlayerStats.Instance != null)
            {
                UpdateStatsUI(PlayerStats.Instance);
                // Also update the shield UI on start using the public property
                UpdatePlayerShield(PlayerStats.Instance.CurrentShield, 100f); // Assuming maxShield is 100 for initialization
            }
        }

        private void Update()
        {
            // Handle debug panel updates
            if (debugPanel != null && debugPanel.activeInHierarchy && playerTransform != null)
            {
                playerPositionText.text = $"POS: {playerTransform.position.ToString("F1")}";
            }
        }

        private void UpdatePowerCoreHealth(float currentHealth, float maxHealth)
        {
            if (powerCoreHealthSlider != null)
            {
                if (!powerCoreHealthSlider.gameObject.activeInHierarchy)
                    powerCoreHealthSlider.gameObject.SetActive(true);

                // Update slider and text
                powerCoreHealthSlider.value = currentHealth / maxHealth;
                if (powerCoreHealthText != null)
                    powerCoreHealthText.text = $"{Mathf.Ceil(currentHealth)} / {maxHealth}";
            }
        }

        private void UpdatePlayerShield(float currentShield, float maxShield)
        {
            if (playerShieldSlider != null)
            {
                if (!playerShieldSlider.gameObject.activeInHierarchy)
                    playerShieldSlider.gameObject.SetActive(true);

                // Update slider and text
                playerShieldSlider.value = currentShield / maxShield;
                if (playerShieldText != null)
                    playerShieldText.text = $"{Mathf.Ceil(currentShield)} / {maxShield}";
            }
        }

        private void UpdateStatsUI(PlayerStats stats)
        {
            if (stats != null && damageText != null)
            {
                damageText.text = $"DAMAGE: {stats.TotalDamage}";
            }
        }

        private void UpdateResourcesUI()
        {
            if (ResourceManager.Instance != null && scrapText != null && scrapResourceType != null)
            {
                int scrapAmount = ResourceManager.Instance.GetResourceAmount(scrapResourceType);
                scrapText.text = $"SCRAP: {scrapAmount}";
            }
        }
    }
}

