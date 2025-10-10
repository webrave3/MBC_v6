using UnityEngine;
using AutoForge.Core;

namespace AutoForge.Enemies
{
    [RequireComponent(typeof(Rigidbody))] // Ensure this enemy has a Rigidbody
    public class Enemy : MonoBehaviour
    {
        [Header("Data")]
        public EnemyData enemyData;

        private float currentHealth;
        private Rigidbody rb; // Reference to the Rigidbody

        void Awake()
        {
            // Get the Rigidbody component so we can apply forces to it
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
        }

        // The function now accepts a direction and force for the knockback
        public void TakeDamage(float amount, Vector3 hitDirection, float force)
        {
            currentHealth -= amount;

            // Apply the knockback force
            if (rb != null)
            {
                // Use ForceMode.Impulse for a single, instant burst of force
                rb.AddForce(hitDirection * force, ForceMode.Impulse);
            }

            if (currentHealth <= 0f)
            {
                Die();
            }
        }

        private void Die()
        {
            EventManager.RaiseEnemyDied(transform.position);
            Destroy(gameObject);
        }
    }
}

