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

            

            // Create Mesh instance once in Awake and reuse it
            _mesh = new Mesh();
            _mesh.name = "Chunk Mesh";
            _meshFilter.mesh = _mesh; // Assign reusable mesh instance
        }

        public void Load(Vector2Int coord, int chunkSize, Material worldMaterial)
        {
            this.chunkCoord = coord;

            transform.position = new Vector3(
                coord.x * chunkSize,
                0,
                coord.y * chunkSize
            );

            gameObject.name = $"Chunk ({coord.x}, {coord.y})";

            // Assign the single splatmap material provided by WorldManager
            if (worldMaterial != null)
            {
                _meshRenderer.material = worldMaterial;
            }
            else
            {
               
                _meshRenderer.material = new Material(Shader.Find("Standard"));
            }

            GenerateTerrain();
        }

        private void GenerateTerrain()
        {
            // Use null-conditional operator ?. for safety
            WorldSettings settings = WorldManager.Instance?.settings;
            if (settings == null)
            {
                
                return;
            }

            int resolution = settings.chunkResolution; // Vertices per side
            int chunkSize = settings.chunkSize;     // World units per chunk edge
            int chunkHeight = settings.chunkHeight;   // Max height scale factor

            // Calculate noise offset based on chunk coordinates and resolution steps
            Vector2 chunkOffset = new Vector2(
                chunkCoord.x * (resolution - 1),
                chunkCoord.y * (resolution - 1)
            );

            // --- 1. Generate Noise Maps ---
            if (settings.heightNoiseSettings == null || settings.temperatureNoiseSettings == null ||
                settings.humidityNoiseSettings == null || settings.buildZoneNoiseSettings == null)
            {
                
                return;
            }
            float[,] heightMap = Noise.GenerateNoiseMap(resolution, resolution, settings.heightNoiseSettings, chunkOffset);
            float[,] tempMap = Noise.GenerateNoiseMap(resolution, resolution, settings.temperatureNoiseSettings, chunkOffset);
            float[,] humidityMap = Noise.GenerateNoiseMap(resolution, resolution, settings.humidityNoiseSettings, chunkOffset);
            float[,] buildZoneMask = Noise.GenerateNoiseMap(resolution, resolution, settings.buildZoneNoiseSettings, chunkOffset);

            if (heightMap == null || tempMap == null || humidityMap == null || buildZoneMask == null)
            {
                
                return;
            }

            // --- 2. Run "AUTO-FORGE" Constraint Pass ---
            // These modify the heightMap BEFORE mesh generation
            TerrainFilter.ApplySlopeClamping(heightMap, chunkSize, chunkHeight, settings.maxNavigableSlope);
            TerrainFilter.ApplyAreaFlattening(heightMap, buildZoneMask, settings.buildZoneFlattenStrength);
            // Add other filters like PathCarving here if implemented


            // --- 3. Determine Dominant Biome (If needed for other logic) ---
            // This part is less important for the visual splatmap but might be useful for gameplay.
            BiomeSettings dominantBiome = settings.defaultBiome;
            if (settings.biomes != null && settings.biomes.Length > 0)
            {
                int centerX = resolution / 2;
                int centerY = resolution / 2;
                if (centerX >= 0 && centerX < resolution && centerY >= 0 && centerY < resolution)
                {
                    float centerTemp = tempMap[centerX, centerY];
                    float centerHumidity = humidityMap[centerX, centerY];
                    BiomeSettings centerBiome = settings.GetBiome(centerTemp, centerHumidity);
                    if (centerBiome != null)
                    {
                        dominantBiome = centerBiome;
                    }
                }
                else
                {
                    
                    dominantBiome = settings.defaultBiome ?? (settings.biomes.Length > 0 ? settings.biomes[0] : null);
                }
            }
            if (dominantBiome == null)
            {
                
            }


            // --- 4. Generate Mesh Data (With Vertex Color Splatting) ---
            // Pass chunkCoord here for logging context inside the generator
            MeshData meshData = MeshGenerator.GenerateMeshWithSplatting(
                heightMap,
                tempMap,
                humidityMap,
                buildZoneMask,
                settings,
                chunkCoord // <-- Pass the chunk coordinate
            );

            // --- 5. Apply Data to Components ---
            // ApplyToMesh handles null checks internally
            MeshGenerator.ApplyToMesh(_mesh, _meshCollider, meshData);
        }
    }
}