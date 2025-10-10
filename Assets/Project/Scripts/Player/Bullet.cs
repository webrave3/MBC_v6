using UnityEngine;
using AutoForge.Enemies; // Required to find the Enemy script
using AutoForge.Player; // Required to get total damage from PlayerStats

namespace AutoForge.Core // Assuming this is the correct namespace from your file
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Bullet : MonoBehaviour
    {
        [Header("Settings")]
        public float speed = 75f;
        public float baseDamage = 10f; // Renamed to clarify it's a fallback/base value
        public float knockbackForce = 200f;

        private Rigidbody rb;
        private Collider col;
        private MeshRenderer meshRenderer;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            meshRenderer = GetComponent<MeshRenderer>();

            // Set initial velocity
            rb.linearVelocity = transform.forward * speed;

            // Destroy after a set lifetime regardless of collision
            Destroy(gameObject, 3.5f);
        }

        void OnCollisionEnter(Collision collision)
        {
            // Check if the object we hit has an Enemy component
            if (collision.gameObject.TryGetComponent<Enemy>(out Enemy enemyComponent))
            {
                // --- MODIFIED DAMAGE LOGIC ---
                // Get the total damage from the PlayerStats singleton. Use baseDamage if it's not found.
                float totalDamage = PlayerStats.Instance != null ? PlayerStats.Instance.TotalDamage : baseDamage;

                // Calculate a more accurate direction for knockback (from collision point away from bullet)
                Vector3 knockbackDirection = (collision.transform.position - transform.position).normalized;

                // Get the precise point of impact from the collision data
                Vector3 hitPoint = collision.GetContact(0).point;

                // Call TakeDamage on the enemy, now with the hitPoint included
                enemyComponent.TakeDamage(totalDamage, knockbackDirection, knockbackForce, hitPoint);
                // --- END MODIFIED LOGIC ---
            }

            // --- LINGER EFFECT LOGIC (from your script) ---
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true; // Stop physics interactions completely
            col.enabled = false;
            if (meshRenderer != null)
                meshRenderer.enabled = false;

            // Destroy the (now invisible) bullet object after a short delay
            Destroy(gameObject, 0.5f);
        }
    }
}