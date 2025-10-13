using UnityEngine;
using AutoForge.Core;

// --- NAMESPACE CHANGED ---
namespace AutoForge.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class ResourcePickup : MonoBehaviour
    {
        public ResourceType resourceType;
        public int amount = 1;

        private Transform targetPlayer;
        private bool isHoming = false;
        private float homingSpeed = 20f;
        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            if (isHoming && targetPlayer != null)
            {
                // Move towards the player
                transform.position = Vector3.MoveTowards(transform.position, targetPlayer.position, homingSpeed * Time.deltaTime);

                // Check if we've reached the player
                if (Vector3.Distance(transform.position, targetPlayer.position) < 0.5f)
                {
                    // If a ResourceManager exists, add the resource
                    if (ResourceManager.Instance != null)
                    {
                        ResourceManager.Instance.AddResource(resourceType, amount);
                    }

                    // Destroy this pickup object
                    Destroy(gameObject);
                }
            }
        }

        // This public method will be called by the player to start the collection process
        public void StartHoming(Transform playerTransform)
        {
            targetPlayer = playerTransform;
            isHoming = true;

            // Disable physics so our movement code can take over smoothly
            rb.isKinematic = true;
            GetComponent<Collider>().enabled = false;
        }
    }
}