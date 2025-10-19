// /Assets/Project/Scripts/World/WorldSettings.cs

using UnityEngine;
using AutoForge.World; // <-- Keep your namespace

[CreateAssetMenu(fileName = "NewWorldSettings", menuName = "AutoForge/World Settings")]
public class WorldSettings : ScriptableObject
{
    [Header("Chunk Settings")]
    public int chunkSize = 100; // Size of one chunk in world units (e.g., 100x100)
    public int chunkHeight = 50; // Max possible height of terrain
    public int viewDistance = 5; // In chunks (e.g., 5 = 11x11 grid)

    // How many vertices per chunk edge (e.g., 101 for a 100x100 unit chunk)
    // Higher = more detail, lower = better performance.
    [Tooltip("Vertices per chunk edge. Must be >= 2. 101 is a good default for a 100-size chunk.")]
    [Min(2)]
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

    // --- NEW TEXTURING SECTION ---
    [Header("Texturing")]
    [Range(0, 1)]
    [Tooltip("Normalized slope steepness (0=flat, 1=vertical) to begin blending the 4th (Alpha/Rock) texture.")]
    public float slopeBlendStart = 0.2f;

    [Range(0, 1)]
    [Tooltip("Normalized slope steepness (0=flat, 1=vertical) to be 100% 4th (Alpha/Rock) texture.")]
    public float slopeBlendEnd = 0.5f;
    // --- END OF NEW SECTION ---

    /// <summary>
    /// Finds the correct biome based on temp/humidity rules. (Used for dominant biome or other logic if needed)
    /// </summary>
    public BiomeSettings GetBiome(float temperature, float humidity)
    {
        foreach (var biome in biomes)
        {
            if (biome == null) continue; // Safety check
            if (biome.IsMatch(temperature, humidity))
            {
                return biome;
            }
        }
        // Return fallback
        return defaultBiome;
    }
}