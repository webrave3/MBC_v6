using UnityEngine;
using UnityEngine.InputSystem;
using AutoForge.Core;

namespace AutoForge.Player
{
    public class PlayerBuilder : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField] private Camera mainCamera;

        [Header("Building Settings")]
        [SerializeField] private BuildingData buildingToPlace;
        [SerializeField] private Material previewMaterial;

        [Header("Placement Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float maxBuildDistance = 100f;
        [SerializeField] private float placementYOffset = 0.5f;

        private GameObject buildPreview;
        private bool isBuildMode = false;

        public bool IsInBuildMode => isBuildMode;

        public void OnBuild(InputValue value)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPlayerInUIMode) return;
            if (value.isPressed) ToggleBuildMode();
        }

        public void OnAttack(InputValue value)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPlayerInUIMode) return;
            if (isBuildMode && value.isPressed) PlaceBuilding();
        }

        private void ToggleBuildMode()
        {
            isBuildMode = !isBuildMode;

            if (isBuildMode && buildingToPlace != null)
            {
                buildPreview = Instantiate(buildingToPlace.buildingPrefab);
                SetLayerRecursively(buildPreview, 2);

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
                Vector3 position = hit.point + new Vector3(0, placementYOffset, 0);
                buildPreview.transform.position = position;
                buildPreview.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
        }

        private void PlaceBuilding()
        {
            if (buildPreview == null || buildingToPlace == null) return;

            if (ResourceManager.Instance.HasResource(buildingToPlace.costType, buildingToPlace.costAmount))
            {
                ResourceManager.Instance.SpendResource(buildingToPlace.costType, buildingToPlace.costAmount);
                Instantiate(buildingToPlace.buildingPrefab, buildPreview.transform.position, buildPreview.transform.rotation);
                ToggleBuildMode();
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