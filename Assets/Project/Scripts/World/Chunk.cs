// /Assets/Project/Scripts/World/Chunk.cs
using UnityEngine;

namespace AutoForge.World
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        public Vector2Int chunkCoord;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private Mesh _mesh;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();

            if (_meshCollider == null)
            {
                Debug.LogError($"<color=red><b>[Chunk ERROR]</b></color> {gameObject.name} is missing its MeshCollider component!", this);
            }

            _mesh = new Mesh();
            _mesh.name = "Chunk Mesh";
            _meshFilter.mesh = _mesh;
        }

        public void Load(Vector2Int coord, int chunkSize)
        {
            this.chunkCoord = coord;

            transform.position = new Vector3(
                coord.x * chunkSize,
                0,
                coord.y * chunkSize
            );

            gameObject.name = $"Chunk ({coord.x}, {coord.y})";
            GenerateTerrain();
        }

        private void GenerateTerrain()
        {
            WorldSettings settings = WorldManager.Instance.settings;
            if (settings == null)
            {
                Debug.LogError("<color=red><b>[Chunk ERROR]</b></color> WorldManager.Instance.settings is null!");
                return;
            }

            int size = settings.chunkResolution; // This is (vertices_per_side)
            int chunkSize = settings.chunkSize; // This is (world_units_per_chunk)
            int chunkHeight = settings.chunkHeight;

            // --- IMPORTANT: ChunkOffset for Perlin Noise ---
            // The Perlin noise map needs to sample from a continuous field.
            // If the noise map is 'size x size' vertices, then the number of 'steps' is (size - 1).
            // So, each chunk's sampling offset needs to be based on its coordinate
            // multiplied by the *number of steps* in a chunk.
            Vector2 chunkOffset = new Vector2(
                chunkCoord.x * (size - 1), // This represents the 'steps' or 'grid units' for noise
                chunkCoord.y * (size - 1)
            );

            // --- DEBUG 1: Verify chunk coordinates and offset calculation ---
            // Debug.Log($"<color=cyan>[Chunk Debug 1/5]</color> Loading {gameObject.name}. Coord: {chunkCoord}, Offset: {chunkOffset}");


            // --- 1. Generate Noise Maps ---
            if (settings.heightNoiseSettings == null || settings.temperatureNoiseSettings == null ||
                settings.humidityNoiseSettings == null || settings.buildZoneNoiseSettings == null)
            {
                Debug.LogError("<color=red><b>[Chunk ERROR]</b></color> One or more NoiseSettings are NULL in WorldSettings!");
                return;
            }
            float[,] heightMap = Noise.GenerateNoiseMap(size, size, settings.heightNoiseSettings, chunkOffset);
            float[,] tempMap = Noise.GenerateNoiseMap(size, size, settings.temperatureNoiseSettings, chunkOffset);
            float[,] humidityMap = Noise.GenerateNoiseMap(size, size, settings.humidityNoiseSettings, chunkOffset);
            float[,] buildZoneMask = Noise.GenerateNoiseMap(size, size, settings.buildZoneNoiseSettings, chunkOffset);


            // --- Error Checks for noise maps ---
            if (heightMap == null || tempMap == null || humidityMap == null || buildZoneMask == null)
            {
                Debug.LogError($"<color=red><b>[Chunk ERROR]</b></color> One or more noise maps returned null for {gameObject.name}!");
                return;
            }


            // --- DEBUG: Chunk Raw Height Debug (Optional, keep commented for less spam) ---
            // int centerX = size / 2;
            // int centerY = size / 2;
            // float centerRawHeight = -999f;
            // if (heightMap.GetLength(0) > centerX && centerX >= 0 && heightMap.GetLength(1) > centerY && centerY >=0) {
            //      centerRawHeight = heightMap[centerX, centerY];
            // } else {
            //      Debug.LogWarning($"<color=yellow>[Chunk Raw Height Debug]</color> Center coordinates ({centerX},{centerY}) out of bounds for heightMap size ({heightMap.GetLength(0)},{heightMap.GetLength(1)}) on {gameObject.name}.");
            // }
            // Debug.Log($"<color=#FFBF00>[Chunk Raw Height Debug]</color> {gameObject.name} center height (0-1 range): {centerRawHeight:F4}");


            // --- 2. Run "AUTO-FORGE" Constraint Pass (RE-ENABLED) ---
            TerrainFilter.ApplySlopeClamping(heightMap, chunkSize, chunkHeight, settings.maxNavigableSlope);
            TerrainFilter.ApplyAreaFlattening(heightMap, buildZoneMask, settings.buildZoneFlattenStrength);


            // --- 3. Determine Biomes & Splatmap (RE-ENABLED) ---
            if (settings.biomes == null || settings.biomes.Length == 0)
            {
                Debug.LogWarning("<color=yellow>[Chunk Warning]</color> No Biomes defined in WorldSettings! Using default material if available.");
            }

            Color[,] splatMap = new Color[size, size];
            BiomeSettings dominantBiome = settings.defaultBiome;
            if (dominantBiome == null && settings.biomes != null && settings.biomes.Length > 0)
            {
                dominantBiome = settings.biomes[0]; // Fallback to first biome if default is null
            }

            // Determine splatmap and dominant biome for rendering material
            if (settings.biomes != null && settings.biomes.Length > 0)
            {
                int centerX = size / 2;
                int centerY = size / 2;
                BiomeSettings currentCenterBiome = null;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float temp = tempMap[x, y];
                        float humidity = humidityMap[x, y];
                        BiomeSettings biome = settings.GetBiome(temp, humidity);
                        if (biome == null) biome = dominantBiome; // Use dominant/fallback if no biome matches

                        splatMap[x, y] = biome?.GetSplatColor() ?? Color.magenta; // Use magenta if biome or color is null

                        if (x == centerX && y == centerY)
                        {
                            currentCenterBiome = biome;
                        }
                    }
                }
                // Assign dominantBiome from the center of the chunk
                if (currentCenterBiome != null)
                {
                    dominantBiome = currentCenterBiome;
                }
            }


            // --- 4. Generate Mesh ---
            MeshData meshData = MeshGenerator.GenerateMesh(heightMap, splatMap, chunkSize, chunkHeight);

            // --- 5. Apply Data to Components ---
            MeshGenerator.ApplyToMesh(_mesh, _meshCollider, meshData);

            if (dominantBiome != null && dominantBiome.terrainMaterial != null)
            {
                _meshRenderer.material = dominantBiome.terrainMaterial;
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[Chunk Warning]</color> No dominant biome or valid material found for chunk {chunkCoord}. Using default grey material.");
                // Ensure a material is always assigned to prevent rendering issues
                _meshRenderer.material = new Material(Shader.Find("Standard")); // Fallback to Unity's default Standard Shader
            }
        }
    }
}