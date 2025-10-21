// /Assets/Project/Scripts/Factory/MobileFactory.cs
using UnityEngine;
using UnityEngine.AI;
using AutoForge.World; // Assuming this namespace exists from your previous code
using AutoForge.Core; // Assuming FactoryManager is here

namespace AutoForge.Factory // Keeping the namespace
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Rigidbody))]
    public class MobileFactory : MonoBehaviour
    {
        private enum FactoryState { Idle, Following }
        private FactoryState currentState;

        [Header("Behavior")]
        [Tooltip("The distance from the player the factory will try to maintain.")]
        public float stoppingDistance = 5f;
        [Tooltip("The distance buffer beyond stoppingDistance before the factory starts moving.")]
        public float activationDistance = 7f;

        [Header("Hover Physics")]
        [Tooltip("The ideal height the factory should float above the ground.")]
        public float hoverHeight = 3.0f;
        [Tooltip("The strength of the force pushing the factory up. (Uses Acceleration, ignores mass)")]
        public float hoverForce = 50f;
        [Tooltip("The damping force to prevent oscillation. (Uses Acceleration, ignores mass)")]
        public float hoverDamp = 5f;
        [Tooltip("The force applied to move the factory towards its target.")]
        public float moveForce = 50f;
        [Tooltip("How quickly the factory rotates to align with ground normal and move direction.")]
        public float rotationSpeed = 10f;
        [Tooltip("Maximum distance to check for ground beneath the factory.")]
        public float groundCheckDistance = 5f;


        [Header("Effects")]
        [Tooltip("How fast the factory bobs up and down when idle.")]
        public float bobFrequency = 0.5f;
        [Tooltip("How high (in meters) the factory bobs.")]
        public float bobAmplitude = 0.25f;
        [Tooltip("How much the factory tilts (in degrees) when accelerating and strafing.")]
        public float tiltAngle = 10f;
        [Tooltip("How quickly the factory tilts to match acceleration.")]
        public float tiltSpeed = 4f;

        private NavMeshAgent navMeshAgent;
        private Rigidbody rb;
        private Transform playerTransform;
        private LayerMask terrainLayer;

        private Vector3 lastVelocity;
        private float currentPitch = 0f;
        private float currentRoll = 0f;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            rb = GetComponent<Rigidbody>();

            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;

            rb.useGravity = false;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            if (FactoryManager.Instance != null)
            {
                // Pass 'this' which is of type AutoForge.Factory.MobileFactory
                FactoryManager.Instance.RegisterFactory(this);
            }
            else
            {
                Debug.LogWarning("MobileFactory: FactoryManager.Instance is null! Cannot register.", this);
            }
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("MobileFactory: Cannot find GameObject with 'Player' tag.", this);
            }

            if (WorldManager.Instance != null)
            {
                terrainLayer = WorldManager.Instance.terrainLayer;
                if (terrainLayer == 0)
                {
                    Debug.LogError("MobileFactory: WorldManager terrainLayer is not set or set to 'Nothing'! Raycasts will fail.", WorldManager.Instance);
                }
            }
            else
            {
                Debug.LogError("MobileFactory: WorldManager.Instance is null! Cannot get terrain layer.", this);
            }

            navMeshAgent.stoppingDistance = stoppingDistance;
            lastVelocity = rb.linearVelocity; // Use linearVelocity here too for consistency
            ChangeState(FactoryState.Idle);
        }

        private void FixedUpdate()
        {
            if (playerTransform == null || terrainLayer == 0 || navMeshAgent == null || rb == null) return;

            UpdateState();
            HandleHoverMovementAndRotation();
        }

        private void UpdateState()
        {
            if (!navMeshAgent.isOnNavMesh)
            {
                if (navMeshAgent.Warp(rb.position))
                {
                    Debug.Log("MobileFactory: Warped NavMeshAgent back onto NavMesh.");
                }
                else
                {
                    // Debug.LogWarning("MobileFactory: NavMeshAgent is not on NavMesh and cannot be warped.", this);
                    return; // Don't proceed if agent is off-mesh
                }
            }

            float distanceToPlayer = Vector3.Distance(rb.position, playerTransform.position);

            switch (currentState)
            {
                case FactoryState.Idle:
                    if (distanceToPlayer > activationDistance)
                    {
                        ChangeState(FactoryState.Following);
                    }
                    break;
                case FactoryState.Following:
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(playerTransform.position, out hit, 5.0f, NavMesh.AllAreas))
                    {
                        navMeshAgent.SetDestination(hit.position);
                    }
                    else
                    {
                        if (NavMesh.SamplePosition(rb.position, out hit, 1.0f, NavMesh.AllAreas))
                        {
                            navMeshAgent.SetDestination(hit.position);
                        }
                        else
                        {
                            // Debug.LogWarning("MobileFactory: Could not find valid NavMesh point near player or factory.", this);
                        }
                    }

                    if (!navMeshAgent.pathPending && navMeshAgent.hasPath && navMeshAgent.remainingDistance <= stoppingDistance)
                    {
                        ChangeState(FactoryState.Idle);
                    }
                    // Add a failsafe: if path becomes invalid or destination is unreachable, go idle.
                    else if (navMeshAgent.hasPath && navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid || navMeshAgent.pathStatus == NavMeshPathStatus.PathPartial)
                    {
                        Debug.LogWarning("MobileFactory: Path became invalid or partial. Returning to Idle.", this);
                        ChangeState(FactoryState.Idle);
                    }
                    break;
            }
        }

        private void HandleHoverMovementAndRotation()
        {
            rb.AddForce(Physics.gravity, ForceMode.Acceleration);

            if (Physics.Raycast(rb.position, Vector3.down, out RaycastHit hit, groundCheckDistance, terrainLayer))
            {
                // --- 1. Hover Lift ---
                float bobOffset = (currentState == FactoryState.Idle) ? Mathf.Sin(Time.time * bobFrequency) * bobAmplitude : 0f;
                float currentTargetHeight = hoverHeight + bobOffset;
                float distanceError = currentTargetHeight - hit.distance;
                float proportionalForce = distanceError * hoverForce;
                float derivativeForce = Vector3.Dot(rb.linearVelocity, hit.normal) * hoverDamp; // Corrected: Use linearVelocity
                Vector3 liftForce = hit.normal * (proportionalForce - derivativeForce);
                rb.AddForce(liftForce, ForceMode.Acceleration);


                // --- 2. Movement ---
                if (currentState == FactoryState.Following && navMeshAgent.desiredVelocity.sqrMagnitude > 0.1f)
                {
                    Vector3 desiredVelocity = navMeshAgent.desiredVelocity;
                    Vector3 projectedVelocity = Vector3.ProjectOnPlane(desiredVelocity, hit.normal);
                    Vector3 currentPlanarVelocity = Vector3.ProjectOnPlane(rb.linearVelocity, hit.normal); // Corrected: Use linearVelocity
                    Vector3 force = (projectedVelocity - currentPlanarVelocity) * moveForce;
                    rb.AddForce(force, ForceMode.Acceleration);
                }


                // --- 3. Rotation (Slope + Look + Tilt) ---
                Vector3 acceleration = (rb.linearVelocity - lastVelocity) / Time.fixedDeltaTime; // Corrected: Use linearVelocity
                lastVelocity = rb.linearVelocity; // Corrected: Use linearVelocity
                Vector3 localAccel = transform.InverseTransformDirection(acceleration);

                float targetPitch = 0f;
                float targetRoll = 0f;
                if (currentState == FactoryState.Following)
                {
                    targetPitch = Mathf.Clamp(-localAccel.z * tiltAngle * 0.1f, -tiltAngle, tiltAngle);
                    targetRoll = Mathf.Clamp(localAccel.x * tiltAngle * 0.1f, -tiltAngle, tiltAngle);
                }
                currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.fixedDeltaTime * tiltSpeed);
                currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.fixedDeltaTime * tiltSpeed);

                Vector3 targetUp = hit.normal;
                Vector3 desiredForward = navMeshAgent.desiredVelocity;
                if (desiredForward.sqrMagnitude < 0.01f)
                {
                    desiredForward = transform.forward;
                }
                Vector3 targetForward = Vector3.ProjectOnPlane(desiredForward, targetUp).normalized;
                if (targetForward == Vector3.zero)
                {
                    targetForward = Vector3.ProjectOnPlane(transform.forward, targetUp).normalized;
                    if (targetForward == Vector3.zero) targetForward = Quaternion.LookRotation(Vector3.right, targetUp) * Vector3.forward;
                }

                Quaternion targetSlopeLookRotation = Quaternion.LookRotation(targetForward, targetUp);
                Quaternion tiltRotation = Quaternion.Euler(currentPitch, 0, currentRoll);
                Quaternion finalTargetRotation = targetSlopeLookRotation * tiltRotation;
                Quaternion smoothedRotation = Quaternion.Slerp(rb.rotation, finalTargetRotation, Time.fixedDeltaTime * rotationSpeed);
                rb.MoveRotation(smoothedRotation);

            }
            else
            {
                // --- Falling Behavior ---
                // Reset tilt smoothly when falling
                currentPitch = Mathf.Lerp(currentPitch, 0f, Time.fixedDeltaTime * tiltSpeed);
                currentRoll = Mathf.Lerp(currentRoll, 0f, Time.fixedDeltaTime * tiltSpeed);

                // Smoothly return rotation towards Vector3.up while falling
                Quaternion flatRotation = Quaternion.LookRotation(transform.forward, Vector3.up);
                Quaternion tilt = Quaternion.Euler(currentPitch, 0, currentRoll);
                Quaternion finalTargetRotation = flatRotation * tilt;
                Quaternion smoothedRotation = Quaternion.Slerp(rb.rotation, finalTargetRotation, Time.fixedDeltaTime * rotationSpeed * 0.5f); // Rotate back slower when falling
                rb.MoveRotation(smoothedRotation);
            }

            navMeshAgent.nextPosition = rb.position;
        }


        private void ChangeState(FactoryState newState)
        {
            if (currentState == newState) return;
            currentState = newState;

            switch (currentState)
            {
                case FactoryState.Idle:
                    navMeshAgent.isStopped = true;
                    navMeshAgent.ResetPath();
                    break;
                case FactoryState.Following:
                    navMeshAgent.isStopped = false;
                    break;
            }
        }

        public void WarpToPosition(Vector3 position)
        {
            if (navMeshAgent != null)
            {
                if (navMeshAgent.Warp(position))
                {
                    rb.position = position;
                    rb.linearVelocity = Vector3.zero; // Corrected: Use linearVelocity
                    rb.angularVelocity = Vector3.zero;
                    lastVelocity = Vector3.zero;
                    ChangeState(FactoryState.Idle);
                    navMeshAgent.ResetPath();
                    Debug.Log($"<color=cyan>[MobileFactory]</color> Warped to {position}");
                }
                else
                {
                    Debug.LogError($"[MobileFactory] Failed to warp NavMeshAgent to {position}. Is it on a NavMesh?", this);
                }
            }
        }

        private void OnDestroy()
        {
            if (FactoryManager.Instance != null)
            {
                // --- FIX FOR ERROR 1 & 3 ---
                // Attempt to unregister, assuming FactoryManager has this method
                // and accepts the namespaced type.
                // If FactoryManager doesn't have UnregisterFactory, comment out/remove this line.
                // If you still get type mismatch errors, check the signature in FactoryManager.cs
                try
                {
                    FactoryManager.Instance.UnregisterFactory(this);
                }
                catch (System.MissingMethodException)
                {
                    Debug.LogWarning("MobileFactory: FactoryManager.Instance does not have an UnregisterFactory method. Cannot unregister.", this);
                }
                catch (System.Exception ex) // Catch other potential errors like type mismatch if method exists but signature is wrong
                {
                    Debug.LogError($"MobileFactory: Error calling UnregisterFactory: {ex.Message}", this);
                }
                // --- END FIX ---
            }
        }
    }
}