// /Assets/Project/Scripts/World/MeshGenerator.cs
using System.Collections.Generic;
using UnityEngine;

namespace AutoForge.World
{
    public struct MeshData
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uvs;
        public Color[] colors; // Used for splatmap weights
    }

    public static class MeshGenerator
    {
        // --- Flag for debugging specific chunks (can be enabled if needed later) ---
        private static bool _logVertexColors = false; // Set to true to enable detailed logging for specific vertices
        private static HashSet<Vector2Int> _loggedChunks = new HashSet<Vector2Int>(); // Keep track of logged chunks per run

        public static MeshData GenerateMeshWithSplatting(
            float[,] heightMap,
            float[,] tempMap,
            float[,] humidityMap,
            float[,] buildZoneMask,
            WorldSettings settings,
            Vector2Int chunkCoord)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            // Check if detailed logging is enabled for this specific chunk run
            bool logThisChunkDetailed = _logVertexColors && !_loggedChunks.Contains(chunkCoord);
            // if (logThisChunkDetailed)
            // {
            //     Debug.Log($"--- Generating Mesh Colors for Chunk {chunkCoord} ---"); // Keep this commented out unless specifically debugging mesh colors
            // }


            if (width <= 1 || height <= 1)
            {
                Debug.LogError($"[MeshGenerator] HeightMap dimensions ({width}x{height}) too small for Chunk {chunkCoord}! Cannot generate mesh.");
                return new MeshData { vertices = new Vector3[0], triangles = new int[0], uvs = new Vector2[0], colors = new Color[0] };
            }

            float vertexSpacing = (float)settings.chunkSize / (width - 1);

            MeshData meshData = new MeshData
            {
                vertices = new Vector3[width * height],
                triangles = new int[(width - 1) * (height - 1) * 6],
                uvs = new Vector2[width * height],
                colors = new Color[width * height]
            };

            int triangleIndex = 0;
            int vertexIndex = 0;

            // Define points to log if detailed logging is enabled
            int centerX = width / 2;
            int centerY = height / 2;
            int midX = width / 4;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // --- 1. VERTEX POSITION ---
                    float yPos = heightMap[x, y] * settings.chunkHeight;
                    float localX = x * vertexSpacing;
                    float localZ = y * vertexSpacing;
                    meshData.vertices[vertexIndex] = new Vector3(
                        localX - (settings.chunkSize / 2f), yPos, localZ - (settings.chunkSize / 2f));

                    // --- 2. UV CALCULATION ---
                    meshData.uvs[vertexIndex] = new Vector2(x / (float)(width - 1), y / (float)(height - 1));

                    // --- 3. CALCULATE BIOME WEIGHTS ---
                    float currentTemp = tempMap[x, y];
                    float currentHumidity = humidityMap[x, y];
                    Vector4 biomeWeights = Vector4.zero;
                    if (WorldManager.Instance != null)
                    {
                        biomeWeights = WorldManager.Instance.GetBiomeWeights(currentTemp, currentHumidity);
                    }
                    else if (logThisChunkDetailed && vertexIndex == 0) // Only log error once per chunk if needed
                    {
                        Debug.LogWarning($"[MeshGenerator Chunk {chunkCoord}] WorldManager.Instance is NULL when getting biome weights!");
                    }

                    // --- 4. CALCULATE SLOPE WEIGHT ---
                    float heightSelf = heightMap[x, y] * settings.chunkHeight;
                    float heightN = heightMap[x, Mathf.Clamp(y + 1, 0, height - 1)] * settings.chunkHeight;
                    float heightS = heightMap[x, Mathf.Clamp(y - 1, 0, height - 1)] * settings.chunkHeight;
                    float heightE = heightMap[Mathf.Clamp(x + 1, 0, width - 1), y] * settings.chunkHeight;
                    float heightW = heightMap[Mathf.Clamp(x - 1, 0, width - 1), y] * settings.chunkHeight;

                    float dX = (heightE - heightW) / (2f * vertexSpacing);
                    float dZ = (heightN - heightS) / (2f * vertexSpacing);
                    Vector3 normal = new Vector3(-dX, 1, -dZ).normalized;
                    float slopeSteepness = 1.0f - Mathf.Clamp01(normal.y);
                    float rockWeight = Mathf.InverseLerp(settings.slopeBlendStart, settings.slopeBlendEnd, slopeSteepness);
                    rockWeight = Mathf.Clamp01(rockWeight);

                    // --- 5. NORMALIZE WEIGHTS AND ASSIGN COLOR ---
                    float r = biomeWeights.x;
                    float g = biomeWeights.y;
                    float b = biomeWeights.z;

                    float biomeSum = r + g + b;
                    if (biomeSum > 0.001f)
                    {
                        float rebalance = (1.0f - rockWeight) / biomeSum;
                        r *= rebalance;
                        g *= rebalance;
                        b *= rebalance;
                    }
                    else if (settings?.biomes != null && settings.biomes.Length > 0 && settings.biomes[0] != null)
                    {
                        r = 1.0f - rockWeight;
                        g = 0; b = 0;
                    }
                    else
                    {
                        r = 1.0f - rockWeight; g = 0; b = 0;
                    }

                    // --- DETAILED DEBUG LOGGING (if enabled) ---
                    if (logThisChunkDetailed)
                    {
                        bool logThisVertex = false;
                        string pointLabel = "";

                        if (x == 0 && y == 0) { logThisVertex = true; pointLabel = "Corner(0,0)"; }
                        else if (x == centerX && y == centerY) { logThisVertex = true; pointLabel = "Center"; }
                        else if (x == midX && y == 0) { logThisVertex = true; pointLabel = "Mid-Edge"; }

                        if (logThisVertex)
                        {
                            Color finalColor = new Color(r, g, b, rockWeight);
                            Debug.Log($"  Vertex {pointLabel}({x},{y}) Chunk({chunkCoord}):\n" + // Added chunk coord here
                                     $"    Height={yPos:F2}, Temp={currentTemp:F2}, Hum={currentHumidity:F2}\n" +
                                     $"    Neighbours (N,S,E,W): {heightN:F2}, {heightS:F2}, {heightE:F2}, {heightW:F2}\n" +
                                     $"    dX={dX:F3}, dZ={dZ:F3}, Normal={normal.ToString("F3")}, Steepness={slopeSteepness:F3}\n" +
                                     $"    Slope Blend Range=({settings.slopeBlendStart:F2} - {settings.slopeBlendEnd:F2})\n" +
                                     $"    BiomeWeights Raw={biomeWeights.ToString("F2")}\n" +
                                     $"    RockWeight={rockWeight:F3}\n" +
                                     $"    BiomeSum={biomeSum:F3}, Rebalance={(biomeSum > 0.001f ? ((1.0f - rockWeight) / biomeSum) : 0):F3}\n" +
                                     $"    Final Color=({finalColor.r:F2}, {finalColor.g:F2}, {finalColor.b:F2}, {finalColor.a:F2})");
                        }
                    }
                    // --- END DETAILED DEBUG LOGGING ---

                    meshData.colors[vertexIndex] = new Color(r, g, b, rockWeight);

                    // --- 6. TRIANGLE GENERATION ---
                    if (x < width - 1 && y < height - 1)
                    {
                        int topLeft = vertexIndex;
                        int topRight = vertexIndex + 1;
                        int bottomLeft = vertexIndex + width;
                        int bottomRight = vertexIndex + width + 1;

                        meshData.triangles[triangleIndex + 0] = topLeft;
                        meshData.triangles[triangleIndex + 1] = bottomLeft;
                        meshData.triangles[triangleIndex + 2] = topRight;
                        meshData.triangles[triangleIndex + 3] = topRight;
                        meshData.triangles[triangleIndex + 4] = bottomLeft;
                        meshData.triangles[triangleIndex + 5] = bottomRight;
                        triangleIndex += 6;
                    }
                    vertexIndex++;
                }
            }

            // Mark chunk as logged for this run if detailed logging was enabled
            if (logThisChunkDetailed)
            {
                _loggedChunks.Add(chunkCoord);
                // Debug.Log($"--- Finished Logging Mesh Colors for Chunk {chunkCoord} ---"); // Keep commented out
            }

            return meshData;
        }

        // --- Method to clear logged chunks (call from WorldManager.Awake/Start) ---
        public static void ClearLoggedChunks()
        {
            _loggedChunks.Clear();
            // Debug.Log("[MeshGenerator] Cleared logged chunk list."); // Keep commented out unless needed
        }

        public static void ApplyToMesh(Mesh mesh, MeshCollider meshCollider, MeshData meshData)
        {
            if (meshData.vertices == null || meshData.vertices.Length == 0 ||
              meshData.triangles == null || meshData.triangles.Length == 0)
            {
                Debug.LogWarning($"[MeshGenerator Warning] Attempted to apply mesh with zero vertices or triangles to {meshCollider?.gameObject.name ?? "Unknown Object"}. Skipping mesh update.");
                return;
            }

            mesh.Clear();
            mesh.vertices = meshData.vertices;
            mesh.triangles = meshData.triangles;
            mesh.uv = meshData.uvs;
            mesh.colors = meshData.colors;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            if (meshCollider == null)
            {
                Debug.LogError($"[MeshGenerator ERROR] MeshCollider is NULL on GameObject using mesh '{mesh.name}'. Cannot apply physics mesh.");
                return;
            }

            // Update MeshCollider safely
            meshCollider.enabled = false;
            meshCollider.sharedMesh = null; // Clear previous mesh first
            meshCollider.sharedMesh = mesh; // Assign the new one
            meshCollider.enabled = true;
        }
    }
}