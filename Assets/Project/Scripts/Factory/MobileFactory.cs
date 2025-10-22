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
        private enum FactoryState { Idle, Following, Braking, Stuck, Recovering }
        private FactoryState currentState;

        [Header("Behavior")]
        public float stoppingDistance = 5f;
        public float activationDistance = 7f;
        public float activationSizeBuffer = 1.5f;
        public float stuckCheckTime = 3.0f;
        public float brakingForceFactor = 0.9f;

        [Header("Hover Physics")]
        public float hoverHeight = 3.0f;
        public float hoverForce = 20f;
        public float hoverDamp = 10f;
        public float groundCheckDistance = 10f;
        [Tooltip("Desired movement ACCELERATION factor.")]
        public float moveForce = 10f; // Represents acceleration now, tune differently
        public float rotationSpeed = 5f;

        [Header("Idle Bobbing Effect")]
        public float bobFrequency = 0.5f;
        public float bobAmplitude = 0.1f;

        [Header("Recovery")]
        public float minYRecoveryThreshold = -50f;
        public float maxSpeedRecoveryThreshold = 100f;
        [Range(-1f, 1f)] public float uprightDotThreshold = -0.5f;
        public Vector3 recoveryOffset = new Vector3(0, 2, -3);

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

        private Dictionary<Rigidbody, float> originalDrags = new Dictionary<Rigidbody, float>();
        // *** API FIX: Use angularDamping ***
        private Dictionary<Rigidbody, float> originalAngularDampings = new Dictionary<Rigidbody, float>();
        private Bounds combinedBounds;

        [Header("Debug")]
        [SerializeField] private bool enablePeriodicLogging = true;
        [SerializeField] private int logFrequencyFrames = 65;
        [SerializeField] private float torqueMagnitudeWarningThreshold = 500f;
        [SerializeField] private bool drawNavMeshPath = true;
        [SerializeField] private bool drawFactoryBounds = true;
        [SerializeField] private bool enableCollisionLogging = false; // Toggle collision logs

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.updatePosition = false; navMeshAgent.updateRotation = false;
            if (WorldManager.Instance != null) terrainLayer = WorldManager.Instance.terrainLayer; else Debug.LogError("WorldManager Missing!", this);
            if (FactoryManager.Instance != null) FactoryManager.Instance.RegisterFactory(this); else Debug.LogWarning("FactoryManager Missing!", this);
        }

        private void Start()
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player"); if (p != null) playerTransform = p.transform; else Debug.LogError("Player Tag Missing!", this);
            if (coreTile == null) { Debug.LogError("Core Tile Missing!", this); enabled = false; return; }
            coreRigidbody = coreTile.GetComponent<Rigidbody>(); if (coreRigidbody == null) { Debug.LogError("Core RB Missing!", coreTile.gameObject); enabled = false; return; }
            FactoryTile[] tiles = GetComponentsInChildren<FactoryTile>(); foreach (var t in tiles) RegisterTile(t); // Stores drags
            if (allTiles.Count > 0) UpdateCombinedBounds();
            navMeshAgent.stoppingDistance = stoppingDistance; lastPositionCheck = coreRigidbody.position; ChangeState(FactoryState.Idle);
        }

        private void Update()
        {
            if (coreRigidbody != null) navMeshAgent.nextPosition = coreRigidbody.position; if (playerTransform == null || coreRigidbody == null) return;
            UpdateState(); if (Time.frameCount % 10 == 0) UpdateCombinedBounds();
            // Draw Debug (unchanged)
            if (drawNavMeshPath && navMeshAgent.hasPath && navMeshAgent.path.corners.Length > 1) { for (int i = 0; i < navMeshAgent.path.corners.Length - 1; i++) Debug.DrawLine(navMeshAgent.path.corners[i] + Vector3.up * 0.1f, navMeshAgent.path.corners[i + 1] + Vector3.up * 0.1f, Color.cyan); }
            if (drawFactoryBounds) { DrawBounds(combinedBounds, Color.yellow); if (currentState == FactoryState.Idle || currentState == FactoryState.Braking) { DrawWireDisk(combinedBounds.center, GetActivationRadius(), Color.magenta); } }
        }

        private void FixedUpdate()
        {
            debugInfo.Clear(); bool log = enablePeriodicLogging && Time.frameCount % logFrequencyFrames == 0; if (log) debugInfo.AppendLine($"--- Factory FU {Time.frameCount} ---");
            if (coreRigidbody != null)
            {
                if (currentState != FactoryState.Recovering && CheckRecoveryConditions()) { InitiateRecovery(); } // Recovery check
                if (currentState == FactoryState.Following) { HandleMovement(); HandleRotation(); CheckIfStuck(); }
                else if (currentState == FactoryState.Braking) { HandleBraking(); }

                // --- DEBUG: Log Core Rigidbody Settings ---
                if (log)
                {
                    // *** API FIX: Use .angularDamping ***
                    debugInfo.AppendLine($"  RB Settings: M={coreRigidbody.mass}, D={coreRigidbody.linearDamping}, AD={coreRigidbody.angularDamping}"); // Corrected property
                }
            }
            // Logging (unchanged)
            if (log && coreRigidbody != null) { /* ... log state ... */ debugInfo.AppendLine($"  State: {currentState}"); debugInfo.AppendLine($"  RB State: Vel={coreRigidbody.linearVelocity.magnitude:F2}, AngVel={coreRigidbody.angularVelocity.magnitude:F2}"); if (navMeshAgent.isOnNavMesh) debugInfo.AppendLine($"  Nav: Dest={navMeshAgent.destination}, DesVel={navMeshAgent.desiredVelocity.magnitude:F1}, Path={navMeshAgent.hasPath}, Status={navMeshAgent.pathStatus}, Dist={navMeshAgent.remainingDistance:F1}"); else debugInfo.AppendLine("  Nav: Off Mesh"); float aR = GetActivationRadius(); float d = GetDistanceToClosestPoint(); debugInfo.AppendLine($"  Follow: Dist={d:F1}, Activate={aR:F1}"); Debug.Log(debugInfo.ToString()); }
        }

        private void UpdateState()
        {
            // (Unchanged - includes bounds distance check, braking state)
            if (currentState == FactoryState.Recovering) return; if (!navMeshAgent.isOnNavMesh) { if (!navMeshAgent.Warp(coreRigidbody.position)) return; }
            float dist = GetDistanceToClosestPoint(); float actRadius = GetActivationRadius(); bool arrived = !navMeshAgent.pathPending && navMeshAgent.hasPath && navMeshAgent.remainingDistance <= stoppingDistance; bool failed = navMeshAgent.hasPath && navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid;
            switch (currentState)
            {
                case FactoryState.Idle: if (dist > actRadius) ChangeState(FactoryState.Following); break;
                case FactoryState.Following: NavMeshHit h; Vector3 dest = playerTransform.position; if (NavMesh.SamplePosition(dest, out h, 15f, NavMesh.AllAreas)) dest = h.position; else if (NavMesh.SamplePosition(coreRigidbody.position, out h, 2f, NavMesh.AllAreas)) dest = h.position; else { ChangeState(FactoryState.Stuck); return; } if (Vector3.Distance(navMeshAgent.destination, dest) > 0.5f) navMeshAgent.SetDestination(dest); if (arrived) ChangeState(FactoryState.Braking); else if (failed) { ChangeState(FactoryState.Idle); } break;
                case FactoryState.Braking: if (GetAverageVelocity().sqrMagnitude < 0.1f * 0.1f) { ChangeState(FactoryState.Idle); } else if (dist > actRadius) { ChangeState(FactoryState.Following); } break;
                case FactoryState.Stuck: if (dist > actRadius * 1.5f) ChangeState(FactoryState.Following); break;
            }
        }

        // Recovery check logic (unchanged)
        private bool CheckRecoveryConditions() { if (coreRigidbody == null || playerTransform == null) return false; if (Vector3.Dot(coreRigidbody.transform.up, Vector3.up) < uprightDotThreshold) { Debug.LogWarning("[Recovery] Overturned!"); return true; } if (coreRigidbody.position.y < minYRecoveryThreshold) { Debug.LogWarning("[Recovery] Fell below world!"); return true; } if (coreRigidbody.linearVelocity.magnitude > maxSpeedRecoveryThreshold) { Debug.LogWarning($"[Recovery] Speed exceeded! ({coreRigidbody.linearVelocity.magnitude:F1})"); return true; } return false; }
        // Recovery action logic (unchanged)
        private void InitiateRecovery() { Debug.LogError("[Recovery] Initiating!"); ChangeState(FactoryState.Recovering); Vector3 tPos = playerTransform.position + playerTransform.TransformDirection(recoveryOffset); RaycastHit h; if (Physics.Raycast(tPos + Vector3.up * 50f, Vector3.down, out h, 100f, terrainLayer)) { tPos.y = h.point.y + hoverHeight; } else { tPos.y = playerTransform.position.y + recoveryOffset.y; } WarpToPosition(tPos); ChangeState(FactoryState.Idle); Debug.LogWarning("[Recovery] Complete."); }
        // Stuck check logic (unchanged)
        private void CheckIfStuck() { if (!isPotentiallyStuck) return; timeSincePositionCheck += Time.fixedDeltaTime; if (timeSincePositionCheck >= stuckCheckTime) { float dSqr = (coreRigidbody.position - lastPositionCheck).sqrMagnitude; if (dSqr < 0.1f * 0.1f) { ChangeState(FactoryState.Stuck); } lastPositionCheck = coreRigidbody.position; timeSincePositionCheck = 0f; } }

        /// <summary> Calculates and applies movement force based on desired acceleration. </summary>
        private void HandleMovement()
        {
            // (Movement logic based on acceleration - unchanged)
            if (navMeshAgent.desiredVelocity.sqrMagnitude < 0.01f || allTiles.Count == 0 || coreRigidbody == null) { isPotentiallyStuck = false; return; }
            isPotentiallyStuck = true;
            Vector3 dV = navMeshAgent.desiredVelocity; Vector3 cAV = GetAverageVelocity(); Vector3 cU = coreRigidbody.transform.up; Vector3 dPV = Vector3.ProjectOnPlane(dV, cU); Vector3 cPV = Vector3.ProjectOnPlane(cAV, cU); Vector3 vD = dPV - cPV;
            float tM = GetTotalMass(); Vector3 fN = vD * tM * moveForce; // F = m * a
            if (float.IsNaN(fN.x)) { return; }
            foreach (var t in allTiles) { Rigidbody r = t.GetRigidbody(); if (r != null) { float mR = r.mass / tM; t.ApplyMovementForce(fN * mR); } }
        }

        // Braking logic (unchanged)
        private void HandleBraking() { if (allTiles.Count == 0) return; Vector3 aV = GetAverageVelocity(); if (aV.sqrMagnitude < 0.01f) return; Vector3 bF = -aV * brakingForceFactor; if (float.IsNaN(bF.x)) { return; } float tM = GetTotalMass(); foreach (var t in allTiles) { Rigidbody r = t.GetRigidbody(); if (r != null) { float mR = r.mass / tM; r.AddForce(bF * mR, ForceMode.VelocityChange); } } }
        // Rotation logic (unchanged)
        private void HandleRotation() { if (navMeshAgent.desiredVelocity.sqrMagnitude < 0.01f || coreRigidbody == null) return; Vector3 dF = navMeshAgent.desiredVelocity.normalized; if (dF == Vector3.zero) return; Quaternion tR = Quaternion.LookRotation(dF, coreRigidbody.transform.up); Quaternion dR = tR * Quaternion.Inverse(coreRigidbody.rotation); dR.ToAngleAxis(out float a, out Vector3 ax); if (float.IsInfinity(ax.x) || float.IsNaN(ax.x)) { return; } if (a > 180f) a -= 360f; float aR = a * Mathf.Deg2Rad; Vector3 rAV = ax.normalized * aR * rotationSpeed; Vector3 cAV = coreRigidbody.angularVelocity; Vector3 tq = (rAV - cAV); if (float.IsNaN(tq.x)) { return; } coreRigidbody.AddTorque(tq, ForceMode.VelocityChange); }

        // --- Helper Methods --- (Unchanged, use linearVelocity, calculate bounds)
        private Vector3 GetAverageVelocity() { if (allTiles.Count == 0) return Vector3.zero; Vector3 tV = Vector3.zero; int c = 0; foreach (var t in allTiles) { Rigidbody rb = t.GetRigidbody(); if (rb != null) { tV += rb.linearVelocity; c++; } } return (c > 0) ? tV / c : Vector3.zero; }
        private float GetTotalMass() { float m = 0f; foreach (var t in allTiles) { Rigidbody rb = t.GetRigidbody(); if (rb != null) m += rb.mass; } return m > 0.01f ? m : 1f; }
        public string GetCurrentStateName() { return currentState.ToString(); }
        private float GetDistanceToClosestPoint() { if (playerTransform == null || allTiles.Count == 0) return float.MaxValue; Vector3 cP = combinedBounds.ClosestPoint(playerTransform.position); return Vector3.Distance(cP, playerTransform.position); }
        private float GetActivationRadius() { float s = combinedBounds.size.magnitude * 0.5f; return stoppingDistance + (s * activationSizeBuffer); }
        private void UpdateCombinedBounds() { if (allTiles.Count == 0 || coreRigidbody == null) { combinedBounds = new Bounds(transform.position, Vector3.one * 0.1f); return; } Collider cC = coreRigidbody.GetComponent<Collider>(); if (cC != null) { combinedBounds = cC.bounds; } else { combinedBounds = new Bounds(coreRigidbody.position, Vector3.one); } foreach (var t in allTiles) { if (t == coreTile) continue; Collider tC = t.GetComponent<Collider>(); if (tC != null) { combinedBounds.Encapsulate(tC.bounds); } } }
        // --- End Helpers ---

        // ChangeState manages drag (uses angularDamping)
        private void ChangeState(FactoryState newState)
        {
            if (currentState == newState) return;
            float targetDrag = -1f; float targetAngularDamping = -1f; bool needsUpdate = false; // Use angularDamping
            if (newState == FactoryState.Following && (currentState == FactoryState.Idle || currentState == FactoryState.Stuck || currentState == FactoryState.Braking)) { RestoreOriginalDrags(); needsUpdate = false; }
            else if ((newState == FactoryState.Idle || newState == FactoryState.Stuck || newState == FactoryState.Braking || newState == FactoryState.Recovering) && currentState == FactoryState.Following) { targetDrag = 3f; targetAngularDamping = 5f; needsUpdate = true; } // Increase damping when stopping/stuck/recovering
            else if (newState == FactoryState.Idle && currentState == FactoryState.Braking) { targetDrag = 3f; targetAngularDamping = 5f; needsUpdate = true; } // Keep high damping when going Idle from Braking
            if (needsUpdate) { SetTileDrags(targetDrag, targetAngularDamping); }
            currentState = newState; isPotentiallyStuck = false; timeSincePositionCheck = 0f; if (coreRigidbody != null) lastPositionCheck = coreRigidbody.position;
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh) { navMeshAgent.isStopped = (currentState != FactoryState.Following); if (navMeshAgent.isStopped && navMeshAgent.hasPath) navMeshAgent.ResetPath(); }
        }

        // --- Tile Management & Drag Helpers --- (Uses angularDamping)
        public void RegisterTile(FactoryTile tile) { if (tile == null) return; if (!allTiles.Contains(tile)) { allTiles.Add(tile); tile.Initialize(this); Rigidbody rb = tile.GetRigidbody(); if (rb != null) { originalDrags[rb] = rb.linearDamping; originalAngularDampings[rb] = rb.angularDamping; if (currentState != FactoryState.Following) { rb.linearDamping = 3f; rb.angularDamping = 5f; } } UpdateCombinedBounds(); } }
        public void UnregisterTile(FactoryTile tile) { if (tile != null && allTiles.Contains(tile)) { Rigidbody rb = tile.GetRigidbody(); if (rb != null) { originalDrags.Remove(rb); originalAngularDampings.Remove(rb); } allTiles.Remove(tile); UpdateCombinedBounds(); if (tile == coreTile) { Debug.LogError("Core Tile unregistered!", this); enabled = false; coreRigidbody = null; } } }
        // *** API FIX: Use angularDamping ***
        private void SetTileDrags(float drag, float angularDamping) { foreach (var tile in allTiles) { Rigidbody rb = tile.GetRigidbody(); if (rb != null) { rb.linearDamping = drag; rb.angularDamping = angularDamping; } } }
        private void RestoreOriginalDrags() { foreach (var tile in allTiles) { Rigidbody rb = tile.GetRigidbody(); if (rb != null) { if (originalDrags.TryGetValue(rb, out float d)) rb.linearDamping = d; if (originalAngularDampings.TryGetValue(rb, out float ad)) rb.angularDamping = ad; } } }
        // --- End Tile Management ---

        // Warp logic (unchanged)
        public void WarpToPosition(Vector3 position) { if (coreRigidbody == null) return; Vector3 o = transform.position - coreRigidbody.position; Vector3 tCP = position - o; if (navMeshAgent.Warp(tCP)) { List<Rigidbody> rbs = new List<Rigidbody>(); Dictionary<Rigidbody, Vector3> rO = new Dictionary<Rigidbody, Vector3>(); Dictionary<Rigidbody, Quaternion> rR = new Dictionary<Rigidbody, Quaternion>(); Quaternion cIR = Quaternion.identity; Vector3 cOP = Vector3.zero; for (int i = 0; i < allTiles.Count; i++) { Rigidbody rb = allTiles[i].GetRigidbody(); if (rb != null) { if (allTiles[i] == coreTile) { cIR = Quaternion.Inverse(rb.rotation); cOP = rb.position; } rb.isKinematic = true; rbs.Add(rb); } } foreach (var rb in rbs) { if (rb != coreRigidbody) { Vector3 off = rb.position - cOP; rO[rb] = cIR * off; rR[rb] = cIR * rb.rotation; } } transform.position = tCP; transform.rotation = Quaternion.identity; coreRigidbody.position = tCP; coreRigidbody.rotation = Quaternion.identity; foreach (var kvp in rO) { Rigidbody rb = kvp.Key; rb.position = tCP + (Quaternion.identity * kvp.Value); rb.rotation = Quaternion.identity * rR[rb]; } foreach (var rb in rbs) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = false; } ChangeState(FactoryState.Idle); UpdateCombinedBounds(); } else Debug.LogError($"Failed warp near {tCP}.", this); }

        private void OnDestroy() { if (FactoryManager.Instance != null) FactoryManager.Instance.UnregisterFactory(this); }

        // --- Gizmo Drawing Helpers --- (unchanged)
        void DrawBounds(Bounds b, Color c) { Vector3 cen = b.center; Vector3 ext = b.extents; Vector3[] pts = new Vector3[8]; pts[0] = cen + new Vector3(-ext.x, -ext.y, -ext.z); pts[1] = cen + new Vector3(ext.x, -ext.y, -ext.z); pts[2] = cen + new Vector3(ext.x, -ext.y, ext.z); pts[3] = cen + new Vector3(-ext.x, -ext.y, ext.z); pts[4] = cen + new Vector3(-ext.x, ext.y, -ext.z); pts[5] = cen + new Vector3(ext.x, ext.y, -ext.z); pts[6] = cen + new Vector3(ext.x, ext.y, ext.z); pts[7] = cen + new Vector3(-ext.x, ext.y, ext.z); Color pC = Gizmos.color; Gizmos.color = c; Gizmos.DrawLine(pts[0], pts[1]); Gizmos.DrawLine(pts[1], pts[2]); Gizmos.DrawLine(pts[2], pts[3]); Gizmos.DrawLine(pts[3], pts[0]); Gizmos.DrawLine(pts[4], pts[5]); Gizmos.DrawLine(pts[5], pts[6]); Gizmos.DrawLine(pts[6], pts[7]); Gizmos.DrawLine(pts[7], pts[4]); Gizmos.DrawLine(pts[0], pts[4]); Gizmos.DrawLine(pts[1], pts[5]); Gizmos.DrawLine(pts[2], pts[6]); Gizmos.DrawLine(pts[3], pts[7]); Gizmos.color = pC; }
        void DrawWireDisk(Vector3 p, float r, Color c) { Color pC = Gizmos.color; Gizmos.color = c; int s = 20; float a = 0f; Vector3 st = p + Vector3.right * r; Vector3 l = st; for (int i = 0; i < s + 1; i++) { a += 360f / s; Vector3 n = p + Quaternion.Euler(0, a, 0) * Vector3.right * r; Gizmos.DrawLine(l, n); l = n; } Gizmos.color = pC; }
        void OnDrawGizmos() { if (!Application.isPlaying || !drawFactoryBounds) return; DrawBounds(combinedBounds, Color.yellow); if (currentState == FactoryState.Idle || currentState == FactoryState.Braking) DrawWireDisk(combinedBounds.center, GetActivationRadius(), Color.magenta); }

        // --- Collision Debugging ---
        private void HandleCollision(Collision collision, string type)
        {
            if (!enableCollisionLogging) return; // Check toggle first
            if (collision.rigidbody != null && allTiles.Exists(t => t.GetRigidbody() == collision.rigidbody)) return; // Ignore self

            Vector3 impulse = collision.impulse; float avgForce = impulse.magnitude / Time.fixedDeltaTime; // Approx. force

            // --- DEBUG: Log Collision Forces ---
            if (avgForce > 200f)
            { // Log significant forces
                ContactPoint contact = collision.GetContact(0);
                // ---> UNCOMMENT TO DEBUG PUSHING <---
                // Debug.LogWarning($"[Physics Debug] Collision {type} with {collision.gameObject.name} (Layer: {LayerMask.LayerToName(collision.gameObject.layer)}). Approx Force: {avgForce:F1} at {contact.point}");

                // Add check for player/enemy specifically if needed
                // if(collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Enemy")) {
                //     Debug.LogError($"FACTORY HIT BY {collision.gameObject.name} - CHECK MOVEMENT SCRIPT!");
                // }
            }
        }
        private void OnCollisionEnter(Collision collision) { HandleCollision(collision, "Enter"); }
        private void OnCollisionStay(Collision collision) { if (Time.frameCount % 10 == 0) { HandleCollision(collision, "Stay"); } } // Log Stay less often
        // --- End Collision Debugging ---

    } // End of Class
} // End of Namespace