using UnityEngine;
using System.Text;

namespace AutoForge.Factory
{
    [RequireComponent(typeof(Rigidbody))]
    public class FactoryTile : MonoBehaviour
    {
        [Header("Suspension")]
        public Transform[] suspensionPoints;

        // --- Bobbing Effect (Values controlled by MobileFactory) ---
        // These will be read from coreController if available
        private float bobFrequency = 0.5f;
        private float bobAmplitude = 0.1f;
        // --- End Bobbing ---

        private Rigidbody rb;
        private MobileFactory coreController;
        private StringBuilder debugInfo = new StringBuilder();

        // --- DEBUG ---
        [Header("Debug & Tuning")]
        [Tooltip("Maximum acceleration force applied per suspension point to prevent instability.")]
        [SerializeField] private float maxLiftAcceleration = 100f;
        [SerializeField] private bool enablePeriodicLogging = true;
        [SerializeField] private int logFrequencyFrames = 60;
        // --- END DEBUG ---

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null) { Debug.LogError($"FactoryTile on {gameObject.name} is missing Rigidbody!", gameObject); return; }
            rb.useGravity = false;
            rb.isKinematic = false;
        }

        public void Initialize(MobileFactory factoryCore)
        {
            this.coreController = factoryCore;
            // --- Get Bobbing Params from Controller ---
            if (coreController != null)
            {
                // Assuming MobileFactory script has these public fields now
                // (We need to add them there next)
                // bobFrequency = coreController.bobFrequency;
                // bobAmplitude = coreController.bobAmplitude;
            }
            // --- End Get Bobbing ---
        }

        private void FixedUpdate()
        {
            if (coreController == null || rb == null) return;
            if (suspensionPoints == null || suspensionPoints.Length == 0) return;

            rb.AddForce(Physics.gravity, ForceMode.Acceleration);

            debugInfo.Clear();
            bool shouldLog = enablePeriodicLogging && Time.frameCount % logFrequencyFrames == 0;
            if (shouldLog) debugInfo.AppendLine($"--- Tile {gameObject.name} FixedUpdate {Time.frameCount} ---");

            // --- Check Factory State for Bobbing ---
            // We need a way to ask coreController its current state
            bool isIdle = coreController.GetCurrentStateName() == "Idle"; // Assumes GetCurrentStateName() exists
            // --- End Check State ---

            int groundedPoints = 0;
            foreach (Transform point in suspensionPoints)
            {
                if (point == null) continue;

                RaycastHit hit;
                bool didHit = Physics.Raycast(point.position, Vector3.down, out hit, coreController.groundCheckDistance, coreController.terrainLayer);

                Color rayColor = didHit ? Color.green : Color.red;
                float rayLength = didHit ? hit.distance : coreController.groundCheckDistance;
                Debug.DrawRay(point.position, Vector3.down * rayLength, rayColor, 0f, false);

                if (didHit)
                {
                    groundedPoints++;

                    // --- Calculate Target Height with Bobbing ---
                    float bobOffset = isIdle ? (Mathf.Sin(Time.time * bobFrequency) * bobAmplitude) : 0f;
                    float currentTargetHeight = coreController.hoverHeight + bobOffset;
                    // --- End Target Height ---

                    float distanceError = currentTargetHeight - hit.distance; // Use currentTargetHeight
                    float proportionalForce = distanceError * coreController.hoverForce;
                    float pointVerticalVelocity = Vector3.Dot(rb.GetPointVelocity(point.position), Vector3.up);
                    float derivativeForce = pointVerticalVelocity * coreController.hoverDamp;
                    Vector3 liftAcceleration = hit.normal * (proportionalForce - derivativeForce);

                    if (float.IsNaN(liftAcceleration.x) || float.IsNaN(liftAcceleration.y) || float.IsNaN(liftAcceleration.z))
                    { Debug.LogError($"Tile {gameObject.name} - Point {point.name}: NaN lift accel!", point); liftAcceleration = Vector3.zero; }

                    liftAcceleration = Vector3.ClampMagnitude(liftAcceleration, maxLiftAcceleration);
                    rb.AddForceAtPosition(liftAcceleration, point.position, ForceMode.Acceleration);

                    if (shouldLog)
                        debugInfo.Append($"  P:{point.name} Hit:{hit.distance:F2} Err:{distanceError:F2} VVel:{pointVerticalVelocity:F2} Accel:{liftAcceleration.magnitude:F1}({liftAcceleration.normalized}) Bob:{bobOffset:F2}|");
                }
                else
                {
                    if (shouldLog) debugInfo.Append($"  P:{point.name} NoHit |");
                }
            }

            if (shouldLog)
            {
                debugInfo.AppendLine($"\n  State: Vel={rb.linearVelocity:F2} ({rb.linearVelocity.magnitude:F2}), AngVel={rb.angularVelocity:F2} ({rb.angularVelocity.magnitude:F2}), Grounded={groundedPoints}/{suspensionPoints.Length}");
                Debug.Log(debugInfo.ToString());
            }
        }

        public void ApplyMovementForce(Vector3 force)
        { /* ... unchanged ... */
            if (rb == null) return;
            if (float.IsNaN(force.x) || float.IsNaN(force.y) || float.IsNaN(force.z))
            { Debug.LogError($"Tile {gameObject.name}: Received NaN movement force!", gameObject); return; }
            rb.AddForce(force, ForceMode.Force);
        }
        public Rigidbody GetRigidbody()
        { /* ... unchanged ... */
            if (rb == null) rb = GetComponent<Rigidbody>();
            return rb;
        }
        private void OnDestroy()
        { /* ... unchanged ... */
            if (coreController != null) coreController.UnregisterTile(this);
        }
    }
}