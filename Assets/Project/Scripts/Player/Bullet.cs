using UnityEngine;
using AutoForge.Enemies;
using AutoForge.Player;

namespace AutoForge.Core
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Bullet : MonoBehaviour
    {
        [Header("Settings")]
        public float speed = 75f;
        public float baseDamage = 10f;
        public float knockbackForce = 5000f; // Increased for more impact!

        [Header("VFX")]
        public GameObject hitEffectPrefab; // Drag your particle prefab here in the Inspector

        private Rigidbody rb;
        private Collider col;
        private MeshRenderer meshRenderer;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            meshRenderer = GetComponent<MeshRenderer>();
            rb.linearVelocity = transform.forward * speed;
            Destroy(gameObject, 3.0f);
        }

        void OnCollisionEnter(Collision collision)
        {
            // Spawn the hit effect at the exact point of collision
            if (hitEffectPrefab != null)
            {
                ContactPoint contact = collision.GetContact(0);
                Instantiate(hitEffectPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            }

            if (collision.gameObject.TryGetComponent<Enemy>(out Enemy enemyComponent))
            {
                float totalDamage = PlayerStats.Instance != null ? PlayerStats.Instance.TotalDamage : baseDamage;
                Vector3 knockbackDirection = (collision.transform.position - transform.position).normalized;
                Vector3 hitPoint = collision.GetContact(0).point;
                enemyComponent.TakeDamage(totalDamage, knockbackDirection, knockbackForce, hitPoint);
            }

            // Linger/disappear effect
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
            col.enabled = false;
            if (meshRenderer != null)
                meshRenderer.enabled = false;

            Destroy(gameObject, 0.25f);
        }
    }
}
