using UnityEngine;
using TMPro;
using AutoForge.Player;
using AutoForge.Core;

namespace AutoForge.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Stats UI")]
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI scrapText;

        // Note: The selectedBuildingText from our previous step is removed here for clarity.
        // We will re-add it when we build the hotbar.

        [Header("Debug Panel")]
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private TextMeshProUGUI playerPositionText;
        [SerializeField] private TextMeshProUGUI enemyCountText;
        [SerializeField] private TextMeshProUGUI buildingCountText;

        [Header("Resource Tracking")]
        [SerializeField] private ResourceType scrapResourceType;

        private Transform playerTransform;

        private void Start()
        {
            if (PlayerStats.Instance != null)
            {
                playerTransform = PlayerStats.Instance.transform;
            }
            UpdateResourcesUI();
        }

        private void OnEnable()
        {
            PlayerStats.OnStatsChanged += UpdateStatsUI;
            // --- FIX ---
            // Subscribe to the event on PlayerInventory, not ResourceManager
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged += UpdateResourcesUI;
            }
        }

        private void OnDisable()
        {
            PlayerStats.OnStatsChanged -= UpdateStatsUI;
            // --- FIX ---
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnInventoryChanged -= UpdateResourcesUI;
            }
        }

        private void UpdateStatsUI(PlayerStats stats)
        {
            if (stats != null)
            {
                damageText.text = $"DAMAGE: {stats.TotalDamage}";
            }
        }

        private void UpdateResourcesUI()
        {
            // --- FIX ---
            // This now correctly calls the pass-through method on ResourceManager
            if (ResourceManager.Instance != null && scrapText != null && scrapResourceType != null)
            {
                int scrapAmount = ResourceManager.Instance.GetResourceAmount(scrapResourceType);
                scrapText.text = $"SCRAP: {scrapAmount}";
            }
        }

        private void Update()
        {
            if (debugPanel.activeInHierarchy && playerTransform != null)
            {
                playerPositionText.text = $"POS: {playerTransform.position.ToString("F1")}";
            }
        }
    }
}
