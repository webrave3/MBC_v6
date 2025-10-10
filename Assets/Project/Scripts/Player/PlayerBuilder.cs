using UnityEngine;
using UnityEngine.InputSystem;

namespace AutoForge.Player
{
    public class PlayerBuilder : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField] private Camera mainCamera;

        [Header("Building Settings")]
        [SerializeField] private GameObject buildingPrefab;
        [SerializeField] private Material previewMaterial;

        [Header("Placement Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float maxBuildDistance = 100f;
        [Tooltip("The vertical offset to apply when placing, to prevent sinking. For a standard cube, 0.5 is a good start.")]
        [SerializeField] private float placementYOffset = 0.5f; // <-- THE NEW VARIABLE

        private GameObject buildPreview;
        private bool isBuildMode = false;

        public bool IsInBuildMode => isBuildMode;

        public void OnBuild(InputValue value)
        {
            if (value.isPressed) ToggleBuildMode();
        }

        public void OnAttack(InputValue value)
        {
            if (isBuildMode && value.isPressed) PlaceBuilding();
        }

        private void ToggleBuildMode()
        {
            isBuildMode = !isBuildMode;

            if (isBuildMode && buildingPrefab != null)
            {
                buildPreview = Instantiate(buildingPrefab);
                SetLayerRecursively(buildPreview, 2); // Set to Ignore Raycast layer

                if (buildPreview.TryGetComponent<Renderer>(out var renderer) && previewMaterial != null)
                {
                    renderer.material = previewMaterial;
                }
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
            if (isBuildMode && buildPreview != null) MoveAndAlignPreview();
        }

        private void MoveAndAlignPreview()
        {
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, groundLayer))
            {
                // --- THIS IS THE FIX ---
                // We add our vertical offset to the hit point on the ground
                Vector3 position = hit.point + new Vector3(0, placementYOffset, 0);

                buildPreview.transform.position = position;
                buildPreview.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
        }

        private void PlaceBuilding()
        {
            if (buildPreview != null)
            {
                // We use the preview's already-corrected position
                Instantiate(buildingPrefab, buildPreview.transform.position, buildPreview.transform.rotation);
                ToggleBuildMode();
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}

