using UnityEngine;
using UnityEngine.InputSystem;
using AutoForge.Core;
using AutoForge.UI; // Make sure this is included

namespace AutoForge.Player
{
    public class PlayerBuilder : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Material previewMaterial;

        [Header("Placement Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float maxBuildDistance = 100f;
        [SerializeField] private float placementYOffset = 0.5f;

        private GameObject buildPreview;
        private BuildingData currentBuildingData;
        private bool isBuildMode = false;

        public bool IsInBuildMode => isBuildMode;

        // The HotbarManager now handles the "OnBuild" and "OnAttack" for building.
        // This script now only listens for the final placement click.
        public void OnAttack(InputValue value)
        {
            if (isBuildMode && value.isPressed)
            {
                PlaceBuilding();
            }
        }

        private void Update()
        {
            if (isBuildMode && buildPreview != null)
            {
                MoveAndAlignPreview();
            }
        }

        // This is called by the HotbarManager to start projecting a building
        public void SetBuildingToPlace(BuildingData data)
        {
            if (data == null)
            {
                CancelBuildMode();
                return;
            }

            isBuildMode = true;
            currentBuildingData = data;

            if (buildPreview != null) Destroy(buildPreview);

            buildPreview = Instantiate(currentBuildingData.buildingPrefab);
            SetLayerRecursively(buildPreview, LayerMask.NameToLayer("Ignore Raycast"));

            if (buildPreview.TryGetComponent<Renderer>(out var renderer) && previewMaterial != null)
            {
                renderer.material = previewMaterial;
            }
            if (buildPreview.TryGetComponent<Building>(out var buildingScript))
            {
                buildingScript.enabled = false;
            }
        }

        // This is called by the HotbarManager to stop projecting
        public void CancelBuildMode()
        {
            if (buildPreview != null)
            {
                Destroy(buildPreview);
            }
            isBuildMode = false;
            currentBuildingData = null;
        }

        private void PlaceBuilding()
        {
            if (buildPreview == null || currentBuildingData == null) return;

            if (ResourceManager.Instance.HasResource(currentBuildingData.costType, currentBuildingData.costAmount))
            {
                ResourceManager.Instance.SpendResource(currentBuildingData.costType, currentBuildingData.costAmount);
                Instantiate(currentBuildingData.buildingPrefab, buildPreview.transform.position, buildPreview.transform.rotation);

                // After placing, we cancel build mode but stay in the UI to select another piece
                CancelBuildMode();
                // The error was here. PlayerBuilder should not know about HotbarManager.
                // We let the manager handle the state.
            }
            else
            {
                Debug.Log($"Not enough {currentBuildingData.costType.resourceName}!");
            }
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

