// /Assets/Project/Scripts/Enemies/Enemy.cs
using UnityEngine;
using AutoForge.Core;
using TMPro; // Required for TextMeshPro UI elements
using System.Collections; // <-- ADD THIS

namespace AutoForge.Enemies
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(EnemyMovement))] // Ensure the movement script is always present
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

        // --- ADD THIS SECTION ---
        [Header("Feedback")]
        [Tooltip("The main Renderer for the enemy's body to flash white.")]
        [SerializeField] private Renderer enemyRenderer;
        [Tooltip("How long the hit flash should last.")]
        [SerializeField] private float hitFlashDuration = 0.1f;

        private MaterialPropertyBlock _propBlock;
        private Coroutine _flashCoroutine;
        // --- END ADD ---

        private TextMeshProUGUI healthText;
        private Transform mainCameraTransform;
        private float currentHealth;
        private Rigidbody rb;
        private EnemyMovement _enemyMovement; // Reference to the movement script

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            _enemyMovement = GetComponent<EnemyMovement>(); // Get the movement script

            // --- ADD THIS SECTION ---
            _propBlock = new MaterialPropertyBlock();

            // As a fallback, try to find the renderer in children if not assigned
            if (enemyRenderer == null)
            {
                enemyRenderer = GetComponentInChildren<Renderer>();
                if (enemyRenderer == null)
                {
                    Debug.LogError("No Renderer found on enemy or in children. Hit flash will not work.", this);
                }
            }
            // --- END ADD ---
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
                healthText.transform.parent.LookAt(mainCameraTransform);
            }
        }

        public void TakeDamage(float amount, Vector3 hitDirection, float force, Vector3 hitPoint)
        {
            float roundedAmount = Mathf.Round(amount);
            currentHealth -= roundedAmount;

            UpdateHealthUI();

            // --- THIS IS THE CRUCIAL LINK ---
            // Tell the movement AI that it has been attacked by the player.
            if (_enemyMovement != null)
            {
                _enemyMovement.OnTookDamageFromPlayer();
            }
            // --- END CRUCIAL LINK ---

            if (damageTextPrefab != null)
            {
                GameObject dmgTextInstance = Instantiate(damageTextPrefab, hitPoint, Quaternion.identity);
                if (dmgTextInstance.GetComponentInChildren<TextMeshProUGUI>() is TextMeshProUGUI tmp)
                {
                    tmp.text = roundedAmount.ToString();
                }
                dmgTextInstance.transform.LookAt(mainCameraTransform);
                Destroy(dmgTextInstance, 1f);
            }

            if (rb != null)
            {
                rb.AddForce(hitDirection * force, ForceMode.Impulse);
            }

            // --- ADD THIS SECTION ---
            // Trigger the hit flash feedback
            if (enemyRenderer != null)
            {
                // Stop any previous flash coroutine to reset the timer
                if (_flashCoroutine != null)
                {
                    StopCoroutine(_flashCoroutine);
                }
                _flashCoroutine = StartCoroutine(HitFlashCoroutine());
            }
            // --- END ADD ---

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        // --- ADD THIS METHOD ---
        /// <summary>
        /// Coroutine that applies a white color override, waits, and then clears it.
        /// </summary>
        private IEnumerator HitFlashCoroutine()
        {
            // Apply the override
            enemyRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", Color.white); // _BaseColor is the URP Lit/Unlit color property
            enemyRenderer.SetPropertyBlock(_propBlock);

            // Wait for the flash duration
            yield return new WaitForSeconds(hitFlashDuration);

            // Clear the override, returning to the material's original color
            enemyRenderer.SetPropertyBlock(null);

            _flashCoroutine = null; // Mark as finished
        }
        // --- END ADD ---

        private void UpdateHealthUI()
        {
            if (healthText != null)
            {
                healthText.text = currentHealth.ToString("F0");
            }
        }

        private void Die()
        {
            if (resourcePickupPrefab != null)
            {
                GameObject pickupObject = Instantiate(resourcePickupPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                if (pickupObject.TryGetComponent<Gameplay.ResourcePickup>(out var resourcePickup))
                {
                    resourcePickup.resourceType = lootDropType;
                    resourcePickup.amount = lootAmount;
                }
                if (pickupObject.TryGetComponent<Rigidbody>(out var pickupRb))
                {
                    Vector3 randomForce = new Vector3(Random.Range(-1f, 1f), 1.5f, Random.Range(-1f, 1f));
                    pickupRb.AddForce(randomForce.normalized * 3f, ForceMode.Impulse);
                }
            }

            // This assumes you have an EventManager. If not, you can comment this line out.
            // EventManager.RaiseEnemyDied(transform.position);

            Destroy(gameObject);
        }
    }
}