using UnityEngine;
using UnityEngine.InputSystem;
using AutoForge.Core; // We need this namespace

namespace AutoForge.Player
{
    public class PlayerBuilder : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField] private Camera mainCamera;

        // --- REFACTORED BUILDING LOGIC ---
        [Header("Building Settings")]
        [Tooltip("The building data for the object we want to place.")]
        [SerializeField] private BuildingData buildingToPlace; // This replaces the prefab and cost fields
        [SerializeField] private Material previewMaterial;
        // --- END REFACTORED LOGIC ---

        [Header("Placement Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float maxBuildDistance = 100f;
        [SerializeField] private float placementYOffset = 0.5f;

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

            // We must have a building selected to enter build mode
            if (isBuildMode && buildingToPlace != null)
            {
                // Instantiate the preview from the data's prefab
                buildPreview = Instantiate(buildingToPlace.buildingPrefab);
                SetLayerRecursively(buildPreview, 2); // Ignore Raycast

                if (buildPreview.TryGetComponent<Renderer>(out var renderer) && previewMaterial != null)
                {
                    renderer.material = previewMaterial;
                }
                if (buildPreview.TryGetComponent<Core.Building>(out var buildingScript))
                {
                    buildingScript.enabled = false; // Disable logic on the preview
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
                Vector3 position = hit.point + new Vector3(0, placementYOffset, 0);
                buildPreview.transform.position = position;
                buildPreview.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
        }

        private void PlaceBuilding()
        {
            if (buildPreview == null || buildingToPlace == null) return;

            // Check for resources USING THE DATA
            if (ResourceManager.Instance.HasResource(buildingToPlace.costType, buildingToPlace.costAmount))
            {
                // Spend the resources USING THE DATA
                ResourceManager.Instance.SpendResource(buildingToPlace.costType, buildingToPlace.costAmount);

                // Instantiate the final building from the data's prefab
                Instantiate(buildingToPlace.buildingPrefab, buildPreview.transform.position, buildPreview.transform.rotation);
                ToggleBuildMode(); // Exit build mode
            }
            else
            {
                Debug.Log($"Not enough {buildingToPlace.costType.resourceName}!");
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