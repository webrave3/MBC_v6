// /Assets/Project/Scripts/Enemies/EnemySpawner.cs
using UnityEngine;
using AutoForge.World;
using System.Collections.Generic; // <-- ADDED THIS NAMESPACE

namespace AutoForge.Core
{
    /// <summary>
    /// A simple script that spawns a random prefab from a list at a regular interval.
    /// Waits for the initial world to generate before starting.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawner Settings")]
        // --- MODIFIED THIS ---
        [Tooltip("A list of enemy prefabs this spawner can choose from.")]
        public List<GameObject> enemyPrefabs; // The enemies to spawn
        // --- END MODIFY ---

        public float spawnInterval = 5f; // Time between spawns

        [Tooltip("Offset from the terrain surface to prevent spawning underground.")]
        public float spawnHeightOffset = 0.1f;

        void Start()
        {
            // Check if the prefab list is assigned and has enemies.
            // --- MODIFIED THIS ---
            if (enemyPrefabs != null && enemyPrefabs.Count > 0)
            // --- END MODIFY ---
            {
                // Wait for the world to be generated before starting to spawn
                if (WorldManager.Instance != null)
                {
                    WorldManager.Instance.OnInitialWorldGenerated += StartSpawning;
                }
                else
                {
                    Debug.LogError("<color=red>[EnemySpawner ERROR]</color> WorldManager.Instance is null! Cannot subscribe to world generation event.", this);
                }
            }
            else
            {
                // --- MODIFIED THIS ---
                Debug.LogError("Enemy Prefabs list not assigned or is empty!", this);
                // --- END MODIFY ---
            }
        }

        private void OnDestroy()
        {
            // Ensure we unsubscribe if this object is destroyed
            if (WorldManager.Instance != null)
            {
                WorldManager.Instance.OnInitialWorldGenerated -= StartSpawning;
            }
        }

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

        void SpawnEnemy()
        {
            // Get a safe spawn position at this spawner's (x, z) location
            if (WorldManager.Instance == null)
            {
                Debug.LogError("<color=red>[EnemySpawner ERROR]</color> WorldManager.Instance is null! Cannot get safe spawn position.", this);
                return;
            }

            Vector3 safeSpawnPos = WorldManager.Instance.GetSafeSpawnPosition(transform.position);
            safeSpawnPos.y += spawnHeightOffset; // Apply offset

            // --- MODIFIED THIS LOGIC ---

            // Failsafe check in case the list is empty
            if (enemyPrefabs == null || enemyPrefabs.Count == 0)
            {
                return;
            }

            // 1. Pick a random enemy prefab from the list
            GameObject prefabToSpawn = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];

            // 2. Create a new instance of the chosen prefab at the corrected safe position
            Instantiate(prefabToSpawn, safeSpawnPos, transform.rotation);
            // --- END MODIFY ---
        }
    }
}