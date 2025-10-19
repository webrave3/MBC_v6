// /Assets/Project/Scripts/Core/WorldManager.cs
using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation; // Keep for NavMesh
using System.Collections;   // Keep for Coroutine

namespace AutoForge.World
{
    public class WorldManager : MonoBehaviour
    {
        public event System.Action OnInitialWorldGenerated;

        public static WorldManager Instance;

        [Header("Configuration")]
        public WorldSettings settings;
        public Transform player;
        public NavMeshSurface navMeshSurface; // Keep if using Unity AI Navigation
        public Material worldMaterial; // Material using your BiomeSplatmapShader.shadergraph

        [Header("Spawning")]
        [Tooltip("Set this to the LayerMask that your Chunk prefabs are on.")]
        public LayerMask terrainLayer;

        [Header("Runtime State")]
        private Vector2Int _currentPlayerChunkCoord = Vector2Int.one * int.MinValue;
        private Dictionary<Vector2Int, Chunk> _activeChunks = new Dictionary<Vector2Int, Chunk>();
        private Queue<Chunk> _chunkPool = new Queue<Chunk>();
        private bool _isFirstLoad = true;
        private bool _navMeshNeedsUpdate = false;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("[WorldManager] Duplicate instance detected. Destroying self.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (settings == null) Debug.LogError("[WorldManager] WorldSettings not assigned!");
            if (worldMaterial == null) Debug.LogError("[WorldManager] WorldMaterial not assigned! Chunks will likely be invisible or pink.");
        }

        private IEnumerator Start()
        {
            if (player == null)
            {
                Debug.LogError("[WorldManager] Player transform is not set!");
                yield break;
            }
            if (navMeshSurface == null)
            {
                Debug.LogWarning("[WorldManager] NavMeshSurface is not set. AI pathfinding will not update.");
            }
            if (terrainLayer == 0)
            {
                Debug.LogWarning("[WorldManager] 'Terrain Layer' is not set. Raycasts (like GetSafeSpawnPosition) might fail.");
            }

            _currentPlayerChunkCoord = GetChunkCoordFromPosition(player.position);
            UpdateChunks(); // Initial load

            yield return null; // Wait one frame for chunks to start their generation

            UpdateNavMesh(); // Build initial NavMesh

            if (_isFirstLoad)
            {
                yield return new WaitForFixedUpdate(); // Wait for physics
                OnInitialWorldGenerated?.Invoke();
                _isFirstLoad = false;
                Debug.Log("[WorldManager] Initial world generation complete.");
            }
        }

        private void Update()
        {
            if (player == null) return;

            Vector2Int playerChunkCoord = GetChunkCoordFromPosition(player.position);

            if (playerChunkCoord != _currentPlayerChunkCoord)
            {
                _currentPlayerChunkCoord = playerChunkCoord;
                UpdateChunks();
                _navMeshNeedsUpdate = true; // Flag for NavMesh update
            }

            if (_navMeshNeedsUpdate)
            {
                UpdateNavMesh();
                _navMeshNeedsUpdate = false;
            }
        }

        public Vector2Int GetChunkCoordFromPosition(Vector3 position)
        {
            if (settings == null || settings.chunkSize <= 0)
            {
                Debug.LogError("[WorldManager] WorldSettings not assigned or chunkSize is invalid!");
                return Vector2Int.zero;
            }

            int x = Mathf.FloorToInt(position.x / settings.chunkSize);
            int z = Mathf.FloorToInt(position.z / settings.chunkSize);
            return new Vector2Int(x, z);
        }

        private void UpdateChunks()
        {
            if (settings == null) return;

            List<Vector2Int> currentlyActiveCoords = new List<Vector2Int>(_activeChunks.Keys);
            HashSet<Vector2Int> requiredCoords = new HashSet<Vector2Int>();
            int viewDist = settings.viewDistance;

            // Determine required chunks
            for (int x = -viewDist; x <= viewDist; x++)
            {
                for (int z = -viewDist; z <= viewDist; z++)
                {
                    Vector2Int coord = new Vector2Int(
                        _currentPlayerChunkCoord.x + x,
                        _currentPlayerChunkCoord.y + z
                    );
                    requiredCoords.Add(coord);
                }
            }

            // Unload out-of-range chunks
            foreach (Vector2Int coord in currentlyActiveCoords)
            {
                if (!requiredCoords.Contains(coord))
                {
                    if (_activeChunks.TryGetValue(coord, out Chunk chunk))
                    {
                        chunk.gameObject.SetActive(false);
                        _chunkPool.Enqueue(chunk);
                        _activeChunks.Remove(coord);
                    }
                }
            }

            // Load new/activate required chunks
            foreach (Vector2Int coord in requiredCoords)
            {
                if (!_activeChunks.ContainsKey(coord))
                {
                    LoadChunk(coord); // Load new or activate from pool
                }
            }
            // Debug.Log($"Active Chunks: {_activeChunks.Count}, Pooled Chunks: {_chunkPool.Count}");
        }

