// /Assets/Project/Scripts/World/NoiseSettings.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewNoiseSettings", menuName = "AutoForge/Noise Settings")]
public class NoiseSettings : ScriptableObject
{
    public int seed;

    [Min(0.0001f)]
    public float scale = 25f;

    [Range(1, 8)]
    public int octaves = 4; // Number of layers (Macro, Meso, Micro...)

    [Range(0f, 1f)]
    public float persistence = 0.5f; // How much amplitude is reduced each octave

    [Min(1f)]
    public float lacunarity = 2f; // How much frequency is increased each octave

    public Vector2 offset; // To scroll the noise map
}