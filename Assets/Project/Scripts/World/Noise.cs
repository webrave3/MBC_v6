// /Assets/Project/Scripts/World/Noise.cs
using UnityEngine;

namespace AutoForge.World
{
    public static class Noise
    {
        // Generates a 2D noise map based on fBM
        public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 chunkOffset)
        {
            float[,] noiseMap = new float[mapWidth, mapHeight];

            // Use seed for pseudo-random offsets per octave
            System.Random prng = new System.Random(settings.seed);
            Vector2[] octaveOffsets = new Vector2[settings.octaves];
            for (int i = 0; i < settings.octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000) + settings.offset.x + chunkOffset.x;
                float offsetY = prng.Next(-100000, 100000) + settings.offset.y + chunkOffset.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            float maxPossibleHeight = 0;
            float amplitude = 1;

            // This is used to calculate maxPossibleHeight for normalization
            for (int i = 0; i < settings.octaves; i++)
            {
                maxPossibleHeight += amplitude;
                amplitude *= settings.persistence;
            }

            // Loop through every point in the map
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    // --- This is the fBM loop ---
                    for (int i = 0; i < settings.octaves; i++)
                    {
                        // Calculate sample coordinates for this octave
                        float sampleX = (x / settings.scale * frequency) + octaveOffsets[i].x;
                        float sampleY = (y / settings.scale * frequency) + octaveOffsets[i].y;

                        // Get the Perlin noise value
                        // * 2 - 1 makes the range [-1, 1] for more interesting terrain
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;

                        // Add it to the total height, scaled by amplitude
                        noiseHeight += perlinValue * amplitude;

                        // Update amplitude and frequency for the next octave (micro-layer)
                        amplitude *= settings.persistence;
                        frequency *= settings.lacunarity;
                    }

                    noiseMap[x, y] = noiseHeight;
                }
            }

            // --- Normalization ---
            // Now, normalize the map to be between 0 and 1
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    // This remaps the value from [-maxPossibleHeight, maxPossibleHeight] to [0, 1]
                    noiseMap[x, y] = (noiseMap[x, y] + maxPossibleHeight) / (maxPossibleHeight * 2f);
                }
            }

            return noiseMap;
        }
    }
}