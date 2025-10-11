using UnityEngine;
using AutoForge.Core;
using TMPro; // Required for TextMeshPro UI elements

namespace AutoForge.Enemies
{
    [RequireComponent(typeof(Rigidbody))]
    public class Enemy : MonoBehaviour
    {
        [Header("Data")]
        public EnemyData enemyData;

        [Header("Loot")]
        [SerializeField] private GameObject resourcePickupPrefab; // The physical object to drop
        [SerializeField] private ResourceType lootDropType;
        [SerializeField] private int lootAmount = 10;

        [Header("UI")]
        [SerializeField] private GameObject healthTextPrefab;
        [SerializeField] private GameObject damageTextPrefab;
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

            // Cache the main camera's transform for performance
            mainCameraTransform = Camera.main.transform;

            // Instantiate and set up the persistent health bar UI
            if (healthTextPrefab != null)
            {
                GameObject healthUI = Instantiate(healthTextPrefab, transform.position + healthTextOffset, Quaternion.identity, transform);
                healthText = healthUI.GetComponentInChildren<TextMeshProUGUI>();
                UpdateHealthUI();
            }
        }

        // LateUpdate is called after all Update functions. Best for camera-facing UI.
        private void LateUpdate()
        {
            // Make the health bar always face the camera
            if (healthText != null)
            {
                healthText.transform.parent.LookAt(mainCameraTransform);
            }
        }

        public void TakeDamage(float amount, Vector3 hitDirection, float force, Vector3 hitPoint)
        {
            float roundedAmount = Mathf.Round(amount);
            currentHealth -= roundedAmount;

            UpdateHealthUI();

            // Spawn the floating damage text at the point of impact
            if (damageTextPrefab != null)
            {
                GameObject dmgTextInstance = Instantiate(damageTextPrefab, hitPoint, Quaternion.identity);

                if (dmgTextInstance.GetComponentInChildren<TextMeshProUGUI>() is TextMeshProUGUI tmp)
                {
                    tmp.text = roundedAmount.ToString();
                }

                // Make the damage text face the camera and destroy it after a second
                dmgTextInstance.transform.LookAt(mainCameraTransform);
                Destroy(dmgTextInstance, 1f);
            }

            // Apply knockback force
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
                healthText.text = currentHealth.ToString("F0"); // "F0" formats as a whole number
            }
        }

        private void Die()
        {
            // If we have a resource prefab to drop, spawn it
            if (resourcePickupPrefab != null)
            {
                GameObject pickupObject = Instantiate(resourcePickupPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

                // Configure the pickup with the correct resource type and amount
                if (pickupObject.TryGetComponent<Items.ResourcePickup>(out var resourcePickup))
                {
                    resourcePickup.resourceType = lootDropType;
                    resourcePickup.amount = lootAmount;
                }

                // Add a small physical pop to the dropped item
                if (pickupObject.TryGetComponent<Rigidbody>(out var pickupRb))
                {
                    Vector3 randomForce = new Vector3(Random.Range(-1f, 1f), 1.5f, Random.Range(-1f, 1f));
                    pickupRb.AddForce(randomForce.normalized * 3f, ForceMode.Impulse);
                }
            }

            // Raise the event for other systems that might care about enemy deaths
            EventManager.RaiseEnemyDied(transform.position);

            // Destroy the enemy GameObject
            Destroy(gameObject);
        }
    }
}