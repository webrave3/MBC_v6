using UnityEngine;

namespace AutoForge.Core
{
    /// <summary>
    /// A simple script that spawns a given prefab at a regular interval.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawner Settings")]
        public GameObject enemyPrefab; // The enemy to spawn
        public float spawnInterval = 5f; // Time between spawns

        void Start()
        {
            // Check if the prefab is assigned to prevent errors.
            if (enemyPrefab != null)
            {
                // Call the SpawnEnemy function every 'spawnInterval' seconds, starting after 2 seconds.
                InvokeRepeating(nameof(SpawnEnemy), 2f, spawnInterval);
            }
            else
            {
                Debug.LogError("Enemy Prefab not assigned to the spawner!", this);
            }
        }

        void SpawnEnemy()
        {
            // Create a new instance of the enemy prefab at the spawner's position and rotation.
            Instantiate(enemyPrefab, transform.position, transform.rotation);
        }
    }
}
