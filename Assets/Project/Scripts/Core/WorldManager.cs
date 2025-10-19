// /Assets/Project/Scripts/Core/WorldManager.cs
using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;

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

        [Header("Runtime State")]
        private Vector2Int _currentPlayerChunkCoord = Vector2Int.one * int.MinValue; // Initialize to force first update
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

        private void Start()
        {
            if (player == null)
            {
                Debug.LogError("Player transform is not set in WorldManager!");
                return;
            }
            if (navMeshSurface == null)
            {
                Debug.LogWarning("NavMeshSurface is not set in WorldManager. AI will not update.");
            }

            // Force initial load calculation correctly
            _currentPlayerChunkCoord = GetChunkCoordFromPosition(player.position);
            Debug.Log($"<color=yellow>[WorldManager Start]</color> Initial player chunk coord: {_currentPlayerChunkCoord}");
            UpdateChunks();
            UpdateNavMesh();

            if (_isFirstLoad)
            {
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
                // --- THIS IS THE ONLY LOG WE CARE ABOUT RIGHT NOW ---
                Debug.Log($"<color=orange>[WorldManager Update]</color> Player moved to new chunk! " +
                          $"Old: {_currentPlayerChunkCoord}, New: {playerChunkCoord}. Triggering UpdateChunks.");

                _currentPlayerChunkCoord = playerChunkCoord;
                UpdateChunks();
                UpdateNavMesh();
            }
        }

        public Vector2Int GetChunkCoordFromPosition(Vector3 position)
        {
            // --- Added check for valid settings ---
            if (settings == null || settings.chunkSize <= 0)
            {
                Debug.LogError("<color=red>[WorldManager ERROR]</color> WorldSettings not assigned or chunkSize is invalid!");
                return Vector2Int.zero;
            }
            // --- End Add ---

            int x = Mathf.FloorToInt(position.x / settings.chunkSize);
            int z = Mathf.FloorToInt(position.z / settings.chunkSize);
            return new Vector2Int(x, z);
        }

        private void UpdateChunks()
        {
            // Debug.Log($"<color=gray>[WorldManager UpdateChunks]</color> Running unload/load logic for center coord: {_currentPlayerChunkCoord}"); // Keep this commented unless needed

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
                    // Debug.Log($"<color=gray>[WorldManager UpdateChunks]</color> Deactivating chunk {coord}"); // Keep commented
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
                        // Removed the log from here as LoadChunk will be called anyway
                        LoadChunk(coord);
                    }
                }
            }
        }

        private void LoadChunk(Vector2Int coord)
        {
            // --- Added safety check ---
            if (settings == null || settings.chunkPrefab == null)
            {
                Debug.LogError("<color=red>[WorldManager ERROR]</color> WorldSettings not assigned or chunkPrefab is NULL!");
                return;
            }
            // --- End Add ---


            Chunk newChunk;
            if (_chunkPool.Count > 0)
            {
                newChunk = _chunkPool.Dequeue();
                newChunk.gameObject.SetActive(true);
                // Debug.Log($"<color=lightblue>[WorldManager LoadChunk]</color> Reusing chunk for coord: {coord}"); // Keep commented
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
                    Debug.LogError($"<color=red><b>[WorldManager ERROR]</b></color> Chunk prefab is missing the Chunk script!", chunkObject);
                    Destroy(chunkObject); // Prevent further errors
                    return;
                }
                // Debug.Log($"<color=lightblue>[WorldManager LoadChunk]</color> Instantiated new chunk for coord: {coord}"); // Keep commented
            }

            // --- Added safety check ---
            if (newChunk != null)
            {
                newChunk.Load(coord, settings.chunkSize);
                _activeChunks.Add(coord, newChunk);
            }
            // --- End Add ---
        }

        private void UpdateNavMesh()
        {
            if (navMeshSurface == null) return;
            navMeshSurface.BuildNavMesh();
            // Debug.Log($"<color=gray>[WorldManager UpdateNavMesh]</color> Rebuilt NavMesh."); // Keep commented
        }
    }
}