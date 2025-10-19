// /Assets/Project/Scripts/World/Chunk.cs
using UnityEngine;

namespace AutoForge.World
{
    // Ensure the prefab has these components
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        public Vector2Int chunkCoord;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
        }

        // Called by WorldManager when this chunk is activated
        public void Load(Vector2Int coord, int chunkSize)
        {
            this.chunkCoord = coord;

            // Position the chunk in the world
            transform.position = new Vector3(
                coord.x * chunkSize,
                0,
                coord.y * chunkSize
            );

            gameObject.name = $"Chunk ({coord.x}, {coord.y})";

            // --- THIS IS WHERE WE WILL GENERATE DATA AND MESH ---
            // We'll add this in the next steps.
            GenerateTerrain();
        }

        private void GenerateTerrain()
        {
            // Placeholder for now.
            // In the next steps, this method will:
            // 1. Get NoiseSettings (from WorldManager or Biome)
            // 2. Call Noise.GenerateNoiseMap() for height
            // 3. Call Noise.GenerateNoiseMap() for temp/humidity
            // 4. Run the "AUTO-FORGE" Constraint Pass on the height data
            // 5. Call a new MeshGenerator.GenerateMesh() with the final data
            // 6. Apply the mesh to _meshFilter and _meshCollider
            // 7. Apply a biome texture to _meshRenderer

            // For now, let's just make a simple plane to prove it works.
            // You can remove this simple plane logic later.
            GenerateSimplePlane(WorldManager.Instance.settings.chunkSize);
        }

        // A temporary method to create a flat plane.
        // We will replace this in Component 1.4
        private void GenerateSimplePlane(int size)
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(0, 0, 0),
                new Vector3(size, 0, 0),
                new Vector3(0, 0, size),
                new Vector3(size, 0, size)
            };
            mesh.vertices = vertices;

            int[] tris = new int[6]
            {
                0, 2, 1,
                2, 3, 1
            };
            mesh.triangles = tris;

            Vector2[] uvs = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            mesh.uv = uvs;

            mesh.RecalculateNormals();

            _meshFilter.mesh = mesh;
            _meshCollider.sharedMesh = mesh;
        }
    }
}