// /Assets/Project/Scripts/World/WorldSettings.cs
using UnityEngine;
using AutoForge.World; // <-- ADD THIS NAMESPACE

[CreateAssetMenu(fileName = "NewWorldSettings", menuName = "AutoForge/World Settings")]
public class WorldSettings : ScriptableObject
{
    [Header("Chunk Settings")]
    public int chunkSize = 100; // Size of one chunk in world units (e.g., 100x100)
    public int chunkHeight = 50; // Max possible height of terrain
    public int viewDistance = 5; // In chunks (e.g., 5 = 11x11 grid)

    // How many vertices per chunk edge (e.g., 101 for a 100x100 unit chunk)
    // Higher = more detail, lower = better performance.
    [Tooltip("Vertices per chunk edge. 101 is a good default for a 100-size chunk.")]
    public int chunkResolution = 101;

    [Header("Prefabs")]
    public GameObject chunkPrefab;

    [Header("Noise Settings")]
    public NoiseSettings heightNoiseSettings;
    public NoiseSettings temperatureNoiseSettings;
    public NoiseSettings humidityNoiseSettings;
    public NoiseSettings buildZoneNoiseSettings; // For Component 1.5

    [Header("Biome Settings")]
    public BiomeSettings[] biomes;
    public BiomeSettings defaultBiome; // Fallback if no biome matches

    [Header("AUTO-FORGE Constraints")]
    [Tooltip("The maximum slope angle (in degrees) the factory can climb.")]
    [Range(0f, 90f)]
    public float maxNavigableSlope = 30f;

    [Tooltip("How strongly to flatten build zones (0 = no flat, 1 = max flat).")]
    [Range(0f, 1f)]
    public float buildZoneFlattenStrength = 0.8f;

    /// <summary>
    /// Finds the correct biome based on temp/humidity rules.
    /// </summary>
    public BiomeSettings GetBiome(float temperature, float humidity)
    {
        foreach (var biome in biomes)
        {
            if (biome.IsMatch(temperature, humidity))
            {
                return biome;
            }
        }
        // Return fallback
        return defaultBiome;
    }
}