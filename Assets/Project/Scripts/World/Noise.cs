// /Assets/Project/Scripts/World/Noise.cs
using UnityEngine;

namespace AutoForge.World
{
    public static class Noise
    {
        public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 chunkOffset)
        {
            if (settings == null)
            {
                Debug.LogError("<color=red><b>[Noise ERROR]</b></color> Received null NoiseSettings!");
                return null;
            }
            if (settings.scale <= 0)
            {
                Debug.LogWarning($"<color=yellow>[Noise Warning]</color> Noise scale is zero or negative ({settings.scale}). Clamping to 0.0001f.");
                settings.scale = 0.0001f;
            }
            if (mapWidth <= 0 || mapHeight <= 0)
            {
                Debug.LogError($"<color=red><b>[Noise ERROR]</b></color> Map dimensions are invalid: Width={mapWidth}, Height={mapHeight}");
                return null;
            }


            float[,] noiseMap = new float[mapWidth, mapHeight];

            System.Random prng = new System.Random(settings.seed);
            Vector2[] octaveOffsets = new Vector2[settings.octaves];
            for (int i = 0; i < settings.octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000) + settings.offset.x; // Apply global offset here
                float offsetY = prng.Next(-100000, 100000) - settings.offset.y; // Apply global offset here
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            float maxPossibleHeight = 0;
            float amplitude = 1;
            for (int i = 0; i < settings.octaves; i++)
            {
                maxPossibleHeight += amplitude;
                amplitude *= settings.persistence;
            }
            if (maxPossibleHeight <= 0) maxPossibleHeight = 1; // Safety


            int logX = mapWidth / 2;
            int logY = mapHeight / 2;

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < settings.octaves; i++)
                    {
                        // --- IMPORTANT FIX FOR CHUNK BOUNDARIES ---
                        // The 'chunkOffset' passed in now represents "grid units".
                        // 'x' and 'y' are also "grid units" within the current chunk.
                        // We combine them before scaling by 'settings.scale'.
                        float sampleX = (x + chunkOffset.x) / settings.scale * frequency + octaveOffsets[i].x;
                        float sampleY = (y + chunkOffset.y) / settings.scale * frequency + octaveOffsets[i].y;
                        // --- END FIX ---


                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= settings.persistence;
                        frequency *= settings.lacunarity;
                    }

                    // --- Noise Raw Debug (Optional, keep commented for less spam) ---
                    // if (x == logX && y == logY) {
                    //    Debug.Log($"<color=yellow>[Noise Raw Debug]</color> Offset: {chunkOffset}, Center Raw NoiseHeight (before norm): {noiseHeight:F4}");
                    // }
                    // --- END Noise Raw Debug ---

                    noiseMap[x, y] = noiseHeight;
                }
            }

            // Normalization
            float halfMaxHeight = maxPossibleHeight;
            float fullRange = maxPossibleHeight * 2f;
            if (fullRange <= 0) fullRange = 1f;

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    float currentNoise = noiseMap[x, y];
                    noiseMap[x, y] = (currentNoise + halfMaxHeight) / fullRange;
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y]);
                }
            }

            return noiseMap;
        }
    }
}