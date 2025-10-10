using UnityEngine;
using TMPro;
using AutoForge.Player;
using AutoForge.Core; // <-- ADD THIS

namespace AutoForge.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Stats UI")]
        [SerializeField] private TextMeshProUGUI damageText;
        [SerializeField] private TextMeshProUGUI scrapText; // <-- ADD THIS

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
            // --- NEW CODE ---
            // Initial UI update
            UpdateResourcesUI();
            // --- END NEW CODE ---
        }

        private void OnEnable()
        {
            PlayerStats.OnStatsChanged += UpdateStatsUI;
            // --- NEW CODE ---
            ResourceManager.OnResourcesChanged += UpdateResourcesUI;
            // --- END NEW CODE ---
        }

        private void OnDisable()
        {
            PlayerStats.OnStatsChanged -= UpdateStatsUI;
            // --- NEW CODE ---
            ResourceManager.OnResourcesChanged -= UpdateResourcesUI;
            // --- END NEW CODE ---
        }

        private void UpdateStatsUI(PlayerStats stats)
        {
            if (stats != null)
            {
                damageText.text = $"DAMAGE: {stats.TotalDamage}";
            }
        }

        // --- NEW CODE ---
        private void UpdateResourcesUI()
        {
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