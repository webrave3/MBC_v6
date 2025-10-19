// /Assets/Project/Scripts/Enemies/EnemySpawner.cs
using UnityEngine;
using AutoForge.World; // <-- ADD THIS NAMESPACE

namespace AutoForge.Core
{
    /// <summary>
    /// A simple script that spawns a given prefab at a regular interval.
    /// Waits for the initial world to generate before starting.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawner Settings")]
        public GameObject enemyPrefab; // The enemy to spawn
        public float spawnInterval = 5f; // Time between spawns

        // --- ADD THIS ---
        [Tooltip("Offset from the terrain surface to prevent spawning underground.")]
        public float spawnHeightOffset = 0.1f;
        // --- END ADD ---

        void Start()
        {
            // Check if the prefab is assigned to prevent errors.
            if (enemyPrefab != null)
            {
                // --- REMOVE THIS LINE ---
                // InvokeRepeating(nameof(SpawnEnemy), 2f, spawnInterval);

                // --- ADD THIS LOGIC ---
                // Wait for the world to be generated before starting to spawn
                if (WorldManager.Instance != null)
                {
                    WorldManager.Instance.OnInitialWorldGenerated += StartSpawning;
                }
                else
                {
                    Debug.LogError("<color=red>[EnemySpawner ERROR]</color> WorldManager.Instance is null! Cannot subscribe to world generation event.", this);
                }
                // --- END ADD ---
            }
            else
            {
                Debug.LogError("Enemy Prefab not assigned to the spawner!", this);
            }
        }

        // --- ADD THIS METHOD ---
        private void OnDestroy()
        {
            // Ensure we unsubscribe if this object is destroyed
            if (WorldManager.Instance != null)
            {
                WorldManager.Instance.OnInitialWorldGenerated -= StartSpawning;
            }
        }

        // --- ADD THIS METHOD ---
        private void StartSpawning()
        {
            // Unsubscribe immediately
            if (WorldManager.Instance != null)
            {
                WorldManager.Instance.OnInitialWorldGenerated -= StartSpawning;
            }

            // Now that the world is generated, we can safely start spawning
            InvokeRepeating(nameof(SpawnEnemy), 2f, spawnInterval);
        }
        // --- END ADD ---

        void SpawnEnemy()
        {
            // --- ADD THIS LOGIC ---
            // Get a safe spawn position at this spawner's (x, z) location
            if (WorldManager.Instance == null)
            {
                Debug.LogError("<color=red>[EnemySpawner ERROR]</color> WorldManager.Instance is null! Cannot get safe spawn position.", this);
                return;
            }

            Vector3 safeSpawnPos = WorldManager.Instance.GetSafeSpawnPosition(transform.position);
            safeSpawnPos.y += spawnHeightOffset; // Apply offset
            // --- END ADD ---

            // --- MODIFY THIS LINE ---
            // Create a new instance of the enemy prefab at the corrected safe position
            Instantiate(enemyPrefab, safeSpawnPos, transform.rotation);
        }
    }
}