// /Assets/Project/Scripts/Factory/MobileFactory.cs
using UnityEngine;
using UnityEngine.AI;
using AutoForge.World;

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
    public float hoverHeight = 3.0f; // Increased default
    [Tooltip("The strength of the force pushing the factory up. (Uses Acceleration, ignores mass)")]
    public float hoverForce = 50f; // NEW DEFAULT
    [Tooltip("The damping force to prevent oscillation. (Uses Acceleration, ignores mass)")]
    public float hoverDamp = 5f; // NEW DEFAULT
    [Tooltip("The force applied to move the factory towards its target.")]
    public float moveForce = 50f;
    [Tooltip("How quickly the factory rotates to match the ground normal and move direction.")]
    public float rotationSpeed = 10f;

    // --- ADD THIS ENTIRE NEW HEADER ---
    [Header("Effects")]
    [Tooltip("How fast the factory bobs up and down when idle.")]
    public float bobFrequency = 0.5f;
    [Tooltip("How high (in meters) the factory bobs.")]
    public float bobAmplitude = 0.25f;
    [Tooltip("How much the factory tilts (in degrees) when accelerating and strafing.")]
    public float tiltAngle = 10f;
    [Tooltip("How quickly the factory tilts to match acceleration.")]
    public float tiltSpeed = 4f;
    // --- END ADD ---

    private NavMeshAgent navMeshAgent;
    private Rigidbody rb;
    private Transform playerTransform;
    private LayerMask terrainLayer;

    // --- ADD THESE VARIABLES ---
    private Vector3 lastVelocity;
    private float currentPitch = 0f;
    private float currentRoll = 0f;
    // --- END ADD ---

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;

        rb.useGravity = false;
        rb.isKinematic = false;

        FactoryManager.Instance.RegisterFactory(this);
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
        }
        else
        {
            Debug.LogError("MobileFactory: WorldManager.Instance is null! Cannot get terrain layer.", this);
        }

        navMeshAgent.stoppingDistance = stoppingDistance;

        // --- ADD THIS ---
        lastVelocity = rb.linearVelocity;
        // --- END ADD ---

        ChangeState(FactoryState.Idle);
    }

    private void FixedUpdate()
    {
        if (playerTransform == null || terrainLayer == 0) return;

        UpdateState();
        HandleHoverAndRotation();
    }

    private void UpdateState()
    {
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
                navMeshAgent.SetDestination(playerTransform.position);
                if (distanceToPlayer <= stoppingDistance)
                {
                    ChangeState(FactoryState.Idle);
                }
                break;
        }
    }

    private void HandleHoverAndRotation()
    {
        // Apply gravity manually
        rb.AddForce(Physics.gravity, ForceMode.Acceleration);

        // Raycast down to find the ground
        if (Physics.Raycast(rb.position, Vector3.down, out RaycastHit hit, hoverHeight * 3f, terrainLayer))
        {
            // --- 1. Apply Hover Lift Force (with Bobbing) ---

            // --- EDIT THIS SECTION ---
            // Calculate bobbing effect
            float bobOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
            float currentTargetHeight = hoverHeight + bobOffset;
            float distanceError = currentTargetHeight - hit.distance;
            // --- END EDIT ---

            // Calculate damping (how fast we are moving towards/away from ground)
            float verticalDamping = Vector3.Dot(rb.linearVelocity, hit.normal) * hoverDamp;

            // Apply the lift force (Proportional-Derivative force)
            Vector3 liftForce = hit.normal * (distanceError * hoverForce - verticalDamping);

            // --- CHANGE THIS LINE ---
            rb.AddForce(liftForce, ForceMode.Acceleration); // Use Acceleration to ignore mass
            // --- END CHANGE ---

            // --- 2. Apply Movement Force ---
            Vector3 desiredVelocity = navMeshAgent.desiredVelocity;
            Vector3 projectedVelocity = Vector3.ProjectOnPlane(desiredVelocity, hit.normal).normalized * desiredVelocity.magnitude;
            Vector3 force = (projectedVelocity - rb.linearVelocity) * moveForce * Time.fixedDeltaTime;
            rb.AddForce(force, ForceMode.VelocityChange);

            // --- 3. Apply Rotation (with Tilting) ---

            // --- ADD THIS SECTION ---
            // Calculate acceleration for tilting
            Vector3 acceleration = (rb.linearVelocity - lastVelocity) / Time.fixedDeltaTime;
            lastVelocity = rb.linearVelocity;
            Vector3 localAccel = transform.InverseTransformVector(acceleration);

            // Calculate target pitch and roll based on acceleration
            float targetPitch = 0f;
            float targetRoll = 0f;

            if (currentState == FactoryState.Following)
            {
                targetPitch = -localAccel.z * tiltAngle; // Tilt forward/back
                targetRoll = localAccel.x * tiltAngle;   // Tilt left/right
            }

            // Smoothly lerp to the target tilt
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.fixedDeltaTime * tiltSpeed);
            currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.fixedDeltaTime * tiltSpeed);
            // --- END ADD ---

            // --- EDIT THIS SECTION ---
            // Determine look direction
            Vector3 lookDirection = navMeshAgent.desiredVelocity;
            if (lookDirection.sqrMagnitude < 0.1f)
            {
                lookDirection = transform.forward; // If not moving, keep facing forward
            }

            // Calculate target rotation to align with ground and face move direction
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, hit.normal);

            // Apply the tilt as a local rotation on top of the target rotation
            Quaternion tilt = Quaternion.Euler(currentPitch, 0, currentRoll);

            // Smoothly slerp to the final combined rotation
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation * tilt, Time.fixedDeltaTime * rotationSpeed);
            // --- END EDIT ---
        }
        else
        {
            // Not over ground. Let gravity take over.
        }

        // Sync the NavMeshAgent to the Rigidbody's position
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
                rb.angularVelocity = Vector3.zero; // Stop spinning
                break;
            case FactoryState.Following:
                navMeshAgent.isStopped = false;
                break;
        }
    }
}