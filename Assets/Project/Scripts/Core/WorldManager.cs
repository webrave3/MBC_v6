// /Assets/Project/Scripts/World/WorldManager.cs
using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation; // <-- 1. ADD THIS NAMESPACE

namespace AutoForge.World
{
    public class WorldManager : MonoBehaviour
    {
        public static WorldManager Instance;

        [Header("Configuration")]
        public WorldSettings settings;
        public Transform player; // Drag your player's transform here
        public NavMeshSurface navMeshSurface; // <-- 2. ADD THIS FIELD. Drag your 'Navigation' object here.

        [Header("Runtime State")]
        private Vector2Int _currentPlayerChunkCoord;
        private Dictionary<Vector2Int, Chunk> _activeChunks = new Dictionary<Vector2Int, Chunk>();
        private Queue<Chunk> _chunkPool = new Queue<Chunk>();

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

            // Initial load
            UpdateChunks();
            UpdateNavMesh(); // <-- 3. ADD THIS CALL for the initial bake
        }

        private void Update()
        {
            if (player == null) return;

            Vector2Int playerChunkCoord = GetChunkCoordFromPosition(player.position);

            if (playerChunkCoord != _currentPlayerChunkCoord)
            {
                _currentPlayerChunkCoord = playerChunkCoord;
                UpdateChunks();
                UpdateNavMesh(); // <-- 4. ADD THIS CALL to update NavMesh after chunks change
            }
        }

        public Vector2Int GetChunkCoordFromPosition(Vector3 position)
        {
            int x = Mathf.FloorToInt(position.x / settings.chunkSize);
            int z = Mathf.FloorToInt(position.z / settings.chunkSize);
            return new Vector2Int(x, z);
        }

        private void UpdateChunks()
        {
            // --- 1. Unload/Pool Chunks ---
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
                Chunk chunk = _activeChunks[coord];
                chunk.gameObject.SetActive(false);
                _chunkPool.Enqueue(chunk);
                _activeChunks.Remove(coord);
            }

            // --- 2. Load/Activate New Chunks ---
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
            }

            newChunk.Load(coord, settings.chunkSize);
            _activeChunks.Add(coord, newChunk);
        }

        // --- 5. ADD THIS ENTIRE METHOD ---
        /// <summary>
        /// Rebuilds the NavMesh for all active surfaces.
        /// This is called after chunks are updated.
        /// </summary>
        private void UpdateNavMesh()
        {
            if (navMeshSurface == null)
            {
                // We already warned in Start(), no need to spam logs
                return;
            }

            // This rebuilds the navigation data for all GameObjects
            // collected by the NavMeshSurface (based on its layer mask).
            navMeshSurface.BuildNavMesh();
        }
    }
}