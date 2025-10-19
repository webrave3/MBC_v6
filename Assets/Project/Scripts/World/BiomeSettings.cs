// /Assets/Project/Scripts/World/BiomeSettings.cs
using UnityEngine;

namespace AutoForge.World
{
    [CreateAssetMenu(fileName = "NewBiomeSettings", menuName = "AutoForge/Biome Settings")]
    public class BiomeSettings : ScriptableObject
    {
        [Header("Biome Identity")]
        public string biomeName;

        // The material MUST use a shader that supports texture splatting
        // (e.g., using Vertex Colors).
        public Material terrainMaterial;

        // The 'channel' this biome will paint on the splatmap (vertex colors).
        // 0 = R, 1 = G, 2 = B, 3 = A
        [Range(0, 3)]
        public int splatmapTextureIndex;

        [Header("Rules")]
        [Range(0f, 1f)]
        public float minTemperature = 0f;

        [Range(0f, 1f)]
        public float maxTemperature = 1f;

        [Range(0f, 1f)]
        public float minHumidity = 0f;

        [Range(0f, 1f)]
        public float maxHumidity = 1f;

        /// <summary>
        /// Checks if a given temperature and humidity fall within this biome's rules.
        /// </summary>
        public bool IsMatch(float temperature, float humidity)
        {
            return temperature >= minTemperature && temperature <= maxTemperature &&
                   humidity >= minHumidity && humidity <= maxHumidity;
        }

        /// <summary>
        /// Gets the vertex color (splatmap weight) for this biome.
        /// </summary>
        public Color GetSplatColor()
        {
            Color color = Color.black;
            color[splatmapTextureIndex] = 1f;
            return color;
        }
    }
}