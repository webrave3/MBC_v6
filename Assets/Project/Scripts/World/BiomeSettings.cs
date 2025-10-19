// /Assets/Project/Scripts/World/BiomeSettings.cs
using UnityEngine;

namespace AutoForge.World
{
    [CreateAssetMenu(fileName = "NewBiomeSettings", menuName = "AutoForge/Biome Settings")]
    public class BiomeSettings : ScriptableObject
    {
        [Header("Identification")]
        public string biomeName = "Unnamed Biome";

        [Header("Conditions")]
        [Tooltip("Minimum temperature (inclusive) for this biome.")]
        public float minTemperature = 0f;
        [Tooltip("Maximum temperature (exclusive) for this biome.")]
        public float maxTemperature = 1f;
        [Tooltip("Minimum humidity (inclusive) for this biome.")]
        public float minHumidity = 0f;
        [Tooltip("Maximum humidity (exclusive) for this biome.")]
        public float maxHumidity = 1f;

        [Header("Rendering")]
        [Tooltip("The primary material used for rendering chunks in this biome (Legacy - Now uses WorldManager's material).")]
        public Material terrainMaterial; // You can keep this for potential non-splatmap uses or phase it out.

        [Tooltip("The color tint or weight used if generating a simple splatmap texture (Legacy).")]
        public Color splatColor = Color.white;

        // --- ADD THIS LINE ---
        [Header("Splatmap Blending")]
        [Tooltip("The 'center' point of this biome in the Temp/Humidity graph (X=Temp, Y=Humidity) used for smooth blending.")]
        public Vector2 biomeGraphPosition = Vector2.one * 0.5f; // Default to center

        /// <summary>
        /// Checks if a given temperature and humidity fall within this biome's range.
        /// </summary>
        public bool IsMatch(float temperature, float humidity)
        {
            return temperature >= minTemperature && temperature < maxTemperature &&
                   humidity >= minHumidity && humidity < maxHumidity;
        }

        /// <summary>
        /// Returns the color associated with this biome for splatmap generation (Legacy).
        /// </summary>
        public Color GetSplatColor()
        {
            return splatColor;
        }
    }
}