// /Assets/Project/Scripts/Core/WorldManager.cs
using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;
using System.Collections; // <-- 1. ADD THIS NAMESPACE

namespace AutoForge.World
{
    public class WorldManager : MonoBehaviour
    {
        public event System.Action OnInitialWorldGenerated;

        public static WorldManager Instance;

        [Header("Configuration")]
        public WorldSettings settings;
        public Transform player;
        public NavMeshSurface navMeshSurface;

        [Header("Spawning")]
        [Tooltip("Set this to the LayerMask that your Chunk prefabs are on. Used for safe spawning raycasts.")]
        public LayerMask terrainLayer;

        [Header("Runtime State")]
        private Vector2Int _currentPlayerChunkCoord = Vector2Int.one * int.MinValue;
        private Dictionary<Vector2Int, Chunk> _activeChunks = new Dictionary<Vector2Int, Chunk>();
        private Queue<Chunk> _chunkPool = new Queue<Chunk>();

        private bool _isFirstLoad = true;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // 2. CHANGE 'void Start()' TO 'IEnumerator Start()'
        private IEnumerator Start()
        {
            if (player == null)
            {
                Debug.LogError("Player transform is not set in WorldManager!");
                yield break; // Use yield break in a coroutine
            }
            if (navMeshSurface == null)
            {
                Debug.LogWarning("NavMeshSurface is not set in WorldManager. AI will not update.");
            }
            if (terrainLayer == 0)
            {
                Debug.LogWarning($"<color=yellow>[WorldManager Warning]</color> 'Terrain Layer' is not set in the inspector. " +
                                 "GetSafeSpawnPosition will not work correctly. Please assign the layer your Chunks are on.", this);
            }

            _currentPlayerChunkCoord = GetChunkCoordFromPosition(player.position);
            Debug.Log($"<color=yellow>[WorldManager Start]</color> Initial player chunk coord: {_currentPlayerChunkCoord}");

            UpdateChunks();
            UpdateNavMesh();

            if (_isFirstLoad)
            {
                // 3. ADD THIS LINE
                // This waits for the physics engine to update and recognize the new MeshColliders
                yield return new WaitForFixedUpdate();

                OnInitialWorldGenerated?.Invoke();
                _isFirstLoad = false;
                Debug.Log("<color=green>[WorldManager Start]</color> Initial world generation complete. Fired event.");
            }
        }

        private void Update()
        {
            if (player == null) return;

            Vector2Int playerChunkCoord = GetChunkCoordFromPosition(player.position);

            if (playerChunkCoord != _currentPlayerChunkCoord)
            {
                Debug.Log($"<color=orange>[WorldManager Update]</color> Player moved to new chunk! " +
                          $"Old: {_currentPlayerChunkCoord}, New: {playerChunkCoord}. Triggering UpdateChunks.");

                _currentPlayerChunkCoord = playerChunkCoord;
                UpdateChunks();
                UpdateNavMesh();
            }
        }

        public Vector2Int GetChunkCoordFromPosition(Vector3 position)
        {
            if (settings == null || settings.chunkSize <= 0)
            {
                Debug.LogError("<color=red>[WorldManager ERROR]</color> WorldSettings not assigned or chunkSize is invalid!");
                return Vector2Int.zero;
            }

            int x = Mathf.FloorToInt(position.x / settings.chunkSize);
            int z = Mathf.FloorToInt(position.z / settings.chunkSize);
            return new Vector2Int(x, z);
        }

        private void UpdateChunks()
        {
            List<Vector2Int> chunksToUnload = new List<Vector2Int>();
            foreach (var chunkPair in _activeChunks)
            {
                Vector2Int coord = chunkPair.Key;
                int xDist = Mathf.Abs(coord.x - _currentPlayerChunkCoord.x);
                int yDist = Mathf.Abs(coord.y - _currentPlayerChunkCoord.y);

                if (xDist > settings.viewDistance || yDist > settings.viewDistance)
                {
                    chunksToUnload.Add(coord);
                }
            }

            foreach (Vector2Int coord in chunksToUnload)
            {
                if (_activeChunks.TryGetValue(coord, out Chunk chunk))
                {
                    chunk.gameObject.SetActive(false);
                    _chunkPool.Enqueue(chunk);
                    _activeChunks.Remove(coord);
                }
            }

            for (int x = -settings.viewDistance; x <= settings.viewDistance; x++)
            {
                for (int z = -settings.viewDistance; z <= settings.viewDistance; z++)
                {
                    Vector2Int coord = new Vector2Int(
                        _currentPlayerChunkCoord.x + x,
                        _currentPlayerChunkCoord.y + z
                    );

                    if (!_activeChunks.ContainsKey(coord))
                    {
                        LoadChunk(coord);
                    }
                }
            }
        }

        private void LoadChunk(Vector2Int coord)
        {
            if (settings == null || settings.chunkPrefab == null)
            {
                Debug.LogError("<color=red>[WorldManager ERROR]</color> WorldSettings not assigned or chunkPrefab is NULL!");
                return;
            }

            Chunk newChunk;
            if (_chunkPool.Count > 0)
            {
                newChunk = _chunkPool.Dequeue();
                newChunk.gameObject.SetActive(true);
            }
            else
            {
                GameObject chunkObject = Instantiate(
                    settings.chunkPrefab,
                    Vector3.zero,
                    Quaternion.identity,
                    transform
                );
                newChunk = chunkObject.GetComponent<Chunk>();
                if (newChunk == null)
                {
                    Debug.LogError($"<color=red><b>[WorldManager ERROR]</b></color> Chunk prefab is missing the Chunk script!", chunkObject);
                    Destroy(chunkObject);
                    return;
                }
            }

            if (newChunk != null)
            {
                newChunk.Load(coord, settings.chunkSize);
                _activeChunks.Add(coord, newChunk);
            }
        }

        private void UpdateNavMesh()
        {
            if (navMeshSurface == null) return;
            navMeshSurface.BuildNavMesh();
        }

        public Vector3 GetSafeSpawnPosition(Vector3 desiredPosition, float raycastMaxHeight = 1000f)
        {
            Vector3 rayStart = new Vector3(desiredPosition.x, raycastMaxHeight, desiredPosition.z);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastMaxHeight * 2f, terrainLayer))
            {
                return hit.point;
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[WorldManager]</color> GetSafeSpawnPosition raycast missed terrain at ({desiredPosition.x}, {desiredPosition.z}). " +
                                 "Ensure the 'Terrain Layer' is set in WorldManager and that Chunks are on this layer. " +
                                 "Falling back to original Y value.", this);
                return desiredPosition;
            }
        }
    }
}