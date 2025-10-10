using UnityEngine;
using AutoForge.Enemies;

namespace AutoForge.Core
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Bullet : MonoBehaviour
    {
        [Header("Settings")]
        public float speed = 75f;
        public float damage = 25f;
        public float knockbackForce = 200f; // The force applied to the enemy

        private Rigidbody rb;
        private Collider col;
        private MeshRenderer meshRenderer;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            meshRenderer = GetComponent<MeshRenderer>();

            rb.linearVelocity = transform.forward * speed;

            Destroy(gameObject, 3.5f);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.TryGetComponent<Enemy>(out Enemy enemyComponent))
            {
                // We now pass the hit direction and force to the enemy
                enemyComponent.TakeDamage(damage, transform.forward, knockbackForce);
            }

            // --- LINGER EFFECT LOGIC ---
            rb.linearVelocity = Vector3.zero;
            col.enabled = false;
            if (meshRenderer != null)
                meshRenderer.enabled = false;

            Destroy(gameObject, 0.5f);
        }
    }
}

