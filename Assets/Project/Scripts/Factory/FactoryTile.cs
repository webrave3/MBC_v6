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
        private float bobFrequency = 0.5f;
        private float bobAmplitude = 0.1f;
        // --- End Bobbing ---

        private Rigidbody rb;
        private MobileFactory coreController;

        [Header("Debug & Tuning")]
        [Tooltip("Maximum acceleration force applied per suspension point to prevent instability.")]
        [SerializeField] private float maxLiftAcceleration = 100f;

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
            if (coreController != null)
            {
                // Read values from the controller
                bobFrequency = coreController.bobFrequency;
                bobAmplitude = coreController.bobAmplitude;
            }
        }

        private void FixedUpdate()
        {
            if (coreController == null || rb == null) return;
            if (suspensionPoints == null || suspensionPoints.Length == 0) return;

            rb.AddForce(Physics.gravity, ForceMode.Acceleration);

            bool isIdle = coreController.GetCurrentStateName() == "Idle";

            foreach (Transform point in suspensionPoints)
            {
                if (point == null) continue;

                RaycastHit hit;
                bool didHit = Physics.Raycast(point.position, Vector3.down, out hit, coreController.groundCheckDistance, coreController.terrainLayer);

                if (didHit)
                {
                    // --- Calculate Target Height with Bobbing ---
                    float bobOffset = isIdle ? (Mathf.Sin(Time.time * bobFrequency) * bobAmplitude) : 0f;
                    float currentTargetHeight = coreController.hoverHeight + bobOffset;
                    // --- End Target Height ---

                    float distanceError = currentTargetHeight - hit.distance;
                    float proportionalForce = distanceError * coreController.hoverForce;
                    float pointVerticalVelocity = Vector3.Dot(rb.GetPointVelocity(point.position), Vector3.up);
                    float derivativeForce = pointVerticalVelocity * coreController.hoverDamp;
                    Vector3 liftAcceleration = hit.normal * (proportionalForce - derivativeForce);

                    if (float.IsNaN(liftAcceleration.x) || float.IsNaN(liftAcceleration.y) || float.IsNaN(liftAcceleration.z))
                    {
                        liftAcceleration = Vector3.zero;
                    }

                    liftAcceleration = Vector3.ClampMagnitude(liftAcceleration, maxLiftAcceleration);
                    rb.AddForceAtPosition(liftAcceleration, point.position, ForceMode.Acceleration);
                }
            }
        }

        public void ApplyMovementForce(Vector3 force)
        {
            if (rb == null) return;
            if (float.IsNaN(force.x) || float.IsNaN(force.y) || float.IsNaN(force.z))
            {
                return;
            }
            rb.AddForce(force, ForceMode.Force);
        }

        public Rigidbody GetRigidbody()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            return rb;
        }

        private void OnDestroy()
        {
            if (coreController != null)
            {
                coreController.UnregisterTile(this);
            }
        }
    }
}