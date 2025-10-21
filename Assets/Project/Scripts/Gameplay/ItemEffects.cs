// /Assets/Project/Scripts/Gameplay/ItemEffects.cs
using UnityEngine;

namespace AutoForge.Gameplay // Assuming ResourcePickup is in this namespace
{
    /// <summary>
    /// Adds visual flair to pickup items like bobbing, spinning, and optional effects.
    /// Designed to be added to pickup prefabs like ScrapPickup.
    /// </summary>
    public class ItemEffects : MonoBehaviour
    {
        [Header("Bobbing")]
        [Tooltip("How fast the item bobs up and down.")]
        public float bobFrequency = 1.5f;
        [Tooltip("How high (in meters) the item bobs.")]
        public float bobAmplitude = 0.15f;

        [Header("Spinning")]
        [Tooltip("How fast the item spins around the Y-axis (degrees per second).")]
        public float spinSpeed = 45f;

        [Header("Optional Effects")]
        [Tooltip("Assign a Particle System GameObject child for extra flair (optional).")]
        public ParticleSystem highlightParticles;
        [Tooltip("Assign a Light component child for a pulsating glow (optional).")]
        public Light pulsateLight;
        [Tooltip("How fast the optional light pulsates.")]
        public float lightPulsateFrequency = 2f;
        [Tooltip("The minimum intensity of the pulsating light.")]
        public float lightMinIntensity = 0.5f;
        [Tooltip("The maximum intensity of the pulsating light.")]
        public float lightMaxIntensity = 1.5f;

        // --- Private Variables ---
        private Vector3 _startLocalPosition;
        private float _randomTimeOffset; // To prevent all items bobbing in sync
        private float _baseLightIntensity; // Original intensity if light exists

        void Awake()
        {
            // Store the initial local position relative to any parent
            _startLocalPosition = transform.localPosition;

            // Add randomness so items dropped at the same time don't sync perfectly
            _randomTimeOffset = Random.Range(0f, 10f);

            // Store base light intensity if a light is assigned
            if (pulsateLight != null)
            {
                _baseLightIntensity = pulsateLight.intensity;
            }

            // Automatically start particles if assigned
            if (highlightParticles != null && !highlightParticles.isPlaying)
            {
                highlightParticles.Play();
            }
        }

        void Update()
        {
            // --- Bobbing Calculation ---
            // Calculate the vertical offset using Sine wave
            float bobOffset = Mathf.Sin((Time.time + _randomTimeOffset) * bobFrequency) * bobAmplitude;
            // Apply the offset to the starting local position
            transform.localPosition = _startLocalPosition + Vector3.up * bobOffset;

            // --- Spinning Calculation ---
            // Rotate around the local Y-axis
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);

            // --- Optional Light Pulsate ---
            if (pulsateLight != null)
            {
                // Calculate intensity using Sine wave, mapped to min/max range
                float lightSine = (Mathf.Sin((Time.time + _randomTimeOffset) * lightPulsateFrequency) + 1f) / 2f; // 0 to 1 range
                pulsateLight.intensity = Mathf.Lerp(lightMinIntensity, lightMaxIntensity, lightSine) * _baseLightIntensity;
            }
        }

        // Optional: Stop particles when the object is disabled/destroyed
        void OnDisable()
        {
            if (highlightParticles != null && highlightParticles.isPlaying)
            {
                highlightParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}