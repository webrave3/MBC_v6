// /Assets/Project/Scripts/World/TerrainFilter.cs
using UnityEngine;

namespace AutoForge.World
{
    public static class TerrainFilter
    {
        /// <summary>
        /// Iterates the heightmap and flattens slopes that are too steep.
        /// This is a simple but effective implementation.
        /// </summary>
        public static void ApplySlopeClamping(float[,] heightMap, int chunkSize, int chunkHeight, float maxSlopeAngle)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            // Calculate max allowed height difference between adjacent vertices
            float maxRise = Mathf.Tan(maxSlopeAngle * Mathf.Deg2Rad);

            // Note: This assumes vertices are 1 world unit apart in the mesh.
            // If chunkSize = 100 and width = 101, then each 'step' is 1 unit.
            float maxDelta = maxRise * (chunkSize / (float)(width - 1));

            // We iterate multiple times to 'relax' the terrain
            for (int i = 0; i < 3; i++)
            {
                // Horizontal pass (X-axis)
                for (int y = 0; y < height; y++)
                {
                    for (int x = 1; x < width; x++)
                    {
                        float delta = heightMap[x, y] - heightMap[x - 1, y];
                        if (Mathf.Abs(delta) > maxDelta)
                        {
                            float avg = (heightMap[x, y] + heightMap[x - 1, y]) / 2f;
                            heightMap[x, y] = avg + (maxDelta * Mathf.Sign(delta) / 2f);
                            heightMap[x - 1, y] = avg - (maxDelta * Mathf.Sign(delta) / 2f);
                        }
                    }
                }

                // Vertical pass (Z-axis)
                for (int x = 0; x < width; x++)
                {
                    for (int y = 1; y < height; y++)
                    {
                        float delta = heightMap[x, y] - heightMap[x, y - 1];
                        if (Mathf.Abs(delta) > maxDelta)
                        {
                            float avg = (heightMap[x, y] + heightMap[x, y - 1]) / 2f;
                            heightMap[x, y] = avg + (maxDelta * Mathf.Sign(delta) / 2f);
                            heightMap[x, y - 1] = avg - (maxDelta * Mathf.Sign(delta) / 2f);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Flattens areas based on a build zone mask.
        /// </summary>
        public static void ApplyAreaFlattening(float[,] heightMap, float[,] buildZoneMask, float strength)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            float[,] smoothedMap = new float[width, height];

            // Simple box blur kernel
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    float avg = 0;
                    avg += heightMap[x - 1, y - 1];
                    avg += heightMap[x, y - 1];
                    avg += heightMap[x + 1, y - 1];
                    avg += heightMap[x - 1, y];
                    avg += heightMap[x, y];
                    avg += heightMap[x + 1, y];
                    avg += heightMap[x - 1, y + 1];
                    avg += heightMap[x, y + 1];
                    avg += heightMap[x + 1, y + 1];
                    smoothedMap[x, y] = avg / 9f;
                }
            }

            // Lerp between original height and smoothed height based on mask
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Use a strong blend, controlled by the mask value and master strength
                    float blendFactor = buildZoneMask[x, y] * strength;

                    // Avoid edges where blur is incomplete
                    if (x > 0 && x < width - 1 && y > 0 && y < height - 1)
                    {
                        heightMap[x, y] = Mathf.Lerp(heightMap[x, y], smoothedMap[x, y], blendFactor);
                    }
                }
            }
        }
    }
}