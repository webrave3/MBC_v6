using UnityEngine;
// --- USING DIRECTIVE CHANGED ---
using AutoForge.Gameplay; // Changed from AutoForge.Items

namespace AutoForge.Player
{
    public class PlayerResourceCollector : MonoBehaviour
    {
        [Tooltip("The radius within which pickups will start homing towards the player.")]
        [SerializeField] private float collectionRadius = 8f;

        private SphereCollider collectionTrigger;

        void Start()
        {
            // Add a SphereCollider component via code
            collectionTrigger = gameObject.AddComponent<SphereCollider>();
            collectionTrigger.isTrigger = true; // Set it to be a trigger
            collectionTrigger.radius = collectionRadius;
        }

        private void OnTriggerEnter(Collider other)
        {
            // When a pickup enters our trigger radius...
            if (other.TryGetComponent<ResourcePickup>(out var pickup))
            {
                // ...tell it to start homing towards us!
                pickup.StartHoming(transform);
            }
        }
    }
}