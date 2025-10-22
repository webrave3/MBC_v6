using UnityEngine;
using UnityEngine.AI;
using AutoForge.World;
using AutoForge.Core;
using System.Collections.Generic;
using System.Text;

namespace AutoForge.Factory
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MobileFactory : MonoBehaviour
    {
        private enum FactoryState { Idle, Following, Stuck }
        private FactoryState currentState;

        [Header("Behavior")]
        public float stoppingDistance = 5f;
        public float activationDistance = 7f;
        public float stuckCheckTime = 3.0f;

        [Header("Hover Physics (Shared Settings for Tiles)")]
        public float hoverHeight = 3.0f;
        public float hoverForce = 15f;
        public float hoverDamp = 8f;
        public float groundCheckDistance = 10f;
        public float moveForce = 50f;
        public float rotationSpeed = 5f;

        [Header("Idle Bobbing Effect")]
        public float bobFrequency = 0.5f;
        public float bobAmplitude = 0.1f;

        [Header("Core References")]
        [SerializeField] private FactoryTile coreTile;

        [HideInInspector] public LayerMask terrainLayer;

        private NavMeshAgent navMeshAgent;
        private Rigidbody coreRigidbody;
        private Transform playerTransform;
        private List<FactoryTile> allTiles = new List<FactoryTile>();
        private StringBuilder debugInfo = new StringBuilder();

        private Vector3 lastPositionCheck;
        private float timeSincePositionCheck;
        private bool isPotentiallyStuck = false;

        [Header("Debug")]
        [SerializeField] private bool enablePeriodicLogging = true;
        [SerializeField] private int logFrequencyFrames = 65;
        [SerializeField] private float torqueMagnitudeWarningThreshold = 500f;
        [SerializeField] private bool drawNavMeshPath = true;

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;

            if (WorldManager.Instance != null) terrainLayer = WorldManager.Instance.terrainLayer;
            else Debug.LogError("MobileFactory: WorldManager.Instance is null!", this);

            if (FactoryManager.Instance != null) FactoryManager.Instance.RegisterFactory(this);
            else Debug.LogWarning("MobileFactory: FactoryManager.Instance is null!", this);
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            else Debug.LogError("MobileFactory: Cannot find Player tag.", this);

            if (coreTile == null) { Debug.LogError("MobileFactory requires 'Core Tile' assignment!", this); enabled = false; return; }
            coreRigidbody = coreTile.GetComponent<Rigidbody>();
            if (coreRigidbody == null) { Debug.LogError($"MobileFactory: Assigned Core Tile '{coreTile.gameObject.name}' missing Rigidbody!", coreTile.gameObject); enabled = false; return; }

            FactoryTile[] presetTiles = GetComponentsInChildren<FactoryTile>();
            foreach (var tile in presetTiles) RegisterTile(tile);
            if (allTiles.Count == 0) Debug.LogWarning("MobileFactory: No child FactoryTiles registered on Start.", this);

            navMeshAgent.stoppingDistance = stoppingDistance;
            lastPositionCheck = coreRigidbody.position;
            ChangeState(FactoryState.Idle);
        }

        private void Update()
        {
            if (coreRigidbody != null) navMeshAgent.nextPosition = coreRigidbody.position;
            if (playerTransform == null || coreRigidbody == null) return;
            UpdateState();

            if (drawNavMeshPath && navMeshAgent.hasPath && navMeshAgent.path.corners.Length > 1)
            {
                for (int i = 0; i < navMeshAgent.path.corners.Length - 1; i++)
                    Debug.DrawLine(navMeshAgent.path.corners[i] + Vector3.up * 0.1f, navMeshAgent.path.corners[i + 1] + Vector3.up * 0.1f, Color.cyan);
            }
        }

        private void FixedUpdate()
        {
            debugInfo.Clear();
            if (enablePeriodicLogging && Time.frameCount % logFrequencyFrames == 0)
                debugInfo.AppendLine($"--- Factory Brain FixedUpdate {Time.frameCount} ---");

            if (currentState == FactoryState.Following && coreRigidbody != null)
            {
                HandleMovement();
                HandleRotation();
                CheckIfStuck();
            }

            if (enablePeriodicLogging && Time.frameCount % logFrequencyFrames == 0 && coreRigidbody != null)
            {
                debugInfo.AppendLine($"  State: {currentState}");
                // *** FIXED: Use .linearVelocity ***
                debugInfo.AppendLine($"  CoreRB State: Vel={coreRigidbody.linearVelocity:F2} ({coreRigidbody.linearVelocity.magnitude:F2}), AngVel={coreRigidbody.angularVelocity:F2} ({coreRigidbody.angularVelocity.magnitude:F2})");
                if (navMeshAgent.isOnNavMesh)
                    debugInfo.AppendLine($"  NavMeshAgent: Dest={navMeshAgent.destination}, DesVel={navMeshAgent.desiredVelocity:F2}({navMeshAgent.desiredVelocity.magnitude:F1}), HasPath={navMeshAgent.hasPath}, Status={navMeshAgent.pathStatus}, RemainDist={navMeshAgent.remainingDistance:F1}");
                else
                    debugInfo.AppendLine("  NavMeshAgent: Currently Off NavMesh");
                Debug.Log(debugInfo.ToString());
            }
        }

        private void UpdateState()
        {
            if (!navMeshAgent.isOnNavMesh)
            {
                if (!navMeshAgent.Warp(coreRigidbody.position)) return;
            }

            float distanceToPlayer = Vector3.Distance(coreRigidbody.position, playerTransform.position);

            switch (currentState)
            {
                case FactoryState.Idle:
                    if (distanceToPlayer > activationDistance) ChangeState(FactoryState.Following);
                    break;

                case FactoryState.Following:
                    NavMeshHit hit;
                    Vector3 targetDestination = playerTransform.position;
                    if (NavMesh.SamplePosition(targetDestination, out hit, 15.0f, NavMesh.AllAreas))
                        targetDestination = hit.position;
                    else if (NavMesh.SamplePosition(coreRigidbody.position, out hit, 2.0f, NavMesh.AllAreas))
                        targetDestination = hit.position;
                    else { Debug.LogError("Cannot find valid NavMesh point near player OR factory!", this); ChangeState(FactoryState.Stuck); return; }

                    if (Vector3.Distance(navMeshAgent.destination, targetDestination) > 0.5f)
                        navMeshAgent.SetDestination(targetDestination);

                    bool arrived = !navMeshAgent.pathPending && navMeshAgent.hasPath && navMeshAgent.remainingDistance <= stoppingDistance;
                    bool pathFailed = navMeshAgent.hasPath && navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid;

                    if (arrived || pathFailed)
                    {
                        if (pathFailed) Debug.LogWarning($"Path became Invalid. Status: {navMeshAgent.pathStatus}", this);
                        ChangeState(FactoryState.Idle);
                    }
                    break;

                case FactoryState.Stuck:
                    if (distanceToPlayer > activationDistance * 1.5f) ChangeState(FactoryState.Following);
                    break;
            }
        }

        private void CheckIfStuck()
        {
            if (!isPotentiallyStuck) return;
            timeSincePositionCheck += Time.fixedDeltaTime;
            if (timeSincePositionCheck >= stuckCheckTime)
            {
                float distanceMovedSqr = (coreRigidbody.position - lastPositionCheck).sqrMagnitude;
                if (distanceMovedSqr < 0.1f * 0.1f)
                {
                    Debug.LogWarning($"Factory detected as Stuck! Moved {Mathf.Sqrt(distanceMovedSqr):F2}m in {stuckCheckTime}s.", this);
                    ChangeState(FactoryState.Stuck);
                }
                lastPositionCheck = coreRigidbody.position;
                timeSincePositionCheck = 0f;
            }
        }

        private void HandleMovement()
        {
            if (navMeshAgent.desiredVelocity.sqrMagnitude < 0.01f || allTiles.Count == 0 || coreRigidbody == null)
            {
                isPotentiallyStuck = false;
                return;
            }
            isPotentiallyStuck = true;

            Vector3 desiredVelocity = navMeshAgent.desiredVelocity;
            Vector3 currentAvgVelocity = GetAverageVelocity();
            Vector3 coreUp = coreRigidbody.transform.up;
            Vector3 desiredPlanarVelocity = Vector3.ProjectOnPlane(desiredVelocity, coreUp);
            Vector3 currentPlanarVelocity = Vector3.ProjectOnPlane(currentAvgVelocity, coreUp);
            Vector3 velocityDifference = desiredPlanarVelocity - currentPlanarVelocity;
            Vector3 forceNeeded = velocityDifference * moveForce;

            if (float.IsNaN(forceNeeded.x) || float.IsNaN(forceNeeded.y) || float.IsNaN(forceNeeded.z))
            { Debug.LogError("HandleMovement calculated NaN force!", this); return; }

            float totalMass = GetTotalMass();
            if (enablePeriodicLogging && Time.frameCount % logFrequencyFrames == 0)
                debugInfo.Append($"  Move: DesVel={desiredVelocity:F2}({desiredVelocity.magnitude:F1}) CurAvgVel={currentAvgVelocity:F2}({currentAvgVelocity.magnitude:F1}) Force={forceNeeded.magnitude:F1} |");

            foreach (var tile in allTiles)
            {
                Rigidbody tileRb = tile.GetRigidbody();
                if (tileRb != null)
                {
                    float massRatio = tileRb.mass / totalMass;
                    tile.ApplyMovementForce(forceNeeded * massRatio);
                }
            }
        }

        private void HandleRotation()
        {
            if (navMeshAgent.desiredVelocity.sqrMagnitude < 0.01f || coreRigidbody == null) return;
            Vector3 desiredForward = navMeshAgent.desiredVelocity.normalized;
            if (desiredForward == Vector3.zero) return;

            Quaternion targetRotation = Quaternion.LookRotation(desiredForward, coreRigidbody.transform.up);
            Quaternion deltaRotation = targetRotation * Quaternion.Inverse(coreRigidbody.rotation);
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

            if (float.IsInfinity(axis.x) || float.IsNaN(axis.x)) { Debug.LogError("HandleRotation calculated NaN/Inf axis!", this); return; }

            if (angle > 180f) angle -= 360f;
            float angleRad = angle * Mathf.Deg2Rad;
            Vector3 requiredAngularVelocity = axis.normalized * angleRad * rotationSpeed;
            Vector3 currentAngularVelocity = coreRigidbody.angularVelocity;
            Vector3 torque = (requiredAngularVelocity - currentAngularVelocity);

            if (float.IsNaN(torque.x) || float.IsNaN(torque.y) || float.IsNaN(torque.z))
            { Debug.LogError("HandleRotation calculated NaN torque!", this); return; }

            if (enablePeriodicLogging && Time.frameCount % logFrequencyFrames == 0)
                debugInfo.Append($"  Rotate: Angle={angle:F1} Axis={axis:F2} ReqAV={requiredAngularVelocity:F2} CurAV={currentAngularVelocity:F2} Tq={torque.magnitude:F1}({torque:F2}) |");

            if (enablePeriodicLogging && torque.magnitude > torqueMagnitudeWarningThreshold)
                Debug.LogWarning($"MobileFactory HandleRotation: High Torque Magnitude! {torque.magnitude:F1}", this);

            coreRigidbody.AddTorque(torque, ForceMode.VelocityChange);
        }

        private Vector3 GetAverageVelocity()
        {
            if (allTiles.Count == 0) return Vector3.zero;
            Vector3 totalVelocity = Vector3.zero; int validCount = 0;
            foreach (var tile in allTiles)
            {
                Rigidbody rb = tile.GetRigidbody();
                if (rb != null)
                {
                    // *** FIXED: Use .linearVelocity ***
                    totalVelocity += rb.linearVelocity;
                    validCount++;
                }
            }
            return (validCount > 0) ? totalVelocity / validCount : Vector3.zero;
        }

        private float GetTotalMass()
        {
            float totalMass = 0f;
            foreach (var tile in allTiles) { Rigidbody rb = tile.GetRigidbody(); if (rb != null) totalMass += rb.mass; }
            return totalMass > 0.01f ? totalMass : 1f;
        }

        public string GetCurrentStateName()
        {
            return currentState.ToString();
        }

        private void ChangeState(FactoryState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            isPotentiallyStuck = false;
            timeSincePositionCheck = 0f;
            if (coreRigidbody != null) lastPositionCheck = coreRigidbody.position;
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = (currentState == FactoryState.Idle || currentState == FactoryState.Stuck);
                if (currentState == FactoryState.Idle || currentState == FactoryState.Stuck)
                {
                    if (navMeshAgent.hasPath) navMeshAgent.ResetPath();
                }
            }
        }

        public void RegisterTile(FactoryTile tile)
        {
            if (tile == null) return;
            if (!allTiles.Contains(tile)) { allTiles.Add(tile); tile.Initialize(this); }
        }
        public void UnregisterTile(FactoryTile tile)
        {
            if (tile != null && allTiles.Contains(tile))
            {
                allTiles.Remove(tile);
                if (tile == coreTile)
                {
                    Debug.LogError("Core Tile unregistered! Disabling factory.", this);
                    enabled = false; coreRigidbody = null;
                }
            }
        }

        public void WarpToPosition(Vector3 position)
        {
            if (coreRigidbody == null) { Debug.LogError("Cannot WarpToPosition: Core Rigidbody missing.", this); return; }
            Vector3 offset = transform.position - coreRigidbody.position;
            Vector3 targetCorePosition = position - offset;
            if (navMeshAgent.Warp(targetCorePosition))
            {
                List<Rigidbody> rbs = new List<Rigidbody>();
                Dictionary<Rigidbody, Vector3> relativeOffsets = new Dictionary<Rigidbody, Vector3>();
                Dictionary<Rigidbody, Quaternion> relativeRotations = new Dictionary<Rigidbody, Quaternion>();
                Quaternion coreInverseRot = Quaternion.identity;
                Vector3 coreOriginalPos = Vector3.zero;

                for (int i = 0; i < allTiles.Count; i++)
                {
                    Rigidbody rb = allTiles[i].GetRigidbody();
                    if (rb != null)
                    {
                        if (allTiles[i] == coreTile)
                        {
                            coreInverseRot = Quaternion.Inverse(rb.rotation);
                            coreOriginalPos = rb.position;
                        }
                        rb.isKinematic = true;
                        rbs.Add(rb);
                    }
                }
                foreach (var rb in rbs)
                {
                    if (rb != coreRigidbody)
                    {
                        Vector3 offsetFromCore = rb.position - coreOriginalPos;
                        relativeOffsets[rb] = coreInverseRot * offsetFromCore;
                        relativeRotations[rb] = coreInverseRot * rb.rotation;
                    }
                }

                transform.position = targetCorePosition;
                transform.rotation = Quaternion.identity;
                coreRigidbody.position = targetCorePosition;
                coreRigidbody.rotation = Quaternion.identity;

                foreach (var kvp in relativeOffsets)
                {
                    Rigidbody rb = kvp.Key;
                    rb.position = targetCorePosition + (Quaternion.identity * kvp.Value);
                    rb.rotation = Quaternion.identity * relativeRotations[rb];
                }

                foreach (var rb in rbs)
                {
                    // *** FIXED: Use .linearVelocity ***
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = false;
                }
                ChangeState(FactoryState.Idle);
                Debug.Log($"<color=cyan>[MobileFactory]</color> Warped factory to ~{position}");
            }
            else Debug.LogError($"[MobileFactory] Failed to warp NavMeshAgent near {targetCorePosition}.", this);
        }

        private void OnDestroy()
        {
            if (FactoryManager.Instance != null) FactoryManager.Instance.UnregisterFactory(this);
        }
    }
}
