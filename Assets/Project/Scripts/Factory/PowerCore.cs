using UnityEngine;
using System;

// This namespace can be your project's main factory or core systems namespace
namespace AutoForge.Factory
{
    public class PowerCore : MonoBehaviour
    {
        // Singleton pattern to make the core easily accessible from anywhere
        public static PowerCore Instance { get; private set; }

        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 1000f;
        public float CurrentHealth { get; private set; }

        // Event for UI to listen to. Sends (currentHealth, maxHealth)
        public static event Action<float, float> OnHealthChanged;

        private void Awake()
        {
            // Enforce Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }

            CurrentHealth = maxHealth;
        }

        private void Start()
        {
            // Announce initial health state for UI
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void TakeDamage(float amount)
        {
            if (CurrentHealth <= 0) return; // Don't take damage if already dead

            CurrentHealth -= amount;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

            // Announce the health change to any listeners (like a UI)
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log("Power Core Destroyed! GAME OVER.");
            // --- TODO ---
            // 1. Instantiate a large explosion particle effect at transform.position.
            // 2. Call a GameManager function to trigger the game over sequence.
            // Example: GameManager.Instance.EndRun(false);

            // For now, we'll just disable the core's visuals
            // In the future, you might want to destroy the entire factory object.
            GetComponent<Renderer>().enabled = false;
        }
    }
}
