// /Assets/Project/Scripts/Gameplay/ResourcePickup.cs
using UnityEngine;
using AutoForge.Core; // For ResourceManager
using System.Collections;

namespace AutoForge.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class ResourcePickup : MonoBehaviour
    {
        [Header("Resource Info")]
        public ResourceType resourceType;
        public int amount = 1;

        [Header("Homing Settings")]
        [Tooltip("The time (in seconds) after being triggered before homing starts.")]
        [SerializeField] private float homingDelay = 0.2f;
        [Tooltip("Base time (approx.) it takes to reach the target. Lower = faster acceleration.")]
        [SerializeField] private float smoothTimeBase = 0.3f;
        [Tooltip("How much the initial distance affects the smooth time (0 = no effect, 1 = fully proportional).")]
        [Range(0f, 1f)]
        [SerializeField] private float distanceInfluence = 0.2f;
        [Tooltip("The base maximum speed the pickup can travel.")]
        [SerializeField] private float maxSpeedBase = 50f;
        [Tooltip("An initial speed boost applied when homing starts.")]
        [SerializeField] private float initialBoost = 1f;
        [Tooltip("Offset from the player's pivot point to aim towards (e.g., Vector3.up * 0.5f).")]
        [SerializeField] private Vector3 targetOffset = Vector3.zero;
        [Tooltip("Distance at which the pickup instantly snaps to the player.")]
        [SerializeField] private float snapDistance = 0.15f; // New QoL variable
        [Tooltip("Distance at which collection normally occurs if snap distance isn't met first.")]
        [SerializeField] private float collectionDistance = 0.8f;

        [Header("Catch-Up Behavior")]
        [Tooltip("Distance beyond which the catch-up boost starts applying.")]
        [SerializeField] private float catchUpMinDistance = 10f;
        [Tooltip("Maximum speed multiplier applied when player is far away and moving away.")]
        [SerializeField] private float catchUpBoostMultiplier = 2.0f;

        [Header("Effects (Optional)")]
        [Tooltip("Particle effect prefab to instantiate upon collection.")]
        [SerializeField] private GameObject collectionEffectPrefab; // New QoL variable

        // --- Private Variables ---
        private Transform targetPlayer;
        private bool isHoming = false;
        private Rigidbody rb;
        private ItemEffects itemEffects;
        private Vector3 homingVelocity = Vector3.zero;
        private float _currentSmoothTime;
        private Vector3 _previousPlayerPosition;
        private bool _isCollecting = false; // Prevent double collection

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            itemEffects = GetComponent<ItemEffects>(); // Get the optional effects script
        }

        void Update()
        {
            // Only run logic if homing is active and the target is valid
            if (isHoming && targetPlayer != null)
            {
                Vector3 targetPosition = targetPlayer.position + targetOffset;
                float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

                // --- IMMEDIATE COLLECTION CHECK (Snap) ---
                if (distanceToTarget < snapDistance)
                {
                    CollectAndDestroy();
                    return; // Exit Update early
                }
                // --- END SNAP CHECK ---

                // --- Calculate Dynamic Max Speed ---
                float currentMaxSpeed = maxSpeedBase;
                // Check if Time.deltaTime is valid to prevent division by zero on first frame or pauses
                if (Time.deltaTime > Mathf.Epsilon)
                {
                    Vector3 playerMovement = targetPlayer.position - _previousPlayerPosition;
                    Vector3 directionToPlayer = (targetPosition - transform.position).normalized;
                    // Ensure directionToPlayer is valid before using Dot product
                    if (directionToPlayer != Vector3.zero)
                    {
                        float playerSpeedAway = Vector3.Dot(playerMovement / Time.deltaTime, directionToPlayer);

                        if (distanceToTarget > catchUpMinDistance && playerSpeedAway > 1.0f)
                        {
                            float distanceFactor = Mathf.InverseLerp(catchUpMinDistance, catchUpMinDistance * 3f, distanceToTarget);
                            float boost = Mathf.Lerp(1.0f, catchUpBoostMultiplier, distanceFactor);
                            currentMaxSpeed = maxSpeedBase * boost;
                        }
                    }
                }
                // --- End Dynamic Max Speed Calc ---

                // Move using SmoothDamp
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref homingVelocity, _currentSmoothTime, currentMaxSpeed);

                // Update previous player position for next frame's calculation
                _previousPlayerPosition = targetPlayer.position;

                // Check if we've reached the normal collection radius
                // Use the distance calculated *before* the SmoothDamp movement for consistency
                if (distanceToTarget < collectionDistance)
                {
                    CollectAndDestroy();
                }
            }
        }

        /// <summary>
        /// Called by the player's collector script to initiate homing towards the player.
        /// </summary>
        public void StartHoming(Transform playerTransform)
        {
            if (isHoming || _isCollecting) return; // Prevent triggering multiple times

            targetPlayer = playerTransform;
            if (targetPlayer == null)
            {
                Debug.LogError("StartHoming called with a null playerTransform!", this);
                return;
            }
            _previousPlayerPosition = targetPlayer.position; // Initialize previous position
            StartCoroutine(HomingCoroutine());
        }

        /// <summary>
        /// Handles the initial setup, delay, and activation of homing.
        /// </summary>
        private IEnumerator HomingCoroutine()
        {
            // Make non-physical and disable effects immediately
            rb.isKinematic = true;
            if (GetComponent<Collider>() != null)
            {
                GetComponent<Collider>().enabled = false;
            }
            if (itemEffects != null)
            {
                itemEffects.enabled = false;
                // Optional: Trigger a small visual effect here to show it's being 'pulled'
            }

            // Calculate Smooth Time based on initial distance
            if (targetPlayer != null) // Check target validity again before distance calc
            {
                float initialDistance = Vector3.Distance(transform.position, targetPlayer.position + targetOffset);
                _currentSmoothTime = Mathf.Lerp(smoothTimeBase, smoothTimeBase * (1 + initialDistance * 0.1f), distanceInfluence);

                // Apply Initial Boost
                Vector3 directionToTarget = ((targetPlayer.position + targetOffset) - transform.position).normalized;
                if (directionToTarget != Vector3.zero) // Check for zero vector
                {
                    homingVelocity = directionToTarget * initialBoost;
                }
                else
                {
                    homingVelocity = Vector3.zero; // Or some default if already at target
                }
            }
            else
            {
                _currentSmoothTime = smoothTimeBase; // Fallback if target is somehow lost
                homingVelocity = Vector3.zero;
                yield break; // Exit coroutine if target is lost
            }


            // Wait for the delay
            yield return new WaitForSeconds(homingDelay);

            // Activate homing in Update loop (check target one last time)
            if (targetPlayer != null)
            {
                isHoming = true;
            }
            else
            {
                Debug.LogWarning("Player target lost during homing delay. Pickup will not home.", this);
            }
        }

        /// <summary>
        /// Handles adding the resource to the manager and destroying the pickup object.
        /// </summary>
        private void CollectAndDestroy()
        {
            // Prevent multiple collections from rapid Update calls or collisions
            if (_isCollecting) return;
            _isCollecting = true;
            isHoming = false; // Stop homing logic

            // Add resource if the manager exists
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.AddResource(resourceType, amount);
            }
            else
            {
                Debug.LogWarning("ResourceManager.Instance is null! Cannot add resource.", this);
            }

            // --- Quality Addition: Instantiate Collection Effect ---
            if (collectionEffectPrefab != null)
            {
                Instantiate(collectionEffectPrefab, transform.position, Quaternion.identity);
                // Consider parenting to player briefly or using an object pool later
            }
            // --- End Quality Addition ---

            // --- Quality Addition: Play Collection Sound ---
            // Example: AudioManager.Instance.PlaySound("PickupCollect", transform.position);
            // --- End Quality Addition ---

            // Destroy this pickup object
            Destroy(gameObject);
        }
    }
}