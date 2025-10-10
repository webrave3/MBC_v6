using UnityEngine;
using AutoForge.Core;
using TMPro; // Make sure this namespace is included

namespace AutoForge.Enemies
{
    [RequireComponent(typeof(Rigidbody))]
    public class Enemy : MonoBehaviour
    {
        [Header("Data")]
        public EnemyData enemyData;

        [Header("Loot")]
        [SerializeField] private ResourceType lootDrop;
        [SerializeField] private int lootAmount = 1;

        [Header("UI")]
        [SerializeField] private GameObject healthTextPrefab;
        [SerializeField] private GameObject damageTextPrefab; // <-- THIS LINE WAS MISSING
        [SerializeField] private Vector3 healthTextOffset = new Vector3(0, 2.5f, 0);
        private TextMeshProUGUI healthText;
        private Transform mainCameraTransform;

        private float currentHealth;
        private Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Start()
        {
            if (enemyData != null)
            {
                currentHealth = enemyData.maxHealth;
            }
            else
            {
                Debug.LogError("EnemyData is not assigned on this enemy!", this);
            }

            mainCameraTransform = Camera.main.transform;
            if (healthTextPrefab != null)
            {
                GameObject healthUI = Instantiate(healthTextPrefab, transform.position + healthTextOffset, Quaternion.identity, transform);
                healthText = healthUI.GetComponentInChildren<TextMeshProUGUI>();
                UpdateHealthUI();
            }
        }

        private void LateUpdate()
        {
            if (healthText != null)
            {
                // This line makes the health bar face the camera
                healthText.transform.parent.LookAt(mainCameraTransform);
            }
        }

        // The method signature is correct, but the body was missing the spawn logic.
        public void TakeDamage(float amount, Vector3 hitDirection, float force, Vector3 hitPoint)
        {
            float roundedAmount = Mathf.Round(amount);
            currentHealth -= roundedAmount;

            UpdateHealthUI();

            // --- THIS IS THE LOGIC THAT SPAWNS THE DAMAGE TEXT ---
            if (damageTextPrefab != null)
            {
                // Create the damage text UI at the exact point of impact
                GameObject dmgTextInstance = Instantiate(damageTextPrefab, hitPoint, Quaternion.identity);

                // Set the text to the damage amount
                if (dmgTextInstance.GetComponentInChildren<TextMeshProUGUI>() is TextMeshProUGUI tmp)
                {
                    tmp.text = roundedAmount.ToString();
                }

                // Make the UI face the camera instantly
                dmgTextInstance.transform.LookAt(mainCameraTransform);

                // Destroy the damage text after 1 second
                Destroy(dmgTextInstance, 1f);
            }
            // --- END OF DAMAGE TEXT LOGIC ---

            if (rb != null)
            {
                rb.AddForce(hitDirection * force, ForceMode.Impulse);
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void UpdateHealthUI()
        {
            if (healthText != null)
            {
                healthText.text = currentHealth.ToString("F0");
            }
        }

        private void Die()
        {
            if (lootDrop != null && ResourceManager.Instance != null)
            {
                ResourceManager.Instance.AddResource(lootDrop, lootAmount);
            }
            EventManager.RaiseEnemyDied(transform.position);
            Destroy(gameObject);
        }
    }
}