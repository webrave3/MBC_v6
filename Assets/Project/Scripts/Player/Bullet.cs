using UnityEngine;
using AutoForge.Enemies;
using AutoForge.Player;
using TMPro; // Required for TextMeshPro

namespace AutoForge.Core
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Bullet : MonoBehaviour
    {
        [Header("Settings")]
        public float speed = 25f;
        public float baseDamage = 10f;
        public float knockbackForce = 200f;

        // --- ADDED THIS SECTION ---
        [Header("VFX")]
        [Tooltip("The particle effect to spawn when hitting an ENEMY.")]
        public GameObject hitEffectPrefab;
        [Tooltip("The floating damage number UI prefab to spawn when hitting an ENEMY.")]
        public GameObject damageTextPrefab;
        // --- END ADDED SECTION ---

        private Rigidbody rb;
        private Collider col;
        private MeshRenderer meshRenderer;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            meshRenderer = GetComponent<MeshRenderer>();

            rb.linearVelocity = transform.forward * speed;

            // Destroy after a set lifetime regardless of collision
            Destroy(gameObject, 3.0f);
        }

        void OnCollisionEnter(Collision collision)
        {
            // Immediately stop the bullet's movement and physics
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;

            // Check if the object we hit has an Enemy component
            if (collision.gameObject.TryGetComponent<Enemy>(out Enemy enemyComponent))
            {
                float totalDamage = PlayerStats.Instance != null ? PlayerStats.Instance.TotalDamage : baseDamage;
                Vector3 knockbackDirection = (collision.transform.position - transform.position).normalized;
                Vector3 hitPoint = collision.GetContact(0).point;

                // --- ADDED THIS LOGIC ---
                // Spawn the visual effects at the point of impact
                SpawnHitEffects(hitPoint, Quaternion.LookRotation(collision.GetContact(0).normal), totalDamage);
                // --- END ADDED LOGIC ---

                // We now call the full TakeDamage method from the Enemy script
                enemyComponent.TakeDamage(totalDamage, knockbackDirection, knockbackForce, hitPoint);
            }

            // --- IMPROVED DESTRUCTION LOGIC ---
            // Visually disable the bullet and its collision
            if (meshRenderer != null)
                meshRenderer.enabled = false;
            col.enabled = false;

            // Destroy the (now invisible) bullet object after a short delay to ensure all logic finishes
            Destroy(gameObject, 0.1f);
        }

        // --- ADDED THIS ENTIRE FUNCTION ---
        private void SpawnHitEffects(Vector3 position, Quaternion rotation, float damageAmount)
        {
            // Spawn the particle effect
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, position, rotation);
            }

            // Spawn the floating damage text
            if (damageTextPrefab != null)
            {
                GameObject dmgTextInstance = Instantiate(damageTextPrefab, position, Quaternion.identity);

                // Set the text and make it face the camera
                if (dmgTextInstance.GetComponentInChildren<TextMeshProUGUI>() is TextMeshProUGUI tmp)
                {
                    tmp.text = Mathf.Round(damageAmount).ToString();
                    if (Camera.main != null)
                    {
                        tmp.transform.parent.LookAt(Camera.main.transform);
                    }
                }
            }
        }
        // --- END ADDED FUNCTION ---
    }
}