        private void LoadChunk(Vector2Int coord)
        {
            if (settings == null || settings.chunkPrefab == null || worldMaterial == null)
            {
                Debug.LogError("[WorldManager] Cannot load chunk - WorldSettings, chunkPrefab, or worldMaterial is NULL!");
                return;
            }

            Chunk newChunk;
            if (_chunkPool.Count > 0)
            {
                newChunk = _chunkPool.Dequeue();
                newChunk.gameObject.SetActive(true);
                // Position and material are set inside Load
            }
            else
            {
                GameObject chunkObject = Instantiate(
                    settings.chunkPrefab,
                    Vector3.zero, // Position is set in Chunk.Load
                    Quaternion.identity,
                    transform // Parent to WorldManager
                );
                newChunk = chunkObject.GetComponent<Chunk>();
                if (newChunk == null)
                {
                    Debug.LogError("[WorldManager] Chunk prefab is missing the Chunk script!", chunkObject);
                    Destroy(chunkObject);
                    return;
                }
            }

            if (newChunk != null)
            {
                // --- CORRECTED Method Call (passing worldMaterial) ---
                newChunk.Load(coord, settings.chunkSize, worldMaterial);
                _activeChunks.Add(coord, newChunk);
            }
        }

        private void UpdateNavMesh()
        {
            if (navMeshSurface == null) return;
            navMeshSurface.BuildNavMesh();
            //Debug.Log("[WorldManager] NavMesh updated.");
        }

        public Vector4 GetBiomeWeights(float temp, float humidity)
        {
            // (Keep the GetBiomeWeights method exactly as provided in the previous answer)
            if (settings?.biomes == null || settings.biomes.Length == 0)
            {
                return new Vector4(1, 0, 0, 0); // Failsafe: Weight to first texture
            }

            int numBiomes = Mathf.Min(settings.biomes.Length, 3); // Max 3 biomes affect RGB
            float[] influences = new float[numBiomes];
            float totalInfluence = 0;
            Vector2 point = new Vector2(temp, humidity);

            for (int i = 0; i < numBiomes; i++)
            {
                BiomeSettings biome = settings.biomes[i];
                if (biome == null) continue;

                // Ensure biomeGraphPosition exists in BiomeSettings.cs
                float dist = Vector2.Distance(point, biome.biomeGraphPosition);
                float influence = 1.0f / (dist * dist + 0.0001f); // Inverse distance squared

                influences[i] = influence;
                totalInfluence += influence;
            }

            float r = 0, g = 0, b = 0;

            if (totalInfluence > 0.001f)
            {
                if (numBiomes > 0) r = influences[0] / totalInfluence;
                if (numBiomes > 1) g = influences[1] / totalInfluence;
                if (numBiomes > 2) b = influences[2] / totalInfluence;
            }
            else if (numBiomes > 0) // Failsafe
            {
                r = 1;
            }

            // Alpha (w) is calculated by slope in MeshGenerator, return 0 here.
            return new Vector4(r, g, b, 0);
        }


        public Vector3 GetSafeSpawnPosition(Vector3 desiredPosition, float raycastMaxHeight = 1000f)
        {
            // (Keep the GetSafeSpawnPosition method exactly as provided in the previous answer)
            Vector3 rayStart = new Vector3(desiredPosition.x, raycastMaxHeight, desiredPosition.z);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastMaxHeight * 2f, terrainLayer))
            {
                return hit.point + Vector3.up * 0.1f; // Add small offset upwards
            }
            else
            {
                Debug.LogWarning($"[WorldManager] GetSafeSpawnPosition raycast missed terrain at ({desiredPosition.x}, {desiredPosition.z}). Check Terrain Layer setup.");
                return desiredPosition; // Fallback
            }
        }
    }
}