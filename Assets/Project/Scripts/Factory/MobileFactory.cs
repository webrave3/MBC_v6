using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class MobileFactory : MonoBehaviour
{
    private enum FactoryState { Idle, Following, Braking }
    private FactoryState currentState;

    [Header("Behavior")]
    [Tooltip("An extra buffer distance beyond the NavMesh 'Stopping Distance' before the factory considers moving.")]
    public float leashDistance = 2f;

    [Tooltip("The distance from the player at which the factory will start to slow down.")]
    public float brakingDistance = 10f;

    [Tooltip("Controls the curve of the deceleration. 1 = linear stop, >1 = sharper, faster stop.")]
    public float brakingCurve = 2f;

    private NavMeshAgent navMeshAgent;
    private Rigidbody rb;
    private Transform playerTransform;

    private void Awake()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;
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
        ChangeState(FactoryState.Idle);
    }

    private void FixedUpdate()
    {
        if (playerTransform == null) return;

        switch (currentState)
        {
            case FactoryState.Idle:
                UpdateIdleState();
                break;
            case FactoryState.Following:
                UpdateFollowingState();
                break;
            case FactoryState.Braking:
                UpdateBrakingState();
                break;
        }
    }

    private void ChangeState(FactoryState newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        switch (currentState)
        {
            case FactoryState.Idle:
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                navMeshAgent.isStopped = true;
                rb.isKinematic = true;
                break;
            case FactoryState.Following:
                rb.isKinematic = false;
                navMeshAgent.isStopped = false;
                break;
            case FactoryState.Braking:
                rb.isKinematic = false;
                navMeshAgent.isStopped = false;
                break;
        }
    }

    private void UpdateIdleState()
    {
        float activationDistance = navMeshAgent.stoppingDistance + leashDistance;
        if (Vector3.Distance(rb.position, playerTransform.position) > activationDistance)
        {
            ChangeState(FactoryState.Following);
        }
    }

    private void UpdateFollowingState()
    {
        navMeshAgent.SetDestination(playerTransform.position);

        float brakingZoneStart = navMeshAgent.stoppingDistance + brakingDistance;
        if (Vector3.Distance(rb.position, playerTransform.position) <= brakingZoneStart)
        {
            ChangeState(FactoryState.Braking);
            return;
        }

        Vector3 desiredVelocity = navMeshAgent.desiredVelocity;
        rb.linearVelocity = desiredVelocity;

        // --- IMPROVED ROTATION LOGIC ---
        if (navMeshAgent.hasPath && navMeshAgent.desiredVelocity.sqrMagnitude > 0.1f)
        {
            Vector3 lookDirection = (navMeshAgent.steeringTarget - rb.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
                rb.rotation = Quaternion.Slerp(rb.rotation, lookRotation, navMeshAgent.angularSpeed * Time.fixedDeltaTime / 360f);
            }
        }

        navMeshAgent.nextPosition = rb.position;
    }

    private void UpdateBrakingState()
    {
        navMeshAgent.SetDestination(playerTransform.position);

        float stoppingZone = navMeshAgent.stoppingDistance;
        float brakingZoneStart = stoppingZone + brakingDistance;
        float distanceToPlayer = Vector3.Distance(rb.position, playerTransform.position);

        if (distanceToPlayer > brakingZoneStart)
        {
            ChangeState(FactoryState.Following);
            return;
        }

        float brakeT = Mathf.InverseLerp(brakingZoneStart, stoppingZone, distanceToPlayer);
        float easedBrakeT = Mathf.Pow(brakeT, brakingCurve);

        float smoothedSpeed = Mathf.Lerp(navMeshAgent.speed, 0f, easedBrakeT);
        Vector3 brakingVelocity = navMeshAgent.desiredVelocity.normalized * smoothedSpeed;
        rb.linearVelocity = brakingVelocity;

        if (navMeshAgent.hasPath && navMeshAgent.desiredVelocity.sqrMagnitude > 0.01f)
        {
            Vector3 lookDirection = (navMeshAgent.steeringTarget - rb.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
                rb.rotation = Quaternion.Slerp(rb.rotation, lookRotation, navMeshAgent.angularSpeed * Time.fixedDeltaTime / 360f);
            }
        }

        navMeshAgent.nextPosition = rb.position;

        if (distanceToPlayer <= stoppingZone + 0.1f && rb.linearVelocity.sqrMagnitude < 0.01f)
        {
            ChangeState(FactoryState.Idle);
        }
    }
}