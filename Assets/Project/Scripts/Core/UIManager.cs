using UnityEngine;
using TMPro; // Required for TextMeshPro
using AutoForge.Player;

namespace AutoForge.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Stats UI")]
        [SerializeField] private TextMeshProUGUI damageText;

        [Header("Debug Panel")]
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private TextMeshProUGUI playerPositionText;
        [SerializeField] private TextMeshProUGUI enemyCountText;
        [SerializeField] private TextMeshProUGUI buildingCountText;

        private Transform playerTransform;

        private void Start()
        {
            // Find player for debug panel
            if (PlayerStats.Instance != null)
            {
                playerTransform = PlayerStats.Instance.transform;
            }
        }

        // Subscribe to the event when this object is enabled
        private void OnEnable()
        {
            PlayerStats.OnStatsChanged += UpdateStatsUI;
        }

        // Unsubscribe when disabled
        private void OnDisable()
        {
            PlayerStats.OnStatsChanged -= UpdateStatsUI;
        }

        // This function is called by the event whenever stats change
        private void UpdateStatsUI(PlayerStats stats)
        {
            if (stats != null)
            {
                damageText.text = $"DAMAGE: {stats.TotalDamage}";
            }
        }

        private void Update()
        {
            // Constantly update the debug panel
            if (debugPanel.activeInHierarchy && playerTransform != null)
            {
                playerPositionText.text = $"POS: {playerTransform.position.ToString("F1")}";
                // These would need more logic to track, for now they are placeholders
                // enemyCountText.text = $"ENEMIES: {FindObjectsOfType<Enemy>().Length}"; 
                // buildingCountText.text = $"BUILDINGS: {FindObjectsOfType<Building>().Length}";
            }
        }
    }
}
