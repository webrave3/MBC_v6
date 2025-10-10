using UnityEngine;
using UnityEngine.InputSystem;

namespace AutoForge.Player
{
    public class PlayerBuilder : MonoBehaviour
    {
        [Header("Building Settings")]
        [SerializeField] private GameObject buildingPrefab;
        [SerializeField] private Material previewMaterial;

        [Header("Placement Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float maxBuildDistance = 100f;

        private GameObject buildPreview;
        private bool isBuildMode = false;
        private Camera mainCamera;

        // Public property so other scripts (like PlayerShoot) can check our state
        public bool IsInBuildMode => isBuildMode;

        private void Start()
        {
            mainCamera = Camera.main;
        }

        // Called by the "Build" Input Action (B key) from Send Messages
        public void OnBuild(InputValue value)
        {
            if (value.isPressed)
            {
                ToggleBuildMode();
            }
        }

        // Called by the "Attack" Input Action (Left Mouse) from Send Messages
        public void OnAttack(InputValue value)
        {
            // We only care about this input if we are in build mode
            if (isBuildMode && value.isPressed)
            {
                PlaceBuilding();
            }
        }

        private void ToggleBuildMode()
        {
            isBuildMode = !isBuildMode;

            if (isBuildMode && buildingPrefab != null)
            {
                buildPreview = Instantiate(buildingPrefab);
                if (buildPreview.TryGetComponent<Renderer>(out var renderer) && previewMaterial != null)
                {
                    renderer.material = previewMaterial;
                }
                // Disable the preview's Building script so it doesn't give a bonus
                if (buildPreview.TryGetComponent<Core.Building>(out var buildingScript))
                {
                    buildingScript.enabled = false;
                }
            }
            else if (buildPreview != null)
            {
                Destroy(buildPreview);
            }
        }

        private void Update()
        {
            // Every frame, if we're in build mode, update the preview's position.
            if (isBuildMode && buildPreview != null)
            {
                MoveAndAlignPreview();
            }
        }

        private void MoveAndAlignPreview()
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, groundLayer))
            {
                // Move the preview to the point of impact
                buildPreview.transform.position = hit.point;

                // Rotate the preview to lie flat on the surface we hit
                buildPreview.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
        }

        private void PlaceBuilding()
        {
            if (buildPreview != null)
            {
                // Create the real building at the preview's final position and rotation
                Instantiate(buildingPrefab, buildPreview.transform.position, buildPreview.transform.rotation);
                // Exit build mode
                ToggleBuildMode();
            }
        }
    }
}

